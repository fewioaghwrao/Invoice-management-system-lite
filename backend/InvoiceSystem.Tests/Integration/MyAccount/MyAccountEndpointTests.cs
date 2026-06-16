using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
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

        var token = await LoginAndGetTokenAsync(
            client,
            "member@example.com",
            "Member1234!"
        );

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/members/me");

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

        res.EnsureSuccessStatusCode();

        var json = await res.Content.ReadFromJsonAsync<LoginResponse>();

        return json!.Token;
    }

    private sealed class LoginResponse
    {
        public string Token { get; set; } = "";
    }
}