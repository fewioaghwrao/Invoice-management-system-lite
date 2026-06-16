using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using InvoiceSystem.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InvoiceSystem.Tests.Integration.MyAccount;

public class MyAccountEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MyAccountEndpointTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("SEED_DEMO_DATA", "true");
        _factory = factory;
    }

    [Fact]
    public async Task Unauthenticated_GetMyProfile_Returns401()
    {
        var client = _factory.CreateClient();

        var res = await client.GetAsync("/api/members/me");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task AdminToken_GetMyProfile_Returns403()
    {
        var client = _factory.CreateClient();

        var token = await LoginAndGetTokenAsync(
            client,
            "admin@example.com",
            "Admin1234!"
        );

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/members/me");

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task MemberToken_GetMyProfile_Returns200()
    {
        var client = _factory.CreateClient();

        var token = await RegisterAndLoginMemberAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/members/me/");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    private static async Task<string> LoginAndGetTokenAsync(
        HttpClient client,
        string email,
        string password)
    {
        var res = await client.PostAsJsonAsync("/auth/login", new
        {
            email,
            password
        });

        var body = await res.Content.ReadAsStringAsync();

        Assert.True(
            res.IsSuccessStatusCode,
            $"Login failed. Status={(int)res.StatusCode} {res.StatusCode}, Body={body}, Email={email}"
        );

        var json = await res.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(json);
        Assert.False(string.IsNullOrWhiteSpace(json!.Token));

        return json.Token;
    }

    private async Task<string> RegisterAndLoginMemberAsync(HttpClient client)
    {
        var email = $"member-{Guid.NewGuid():N}@example.com";
        var password = "Member1234!";

        var registerRes = await client.PostAsJsonAsync("/auth/register", new
        {
            name = "Integration Test Member",
            email,
            password,
            postalCode = "1000001",
            address = "Tokyo",
            phone = "09000000000"
        });

        var registerBody = await registerRes.Content.ReadAsStringAsync();

        Assert.True(
            registerRes.IsSuccessStatusCode,
            $"Register failed. Status={(int)registerRes.StatusCode} {registerRes.StatusCode}, Body={registerBody}"
        );

        // ★ テスト用にメール確認済みにする
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var member = await db.Members
                .FirstOrDefaultAsync(m => m.Email == email);

            Assert.NotNull(member);

            member!.IsEmailConfirmed = true;
            member.EmailVerificationToken = null;
            member.EmailVerificationTokenExpiresAt = null;
            member.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
        }

        return await LoginAndGetTokenAsync(client, email, password);
    }

    private sealed class LoginResponse
    {
        public string Token { get; set; } = "";
    }
}