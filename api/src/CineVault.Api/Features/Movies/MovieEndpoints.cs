using System.Diagnostics;
using CineVault.Api.Data;
using CineVault.Api.Features.Auth;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace CineVault.Api.Features.Movies;

public static class MovieEndpoints
{
    public const int MaxPageSize = 50;

    public static RouteGroupBuilder MapMovies(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/movies").WithTags("Movies");

        group.MapGet("/", SearchAsync);
        group.MapGet("/{id:int}", GetAsync).WithName("GetMovie");
        group.MapPost("/", CreateAsync).RequireAuthorization("Admin");
        group.MapPut("/{id:int}", UpdateAsync).RequireAuthorization("Admin");
        group.MapDelete("/{id:int}", DeleteAsync).RequireAuthorization("Admin");
        group.MapPut("/{id:int}/rating", RateAsync).RequireAuthorization();
        group.MapDelete("/{id:int}/rating", UnrateAsync).RequireAuthorization();

        return group;
    }

    /// <summary>Search by title or cast name, filter by year and minimum rating, one page at a time.</summary>
    private static async Task<Ok<PagedResponse<MovieSummary>>> SearchAsync(
        CineVaultDbContext db, SearchIndex search,
        string? q, int? year, double? minRating, MovieSort sort = MovieSort.Title, int page = 1, int pageSize = 20,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var movies = db.Movies.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            if (search.IsAvailable)
            {
                var expression = SearchIndex.ToMatchExpression(q);
                if (expression.Length == 0)
                    return TypedResults.Ok(new PagedResponse<MovieSummary>([], 0, page, pageSize, 0));
                movies = search.Match(expression).AsNoTracking();
            }
            else
            {
                var term = q.Trim().ToLowerInvariant();
                movies = movies.Where(m => m.Title.ToLower().Contains(term) || m.Cast.Any(c => c.Name.ToLower().Contains(term)));
            }
        }

        if (year is not null)
            movies = movies.Where(m => m.ReleaseDate.Year == year);
        if (minRating is not null)
            movies = movies.Where(m => m.Ratings.Any() && m.Ratings.Average(r => r.Stars) >= minRating);

        movies = sort switch
        {
            MovieSort.Newest => movies.OrderByDescending(m => m.ReleaseDate).ThenBy(m => m.Id),
            MovieSort.Oldest => movies.OrderBy(m => m.ReleaseDate).ThenBy(m => m.Id),
            MovieSort.TopRated => movies
                .OrderByDescending(m => m.Ratings.Average(r => (double?)r.Stars) ?? 0)
                .ThenByDescending(m => m.Ratings.Count)
                .ThenBy(m => m.Title),
            _ => movies.OrderBy(m => m.Title).ThenBy(m => m.Id)
        };

