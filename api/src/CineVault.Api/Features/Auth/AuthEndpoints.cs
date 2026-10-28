using System.ComponentModel.DataAnnotations;
using CineVault.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace CineVault.Api.Features.Auth;

public record RegisterRequest(
    [property: Required, EmailAddress, MaxLength(256)] string Email,
    [property: Required, StringLength(128, MinimumLength = 8)] string Password,
    [property: Required, StringLength(100, MinimumLength = 2)] string DisplayName);

public record LoginRequest(
    [property: Required, MaxLength(256)] string Email,
    [property: Required, MaxLength(128)] string Password);

public record UserResponse(Guid Id, string Email, string DisplayName, string Role);

public record AuthResponse(string Token, DateTime ExpiresAt, UserResponse User);

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuth(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", RegisterAsync).RequireRateLimiting("auth");
        group.MapPost("/login", LoginAsync).RequireRateLimiting("auth");
        group.MapGet("/me", MeAsync).RequireAuthorization();

        return group;
    }

    private static async Task<Results<Created<AuthResponse>, ProblemHttpResult, ValidationProblem>> RegisterAsync(
        RegisterRequest request, CineVaultDbContext db, TokenService tokens, CancellationToken ct)
    {
        if (!HasLetterAndDigit(request.Password))
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["password"] = ["Use at least one letter and one digit."]
            });

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            return TypedResults.Problem("Sign in instead, or use another email.", statusCode: StatusCodes.Status409Conflict, title: "Email already registered");

        var user = new User
        {
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created("/api/auth/me", Respond(user, tokens));
    }

    private static async Task<Results<Ok<AuthResponse>, UnauthorizedHttpResult>> LoginAsync(
        LoginRequest request, CineVaultDbContext db, TokenService tokens, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return TypedResults.Unauthorized();

        return TypedResults.Ok(Respond(user, tokens));
    }

    private static async Task<Results<Ok<UserResponse>, UnauthorizedHttpResult>> MeAsync(
        HttpContext http, CineVaultDbContext db, CancellationToken ct)
    {
        var id = http.User.UserId();
        var user = id is null ? null : await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
        return user is null ? TypedResults.Unauthorized() : TypedResults.Ok(ToResponse(user));
    }

    private static AuthResponse Respond(User user, TokenService tokens)
    {
        var (token, expires) = tokens.Issue(user);
        return new AuthResponse(token, expires, ToResponse(user));
    }

    private static UserResponse ToResponse(User u) => new(u.Id, u.Email, u.DisplayName, u.Role.ToString());

    private static bool HasLetterAndDigit(string s) => s.Any(char.IsLetter) && s.Any(char.IsDigit);
}
