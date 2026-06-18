using InvoiceSystem.Domain.Entities;
using InvoiceSystem.Domain.Enums;
using InvoiceSystem.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
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


        var token = CreateJwtToken(memberId: 1, role: "Admin");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/members/me");

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task MemberToken_GetMyProfile_Returns200()
    {
        var client = _factory.CreateClient();

        var memberId = await CreateActiveMemberAsync();

        var token = CreateJwtToken(memberId, "Member");

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

    private string CreateJwtToken(long memberId, string role)
    {
        using var scope = _factory.Services.CreateScope();

        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var jwtSection = config.GetSection("Jwt");

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSection["Key"]!)
        );

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, memberId.ToString()),
        new Claim("memberId", memberId.ToString()),
        new Claim(ClaimTypes.Role, role),
        new Claim("role", role)
    };

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    private async Task<long> CreateActiveMemberAsync()
    {
        using var scope = _factory.Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var member = new Member
        {
            Name = "Integration Test Member",
            Email = $"member-{Guid.NewGuid():N}@example.com",
            PasswordHash = "dummy",
            IsActive = true,
            IsEmailConfirmed = true,
            PostalCode = "1000001",
            Address = "Tokyo",
            Phone = "09000000000",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Members.Add(member);
        await db.SaveChangesAsync();

        return member.Id;
    }
}