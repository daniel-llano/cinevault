using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using Microsoft.EntityFrameworkCore;

namespace CineVault.Api.Data;

/// <summary>Applies migrations, creates the bootstrap admin, loads the catalog CSV, and builds the search index.</summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration config, ILogger logger, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CineVaultDbContext>();
        var search = scope.ServiceProvider.GetRequiredService<SearchIndex>();

        if (config.GetValue("Database:MigrateOnStartup", true))
            await db.Database.MigrateAsync(ct);

        await EnsureAdminAsync(db, config, logger, ct);

        var csvPath = config["Catalog:ImportCsv"];
        if (!string.IsNullOrWhiteSpace(csvPath) && !await db.Movies.AnyAsync(ct))
        {
            var path = Path.IsPathRooted(csvPath) ? csvPath : Path.Combine(AppContext.BaseDirectory, csvPath);
            if (File.Exists(path))
            {
                var count = await ImportCsvAsync(db, path, ct);
                logger.LogInformation("Imported {Count} movies from {Path}", count, path);
            }
            else
            {
                logger.LogWarning("Catalog:ImportCsv points at {Path}, which doesn't exist", path);
            }
        }

        if (search.IsAvailable && !await search.IsInSyncAsync(ct))
        {
            await search.RebuildAsync(ct);
            logger.LogInformation("Rebuilt the search index");
        }
    }

    private static async Task EnsureAdminAsync(CineVaultDbContext db, IConfiguration config, ILogger logger, CancellationToken ct)
    {
        var email = config["Bootstrap:AdminEmail"]?.Trim().ToLowerInvariant();
        var password = config["Bootstrap:AdminPassword"];
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password)) return;
        if (await db.Users.AnyAsync(u => u.Email == email, ct)) return;

        db.Users.Add(new User
        {
            Email = email,
            DisplayName = "Administrator",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = UserRole.Admin
        });
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Created bootstrap admin {Email}", email);
    }

    private static async Task<int> ImportCsvAsync(CineVaultDbContext db, string path, CancellationToken ct)
    {
        db.ChangeTracker.AutoDetectChangesEnabled = false;
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var batch = new List<Movie>(1000);
        var total = 0;

        await foreach (var row in csv.GetRecordsAsync<CsvRow>(ct))
        {
            if (string.IsNullOrWhiteSpace(row.Title)
                || !DateOnly.TryParse(row.ReleaseDate, CultureInfo.InvariantCulture, out var released))
                continue;

            batch.Add(new Movie
            {
                Title = row.Title.Trim()[..Math.Min(row.Title.Trim().Length, 300)],
                ReleaseDate = released,
                Cast = row.Actors
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Take(8)
                    .Select((name, i) => new CastMember { Name = name[..Math.Min(name.Length, 200)], Position = i })
                    .ToList()
            });

            if (batch.Count == batch.Capacity)
            {
                total += await FlushAsync(db, batch, ct);
            }
        }

        total += await FlushAsync(db, batch, ct);
        await tx.CommitAsync(ct);
        db.ChangeTracker.AutoDetectChangesEnabled = true;
        return total;
    }

    private static async Task<int> FlushAsync(CineVaultDbContext db, List<Movie> batch, CancellationToken ct)
    {
        if (batch.Count == 0) return 0;
        db.Movies.AddRange(batch);
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
        var count = batch.Count;
        batch.Clear();
        return count;
    }

    private sealed class CsvRow
    {
        [Name("Title")] public string Title { get; set; } = "";
        [Name("Release Date")] public string ReleaseDate { get; set; } = "";
        [Name("Actors")] public string Actors { get; set; } = "";
    }
}
