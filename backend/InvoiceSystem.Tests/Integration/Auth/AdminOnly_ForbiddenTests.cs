using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace InvoiceSystem.Tests.Integration.Auth;

public class AdminOnly_ForbiddenTests
{
    [Fact]
    public async Task MemberToken_CannotAccess_AdminOnly_Endpoint_Returns403()
    {
        using var factory = new TestingFactory();
        using var client = factory.CreateClient();

        // ★ ログインせず、MemberロールのJWTを自前生成
        var token = CreateJwt(factory.Services.GetRequiredService<IConfiguration>(), role: "Member");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/admin/summary?year=2026");

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    private static string CreateJwt(IConfiguration config, string role)
    {
        var jwt = config.GetSection("Jwt");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "999"), //（DB未使用）
            new Claim(JwtRegisteredClaimNames.Email, "member@example.com"),
            new Claim("name", "Test Member"),
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

    private sealed class TestingFactory : WebApplicationFactory<Program>
    {
        private const string TestJwtKey =
    "TEST_ONLY_SUPER_SECRET_KEY_32_BYTES_MIN!!";

        private const string TestJwtIssuer = "InvoiceSystem";
        private const string TestJwtAudience = "InvoiceSystemFrontend";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Host=localhost;Port=5432;Database=invoicesystem;Username=postgres;Password=postgres",

                    ["Jwt:Key"] = TestJwtKey,
                    ["Jwt:Issuer"] = TestJwtIssuer,
                    ["Jwt:Audience"] = TestJwtAudience,
                    ["Jwt:ExpiresMinutes"] = "60"
                });
            });
        }
    }
}


