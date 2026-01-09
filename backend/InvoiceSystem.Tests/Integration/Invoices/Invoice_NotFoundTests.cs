using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace InvoiceSystem.Tests.Integration.Invoices;

public class Invoice_NotFoundTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public Invoice_NotFoundTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("SEED_DEMO_DATA", "true");
        _factory = factory;
    }

    [Fact]
    public async Task AdminToken_GetInvoiceById_NotExists_Returns404()
    {
        // Arrange
        var client = _factory.CreateClient();

        var token = await LoginAndGetTokenAsync(client,
            email: "admin@example.com",
            password: "Admin1234!"
        );

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        const long notExistsId = 99999999;

        // Act
        var res = await client.GetAsync($"/api/invoices/{notExistsId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    private static async Task<string> LoginAndGetTokenAsync(HttpClient client, string email, string password)
    {
        var loginRes = await client.PostAsJsonAsync("/auth/login", new { email, password });
        var raw = await loginRes.Content.ReadAsStringAsync();

        Assert.True(loginRes.IsSuccessStatusCode, $"Login failed. Status={(int)loginRes.StatusCode}. Body={raw}");

        var token = ExtractToken(raw);
        Assert.False(string.IsNullOrWhiteSpace(token), $"Token not found in login response: {raw}");

        return token!;
    }

    private static string? ExtractToken(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            // ルート直下
            foreach (var key in new[] { "accessToken", "AccessToken", "token", "Token", "jwt", "Jwt", "access_token" })
            {
                if (root.ValueKind == JsonValueKind.Object &&
                    root.TryGetProperty(key, out var v) &&
                    v.ValueKind == JsonValueKind.String)
                {
                    return v.GetString();
                }
            }

            // ネスト（data/result/payload）
            foreach (var containerKey in new[] { "data", "result", "payload" })
            {
                if (root.ValueKind == JsonValueKind.Object &&
                    root.TryGetProperty(containerKey, out var nested) &&
                    nested.ValueKind == JsonValueKind.Object)
                {
                    foreach (var key in new[] { "accessToken", "AccessToken", "token", "Token", "jwt", "Jwt", "access_token" })
                    {
                        if (nested.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String)
                            return v.GetString();
                    }
                }
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

