using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace InvoiceSystem.Tests.Integration.Sales;

public class SalesEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SalesEndpointTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("SEED_DEMO_DATA", "true");
        _factory = factory;
    }

    [Fact]
    public async Task Unauthenticated_GetSales_Returns401()
    {
        var client = _factory.CreateClient();

        var res = await client.GetAsync("/api/sales/");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task MemberToken_GetSales_Returns403()
    {
        var client = _factory.CreateClient();

        var token = CreateJwtToken(memberId: 999, role: "Member");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/sales/");

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task AdminToken_GetSales_Returns200()
    {
        var client = _factory.CreateClient();

        var token = CreateJwtToken(memberId: 1, role: "Admin");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/sales/");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task AdminToken_GetSalesByMember_Returns200()
    {
        var client = _factory.CreateClient();

        var token = CreateJwtToken(memberId: 1, role: "Admin");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/sales/by-member");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task AdminToken_GetWorstTop5_Returns200()
    {
        var client = _factory.CreateClient();

        var token = CreateJwtToken(memberId: 1, role: "Admin");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/sales/by-member/worst-top5");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task AdminToken_ExportSalesCsv_ReturnsCsv()
    {
        var client = _factory.CreateClient();

        var token = CreateJwtToken(memberId: 1, role: "Admin");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/sales/export");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.StartsWith("text/csv", res.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task AdminToken_ExportSalesByMemberCsv_ReturnsCsv()
    {
        var client = _factory.CreateClient();

        var token = CreateJwtToken(memberId: 1, role: "Admin");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/sales/by-member/export");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.StartsWith("text/csv", res.Content.Headers.ContentType?.MediaType);
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
}