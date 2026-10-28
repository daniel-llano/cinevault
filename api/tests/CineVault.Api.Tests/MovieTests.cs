using System.Net;
using System.Net.Http.Json;
using CineVault.Api.Features.Movies;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace CineVault.Api.Tests;

public class MovieTests(CineVaultFactory factory) : IClassFixture<CineVaultFactory>
{
    private static SaveMovieRequest Movie(string title, int year = 2001, params string[] cast) =>
        new(title, new DateOnly(year, 7, 20), "Overview", cast);

    private static async Task<MovieDetails> CreateAsync(HttpClient admin, SaveMovieRequest request)
    {
        var resp = await admin.PostAsJsonAsync("/api/movies", request, Json.Options);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<MovieDetails>(Json.Options))!;
    }

    private static Task<PagedResponse<MovieSummary>?> SearchAsync(HttpClient client, string query) =>
        client.GetFromJsonAsync<PagedResponse<MovieSummary>>($"/api/movies?{query}", Json.Options);

    [Fact]
    public async Task Create_AsAdmin_Returns201WithLocation()
    {
        var admin = await factory.AdminClientAsync();

        var resp = await admin.PostAsJsonAsync("/api/movies", Movie("Created Film", 1999, "Lead Actor"), Json.Options);

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        resp.Headers.Location!.ToString().Should().MatchRegex(@"/api/movies/\d+$");
    }

    [Fact]
    public async Task Create_AsMember_Returns403()
    {
        var (member, _) = await factory.MemberClientAsync();

        (await member.PostAsJsonAsync("/api/movies", Movie("Nope"), Json.Options)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_Anonymous_Returns401()
    {
        (await factory.CreateClient().PostAsJsonAsync("/api/movies", Movie("Nope"), Json.Options))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_MissingTitle_Returns400()
    {
        var admin = await factory.AdminClientAsync();

        var resp = await admin.PostAsJsonAsync("/api/movies", new SaveMovieRequest("", null, null, null), Json.Options);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await resp.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Keys.Should().Contain(k => k.Equals("title", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Search_IsPublic_AndMatchesTitlePrefixAndCast()
    {
        var admin = await factory.AdminClientAsync();
        var tag = "Zq" + Guid.NewGuid().ToString("N")[..6];
        await CreateAsync(admin, Movie($"{tag} Returns", 2005, "Ada Lovelace"));
        await CreateAsync(admin, Movie("Unrelated Picture", 2005, $"{tag}son Actor"));

        var anon = factory.CreateClient();
        var byTitle = await SearchAsync(anon, $"q={tag[..5]}");
        var byYear = await SearchAsync(anon, $"q={tag}&year=1990");

        byTitle!.Total.Should().Be(2);
        byTitle.Items.Select(m => m.Title).Should().Contain([$"{tag} Returns", "Unrelated Picture"]);
        byYear!.Total.Should().Be(0);
    }

    [Fact]
    public async Task Search_PagesAndSorts()
    {
        var admin = await factory.AdminClientAsync();
        var tag = "Pg" + Guid.NewGuid().ToString("N")[..6];
        foreach (var year in new[] { 1980, 2010, 1995 })
            await CreateAsync(admin, Movie($"{tag} {year}", year));

        var newest = await SearchAsync(factory.CreateClient(), $"q={tag}&sort=Newest&page=1&pageSize=2");

        newest!.Total.Should().Be(3);
        newest.TotalPages.Should().Be(2);
        newest.Items.Select(m => m.ReleaseDate.Year).Should().Equal(2010, 1995);
    }

    [Fact]
    public async Task Get_Missing_Returns404()
    {
        (await factory.CreateClient().GetAsync("/api/movies/999999")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Rating_IsOnePerUser_AndAveragesAcrossUsers()
    {
        var admin = await factory.AdminClientAsync();
        var movie = await CreateAsync(admin, Movie("Rated Film"));
        var (alice, _) = await factory.MemberClientAsync();
        var (bob, _) = await factory.MemberClientAsync();

        await alice.PutAsJsonAsync($"/api/movies/{movie.Id}/rating", new RateMovieRequest(2));
        await alice.PutAsJsonAsync($"/api/movies/{movie.Id}/rating", new RateMovieRequest(4));
        var resp = await bob.PutAsJsonAsync($"/api/movies/{movie.Id}/rating", new RateMovieRequest(5));

        var summary = await resp.Content.ReadFromJsonAsync<RatingSummary>();
        summary!.RatingCount.Should().Be(2);
        summary.AverageRating.Should().Be(4.5);
        summary.MyRating.Should().Be(5);

        var details = await alice.GetFromJsonAsync<MovieDetails>($"/api/movies/{movie.Id}", Json.Options);
        details!.MyRating.Should().Be(4);
    }

    [Fact]
    public async Task Rating_OutOfRange_Returns400()
    {
        var admin = await factory.AdminClientAsync();
        var movie = await CreateAsync(admin, Movie("Strict Film"));
        var (member, _) = await factory.MemberClientAsync();

        (await member.PutAsJsonAsync($"/api/movies/{movie.Id}/rating", new RateMovieRequest(6)))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Rating_CanBeRemoved()
    {
        var admin = await factory.AdminClientAsync();
        var movie = await CreateAsync(admin, Movie("Unrated Film"));
        var (member, _) = await factory.MemberClientAsync();
        await member.PutAsJsonAsync($"/api/movies/{movie.Id}/rating", new RateMovieRequest(3));

        var resp = await member.DeleteAsync($"/api/movies/{movie.Id}/rating");

        var summary = await resp.Content.ReadFromJsonAsync<RatingSummary>();
        summary!.RatingCount.Should().Be(0);
        summary.AverageRating.Should().BeNull();
    }

    [Fact]
    public async Task Update_KeepsRatings_AndReindexesSearch()
    {
        var admin = await factory.AdminClientAsync();
        var movie = await CreateAsync(admin, Movie("Old Name"));
        var (member, _) = await factory.MemberClientAsync();
        await member.PutAsJsonAsync($"/api/movies/{movie.Id}/rating", new RateMovieRequest(5));
        var tag = "Rn" + Guid.NewGuid().ToString("N")[..6];

        var resp = await admin.PutAsJsonAsync($"/api/movies/{movie.Id}", Movie($"{tag} New Name", 2002, "New Cast"), Json.Options);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await resp.Content.ReadFromJsonAsync<MovieDetails>(Json.Options);
        updated!.RatingCount.Should().Be(1);
        updated.Cast.Should().Equal("New Cast");
        (await SearchAsync(factory.CreateClient(), $"q={tag}"))!.Total.Should().Be(1);
    }

    [Fact]
    public async Task Delete_RemovesMovieAndSearchEntry()
    {
        var admin = await factory.AdminClientAsync();
        var tag = "Dl" + Guid.NewGuid().ToString("N")[..6];
        var movie = await CreateAsync(admin, Movie($"{tag} Doomed"));

        (await admin.DeleteAsync($"/api/movies/{movie.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await admin.GetAsync($"/api/movies/{movie.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await SearchAsync(admin, $"q={tag}"))!.Total.Should().Be(0);
    }
}
