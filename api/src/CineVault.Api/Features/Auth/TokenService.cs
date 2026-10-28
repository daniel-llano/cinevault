using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CineVault.Api.Data;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CineVault.Api.Features.Auth;

public sealed class JwtOptions
{
    public const string Section = "Jwt";

    public string Secret { get; set; } = "";
    public string Issuer { get; set; } = "CineVault";
    public string Audience { get; set; } = "CineVault";
    public int AccessTokenMinutes { get; set; } = 120;

    public SymmetricSecurityKey SigningKey => new(Encoding.UTF8.GetBytes(Secret));
}

public sealed class TokenService(IOptions<JwtOptions> options, TimeProvider clock)
{
    private readonly JwtOptions _options = options.Value;

    public (string Token, DateTime ExpiresAt) Issue(User user)
    {
        var expires = clock.GetUtcNow().UtcDateTime.AddMinutes(_options.AccessTokenMinutes);
        var token = new JwtSecurityToken(
            _options.Issuer,
            _options.Audience,
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Name, user.DisplayName),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ],
            expires: expires,
            signingCredentials: new SigningCredentials(_options.SigningKey, SecurityAlgorithms.HmacSha256));
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}

public static class ClaimsPrincipalExtensions
{
    public static Guid? UserId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id)
            ? id
            : null;
}
