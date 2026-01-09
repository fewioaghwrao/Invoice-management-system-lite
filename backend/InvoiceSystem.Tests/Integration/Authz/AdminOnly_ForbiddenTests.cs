using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace InvoiceSystem.Tests.Integration.Authz;

public class AdminOnly_ForbiddenTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AdminOnly_ForbiddenTests(WebApplicationFactory<Program> factory)
    {
        // ★デモユーザー投入を必ず有効化（CIでも安定）
        Environment.SetEnvironmentVariable("SEED_DEMO_DATA", "true");

        _factory = factory;
    }

    [Fact]
    public async Task MemberToken_CannotAccess_AdminOnly_Endpoint_Returns403()
    {
        // Arrange
        var client = _factory.CreateClient();

        // ① MemberでログインしてJWT取得
        var loginRes = await client.PostAsJsonAsync("/auth/login", new
        {
            email = "member@example.com",
            password = "Member1234!"
        });

        loginRes.EnsureSuccessStatusCode();

        var login = await loginRes.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(login);
        Assert.False(string.IsNullOrWhiteSpace(login!.Token));

        // ② Authorization: Bearer {token}
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        // Act：AdminOnly endpoint を叩く
        var res = await client.GetAsync("/api/admin/summary?year=2026");

        // Assert：Memberなので403
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    private sealed record LoginResponse(
        long Id,
        string Name,
        string Email,
        string Role,
        string Token
    );
}

