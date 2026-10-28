using System.Net.Http.Headers;
using System.Net.Http.Json;
using CineVault.Api.Features.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CineVault.Api.Tests;

/// <summary>Runs the API on a throwaway SQLite file, so migrations and FTS behave like production.</summary>
public sealed class CineVaultFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@test.local";
    public const string AdminPassword = "Admin@1234";

    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"cinevault-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:CineVault", $"Data Source={_dbPath};Pooling=False");
        builder.UseSetting("Jwt:Secret", "test-only-signing-key-that-is-long-enough");
        builder.UseSetting("Bootstrap:AdminEmail", AdminEmail);
        builder.UseSetting("Bootstrap:AdminPassword", AdminPassword);
        builder.UseSetting("RateLimiting:AuthPermitsPerMinute", "10000");
    }

    public async Task<HttpClient> AdminClientAsync() => await ClientForAsync(AdminEmail, AdminPassword);

    public async Task<(HttpClient Client, AuthResponse Auth)> MemberClientAsync()
    {
        var client = CreateClient();
        var email = $"member-{Guid.NewGuid():N}@test.local";
        var resp = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Member1234", "Test Member"));
        resp.EnsureSuccessStatusCode();
        var auth = (await resp.Content.ReadFromJsonAsync<AuthResponse>(Json.Options))!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return (client, auth);
    }

    private async Task<HttpClient> ClientForAsync(string email, string password)
    {
        var client = CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        resp.EnsureSuccessStatusCode();
        var auth = (await resp.Content.ReadFromJsonAsync<AuthResponse>(Json.Options))!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try { File.Delete(_dbPath); } catch (IOException) { }
    }
}

public static class Json
{
    public static readonly System.Text.Json.JsonSerializerOptions Options = new(System.Text.Json.JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };
}
