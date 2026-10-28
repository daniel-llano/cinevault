using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CineVault.Api.Data;

/// <summary>
/// SQLite FTS5 table over titles and cast names. Title search goes through it instead of a LIKE scan.
/// </summary>
public class SearchIndex(CineVaultDbContext db)
{
    public const string Table = "MovieSearch";

    public bool IsAvailable => db.Database.IsSqlite();

    /// <summary>Turns user input into an FTS5 prefix query. Returns "" when nothing searchable is left.</summary>
    public static string ToMatchExpression(string raw)
    {
        var tokens = raw
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => new string(part.Where(ch => char.IsLetterOrDigit(ch) || ch == '\'').ToArray()))
            .Where(t => t.Length > 0)
            .Select(t => $"\"{t}\"*");
        return string.Join(' ', tokens);
    }

    public IQueryable<Movie> Match(string expression) =>
        db.Movies.FromSqlInterpolated(
            $"SELECT * FROM Movies WHERE Id IN (SELECT MovieId FROM MovieSearch WHERE MovieSearch MATCH {expression})");

    public async Task RebuildAsync(CancellationToken ct = default)
    {
        await db.Database.ExecuteSqlRawAsync($"""
            DROP TABLE IF EXISTS {Table};
            CREATE VIRTUAL TABLE {Table} USING fts5(MovieId UNINDEXED, Title, Cast, tokenize = 'unicode61 remove_diacritics 2');
            INSERT INTO {Table} (MovieId, Title, Cast)
            SELECT m.Id, m.Title, IFNULL((SELECT group_concat(c.Name, ' ') FROM CastMembers c WHERE c.MovieId = m.Id), '')
            FROM Movies m;
            """, ct);
    }

    public async Task<bool> IsInSyncAsync(CancellationToken ct = default)
    {
        var conn = (SqliteConnection)db.Database.GetDbConnection();
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT (SELECT COUNT(*) FROM sqlite_master WHERE name = '{Table}'), (SELECT COUNT(*) FROM Movies)";
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            await reader.ReadAsync(ct);
            if (reader.GetInt64(0) == 0) return false;
            var movies = reader.GetInt64(1);
            await reader.CloseAsync();
            cmd.CommandText = $"SELECT COUNT(*) FROM {Table}";
            return (long)(await cmd.ExecuteScalarAsync(ct) ?? 0L) == movies;
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    public async Task UpsertAsync(Movie movie, CancellationToken ct = default)
    {
        if (!IsAvailable) return;
        var cast = string.Join(' ', movie.Cast.OrderBy(c => c.Position).Select(c => c.Name));
        await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM MovieSearch WHERE MovieId = {movie.Id}", ct);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO MovieSearch (MovieId, Title, Cast) VALUES ({movie.Id}, {movie.Title}, {cast})", ct);
    }

    public async Task RemoveAsync(int movieId, CancellationToken ct = default)
    {
        if (!IsAvailable) return;
        await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM MovieSearch WHERE MovieId = {movieId}", ct);
    }
}
