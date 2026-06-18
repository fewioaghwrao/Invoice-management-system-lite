using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace InvoiceSystem.Tests.Integration.Admin;

public class AdminOperationLogsEndpointTests
{
    [Fact]
    public async Task NoToken_GetOperationLogs_Returns401()
    {
        using var factory = new TestingFactory();
        using var client = factory.CreateClient();

        var res = await client.GetAsync("/api/admin/operation-logs?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task MemberToken_GetOperationLogs_Returns403()
    {
        using var factory = new TestingFactory();
        using var client = factory.CreateClient();

        var token = CreateJwt(
            factory.Services.GetRequiredService<IConfiguration>(),
            role: "Member"
        );

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/admin/operation-logs?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task AdminToken_GetOperationLogs_Returns200()
    {
        using var factory = new TestingFactory();
        using var client = factory.CreateClient();

        var token = CreateJwt(
            factory.Services.GetRequiredService<IConfiguration>(),
            role: "Admin"
        );

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/admin/operation-logs?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task AdminToken_GetRecentOperationLogs_Returns200()
    {
        using var factory = new TestingFactory();
        using var client = factory.CreateClient();

        var token = CreateJwt(
            factory.Services.GetRequiredService<IConfiguration>(),
            role: "Admin"
        );

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/admin/operation-logs/recent?limit=5");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    private static string CreateJwt(IConfiguration config, string role)
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
            new Claim(JwtRegisteredClaimNames.Sub, "999"),
            new Claim(JwtRegisteredClaimNames.Email, "test@example.com"),
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