        var total = await movies.CountAsync(ct);
        var rows = await movies
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new
            {
                m.Id,
                m.Title,
                m.ReleaseDate,
                Cast = m.Cast.OrderBy(c => c.Position).Select(c => c.Name).Take(4).ToList(),
                Average = m.Ratings.Average(r => (double?)r.Stars),
                Count = m.Ratings.Count
            })
            .ToListAsync(ct);

        var items = rows
            .Select(r => new MovieSummary(r.Id, r.Title, r.ReleaseDate, string.Join(", ", r.Cast), Round(r.Average), r.Count))
            .ToList();
        return TypedResults.Ok(new PagedResponse<MovieSummary>(items, total, page, pageSize, sw.ElapsedMilliseconds));
    }

    private static async Task<Results<Ok<MovieDetails>, NotFound>> GetAsync(
        int id, HttpContext http, CineVaultDbContext db, CancellationToken ct)
    {
        var userId = http.User.UserId();
        var movie = await db.Movies.AsNoTracking()
            .Where(m => m.Id == id)
            .Select(m => new
            {
                m.Id,
                m.Title,
                m.ReleaseDate,
                m.Overview,
                Cast = m.Cast.OrderBy(c => c.Position).Select(c => c.Name).ToList(),
                Average = m.Ratings.Average(r => (double?)r.Stars),
                Count = m.Ratings.Count,
                Mine = m.Ratings.Where(r => r.UserId == userId).Select(r => (int?)r.Stars).FirstOrDefault()
            })
            .FirstOrDefaultAsync(ct);

        return movie is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(new MovieDetails(movie.Id, movie.Title, movie.ReleaseDate, movie.Overview, movie.Cast,
                Round(movie.Average), movie.Count, movie.Mine));
    }

    private static async Task<CreatedAtRoute<MovieDetails>> CreateAsync(
        SaveMovieRequest request, CineVaultDbContext db, SearchIndex search, CancellationToken ct)
    {
        var movie = new Movie();
        Apply(movie, request);
        db.Movies.Add(movie);
        await db.SaveChangesAsync(ct);
        await search.UpsertAsync(movie, ct);

        return TypedResults.CreatedAtRoute(ToDetails(movie, null, 0, null), "GetMovie", new { id = movie.Id });
    }

    private static async Task<Results<Ok<MovieDetails>, NotFound>> UpdateAsync(
        int id, SaveMovieRequest request, CineVaultDbContext db, SearchIndex search, CancellationToken ct)
    {
        var movie = await db.Movies.Include(m => m.Cast).FirstOrDefaultAsync(m => m.Id == id, ct);
        if (movie is null) return TypedResults.NotFound();

        // Ratings belong to users, so an edit only touches the movie's own fields and cast.
        db.Cast.RemoveRange(movie.Cast);
        Apply(movie, request);
        movie.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await search.UpsertAsync(movie, ct);

        var stats = await db.Ratings.Where(r => r.MovieId == id)
            .GroupBy(r => r.MovieId)
            .Select(g => new { Average = g.Average(r => (double)r.Stars), Count = g.Count() })
            .FirstOrDefaultAsync(ct);
        return TypedResults.Ok(ToDetails(movie, stats?.Average, stats?.Count ?? 0, null));
    }

    private static async Task<Results<NoContent, NotFound>> DeleteAsync(
        int id, CineVaultDbContext db, SearchIndex search, CancellationToken ct)
    {
        var deleted = await db.Movies.Where(m => m.Id == id).ExecuteDeleteAsync(ct);
        if (deleted == 0) return TypedResults.NotFound();
        await search.RemoveAsync(id, ct);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<RatingSummary>, NotFound, UnauthorizedHttpResult>> RateAsync(
        int id, RateMovieRequest request, HttpContext http, CineVaultDbContext db, CancellationToken ct)
    {
        if (http.User.UserId() is not { } userId) return TypedResults.Unauthorized();
        if (!await db.Movies.AnyAsync(m => m.Id == id, ct)) return TypedResults.NotFound();

        var rating = await db.Ratings.FindAsync([id, userId], ct);
        if (rating is null)
            db.Ratings.Add(new Rating { MovieId = id, UserId = userId, Stars = request.Stars });
        else
        {
            rating.Stars = request.Stars;
            rating.RatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(await SummaryAsync(db, id, request.Stars, ct));
    }

    private static async Task<Results<Ok<RatingSummary>, NotFound, UnauthorizedHttpResult>> UnrateAsync(
        int id, HttpContext http, CineVaultDbContext db, CancellationToken ct)
    {
        if (http.User.UserId() is not { } userId) return TypedResults.Unauthorized();
        if (!await db.Movies.AnyAsync(m => m.Id == id, ct)) return TypedResults.NotFound();

        await db.Ratings.Where(r => r.MovieId == id && r.UserId == userId).ExecuteDeleteAsync(ct);
        return TypedResults.Ok(await SummaryAsync(db, id, null, ct));
    }

    private static async Task<RatingSummary> SummaryAsync(CineVaultDbContext db, int movieId, int? mine, CancellationToken ct)
    {
        var ratings = db.Ratings.Where(r => r.MovieId == movieId);
        var count = await ratings.CountAsync(ct);
        var average = count == 0 ? (double?)null : await ratings.AverageAsync(r => (double)r.Stars, ct);
        return new RatingSummary(Round(average), count, mine);
    }

    private static void Apply(Movie movie, SaveMovieRequest request)
    {
        movie.Title = request.Title.Trim();
        movie.ReleaseDate = request.ReleaseDate!.Value;
        movie.Overview = string.IsNullOrWhiteSpace(request.Overview) ? null : request.Overview.Trim();
        movie.Cast = (request.Cast ?? [])
            .Select(n => n.Trim())
            .Where(n => n.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select((name, i) => new CastMember { Name = name[..Math.Min(name.Length, 200)], Position = i })
            .ToList();
    }

    private static MovieDetails ToDetails(Movie m, double? average, int count, int? mine) =>
        new(m.Id, m.Title, m.ReleaseDate, m.Overview, m.Cast.OrderBy(c => c.Position).Select(c => c.Name).ToList(),
            Round(average), count, mine);

    private static double? Round(double? value) => value is null ? null : Math.Round(value.Value, 2);
}
