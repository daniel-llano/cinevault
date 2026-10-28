namespace CineVault.Api.Data;

public class Movie
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public DateOnly ReleaseDate { get; set; }
    public string? Overview { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public List<CastMember> Cast { get; set; } = [];
    public List<Rating> Ratings { get; set; } = [];
}

public class CastMember
{
    public int Id { get; set; }
    public int MovieId { get; set; }
    public string Name { get; set; } = "";

    // Billing order as it appears in the credits.
    public int Position { get; set; }
}

public class Rating
{
    public int MovieId { get; set; }
    public Guid UserId { get; set; }
    public int Stars { get; set; }
    public DateTime RatedAt { get; set; } = DateTime.UtcNow;
}

public enum UserRole
{
    Member,
    Admin
}

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public UserRole Role { get; set; } = UserRole.Member;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
