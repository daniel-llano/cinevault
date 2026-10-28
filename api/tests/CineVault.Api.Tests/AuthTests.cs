using System.Net;
using System.Net.Http.Json;
using CineVault.Api.Features.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace CineVault.Api.Tests;

public class AuthTests(CineVaultFactory factory) : IClassFixture<CineVaultFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_ReturnsTokenAndMemberRole()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("New@Test.local", "Secret123", "Newcomer"));

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await resp.Content.ReadFromJsonAsync<AuthResponse>(Json.Options);
        body!.Token.Should().NotBeNullOrEmpty();
        body.User.Email.Should().Be("new@test.local");
        body.User.Role.Should().Be("Member");
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409Problem()
    {
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("dup@test.local", "Secret123", "First"));

        var resp = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("DUP@test.local", "Secret456", "Second"));

        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        resp.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Theory]
    [InlineData("not-an-email", "Secret123", "email")]
    [InlineData("short@test.local", "abc1", "password")]
    [InlineData("nodigit@test.local", "abcdefghij", "password")]
    public async Task Register_InvalidInput_Returns400WithField(string email, string password, string field)
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password, "Someone"));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await resp.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Keys.Should().Contain(k => k.Equals(field, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(CineVaultFactory.AdminEmail, "wrong"));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_ReturnsCurrentUser()
    {
        var admin = await factory.AdminClientAsync();

        var me = await admin.GetFromJsonAsync<UserResponse>("/api/auth/me");

        me!.Email.Should().Be(CineVaultFactory.AdminEmail);
        me.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        (await _client.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task Health_Returns200(string path)
    {
        (await _client.GetAsync(path)).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
