using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using InvoiceSystem.Domain.Entities;
using InvoiceSystem.Domain.Enums;
using InvoiceSystem.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace InvoiceSystem.Tests.Integration.Members;

public sealed class MemberEndpointTests
{
    [Fact]
    public async Task NoToken_GetMembers_Returns401()
    {
        using var factory = new TestingFactory();
        using var client = factory.CreateClient();

        var res = await client.GetAsync("/api/members?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task MemberToken_GetMembers_Returns403()
    {
        using var factory = new TestingFactory();
        using var client = factory.CreateClient();

        var memberId = await SeedMemberAsync(factory, "member-list@example.com", MemberRole.Customer);

        var token = CreateJwt(
            factory.Services.GetRequiredService<IConfiguration>(),
            userId: memberId,
            role: "Member",
            email: "member-list@example.com"
        );

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/members?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task AdminToken_GetMembers_Returns200()
    {
        using var factory = new TestingFactory();
        using var client = factory.CreateClient();

        var adminId = await SeedMemberAsync(factory, "admin-list@example.com", MemberRole.Admin);

        var token = CreateJwt(
            factory.Services.GetRequiredService<IConfiguration>(),
            userId: adminId,
            role: "Admin",
            email: "admin-list@example.com"
        );

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/members?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task AdminToken_GetMemberById_NotFound_Returns404()
    {
        using var factory = new TestingFactory();
        using var client = factory.CreateClient();

        var adminId = await SeedMemberAsync(factory, "admin-member-notfound@example.com", MemberRole.Admin);

        var token = CreateJwt(
            factory.Services.GetRequiredService<IConfiguration>(),
            userId: adminId,
            role: "Admin",
            email: "admin-member-notfound@example.com"
        );

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/members/99999999");

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    private static async Task<long> SeedMemberAsync(
        WebApplicationFactory<Program> factory,
        string email,
        MemberRole role,
        bool isActive = true)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var existing = await db.Members.FirstOrDefaultAsync(x => x.Email == email);
        if (existing is not null)
            return existing.Id;

        var member = new Member
        {
            Name = "Test User",
            Email = email,
            PasswordHash = "dummy-hash",
            Role = role,
            IsActive = isActive,
            IsEmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Members.Add(member);
        await db.SaveChangesAsync();

        return member.Id;
    }

    private static string CreateJwt(
        IConfiguration config,
        long userId,
        string role,
        string email)
    {
        var jwt = config.GetSection("Jwt");

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwt["Key"]!)
        );

        var creds = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim("name", "Test User"),
            new Claim(ClaimTypes.Role, role),
        };

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private const string TestJwtKey =
        "TEST_ONLY_SUPER_SECRET_KEY_32_BYTES_MIN!!";

    private const string TestJwtIssuer = "InvoiceSystem";
    private const string TestJwtAudience = "InvoiceSystemFrontend";

    private sealed class TestingFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((context, config) =>
            {
                var connectionString =
                    Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                    ?? "Host=localhost;Port=5432;Database=invoicesystem;Username=postgres;Password=postgres";

                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = connectionString,

                    ["Jwt:Key"] = TestJwtKey,
                    ["Jwt:Issuer"] = TestJwtIssuer,
                    ["Jwt:Audience"] = TestJwtAudience,
                    ["Jwt:ExpiresMinutes"] = "60"
                });
            });
        }
    }
}