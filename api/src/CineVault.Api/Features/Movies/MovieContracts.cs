using System.ComponentModel.DataAnnotations;

namespace CineVault.Api.Features.Movies;

public enum MovieSort
{
    Title,
    Newest,
    Oldest,
    TopRated
}

public record MovieSummary(int Id, string Title, DateOnly ReleaseDate, string Cast, double? AverageRating, int RatingCount);

public record MovieDetails(
    int Id,
    string Title,
    DateOnly ReleaseDate,
    string? Overview,
    IReadOnlyList<string> Cast,
    double? AverageRating,
    int RatingCount,
    int? MyRating);

public record PagedResponse<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize, long ElapsedMs)
{
    public int TotalPages => (int)Math.Ceiling(Total / (double)PageSize);
}

public record SaveMovieRequest(
    [property: Required, StringLength(300, MinimumLength = 1)] string Title,
    [property: Required] DateOnly? ReleaseDate,
    [property: StringLength(2000)] string? Overview,
    [property: MaxLength(30)] IReadOnlyList<string>? Cast);

public record RateMovieRequest([property: Range(1, 5)] int Stars);

public record RatingSummary(double? AverageRating, int RatingCount, int? MyRating);
