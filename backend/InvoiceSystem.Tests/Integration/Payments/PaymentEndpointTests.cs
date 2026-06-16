using InvoiceSystem.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Xunit;

namespace InvoiceSystem.Tests.Integration.Payments;

public class PaymentEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PaymentEndpointTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("SEED_DEMO_DATA", "true");
        _factory = factory;
    }

    [Fact]
    public async Task Unauthenticated_GetPayments_Returns401()
    {
        var client = _factory.CreateClient();

        var res = await client.GetAsync("/api/payments");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task MemberToken_GetPayments_Returns403()
    {
        var client = _factory.CreateClient();

        var token = await LoginAndGetTokenAsync(
            client,
            "member@example.com",
            "Member1234!"
        );

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/payments");

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task AdminToken_GetPayments_Returns200()
    {
        var client = _factory.CreateClient();

        var token = await LoginAndGetTokenAsync(
            client,
            "admin@example.com",
            "Admin1234!"
        );

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/payments");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task AdminToken_CreatePayment_InvalidMemberId_Returns400()
    {
        var client = _factory.CreateClient();

        var token = await LoginAndGetTokenAsync(
            client,
            "admin@example.com",
            "Admin1234!"
        );

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var res = await client.PostAsJsonAsync("/api/payments", new
        {
            memberId = 0,
            amount = 1000,
            paymentDate = DateTime.Today,
            method = "BankTransfer",
            note = "test"
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task AdminToken_CreatePayment_InvalidAmount_Returns400()
    {
        var client = _factory.CreateClient();

        var token = await LoginAndGetTokenAsync(
            client,
            "admin@example.com",
            "Admin1234!"
        );

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var res = await client.PostAsJsonAsync("/api/payments", new
        {
            memberId = 1,
            amount = 0,
            paymentDate = DateTime.Today,
            method = "BankTransfer",
            note = "test"
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task AdminToken_CreatePayment_ValidRequest_Returns201()
    {
        // Arrange
        var client = _factory.CreateClient();

        var token = await LoginAndGetTokenAsync(
            client,
            "admin@example.com",
            "Admin1234!"
        );

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var memberId = await GetActiveMemberIdAsync();

        // Act
        var res = await client.PostAsJsonAsync("/api/payments", new
        {
            memberId,
            amount = 5000,
            paymentDate = DateTime.Today,
            method = "BankTransfer",
            note = "integration test payment"
        });

        // Assert
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        Assert.NotNull(res.Headers.Location);

        var json = await res.Content.ReadFromJsonAsync<CreatePaymentResponse>();

        Assert.NotNull(json);
        Assert.True(json!.Id > 0);
    }

    [Fact]
    public async Task AdminToken_GetPaymentById_Exists_Returns200()
    {
        // Arrange
        var client = _factory.CreateClient();

        var token = await LoginAndGetTokenAsync(
            client,
            "admin@example.com",
            "Admin1234!"
        );

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var memberId = await GetActiveMemberIdAsync();

        var createRes = await client.PostAsJsonAsync("/api/payments", new
        {
            memberId,
            amount = 7000,
            paymentDate = DateTime.Today,
            method = "BankTransfer",
            note = "integration test payment detail"
        });

        createRes.EnsureSuccessStatusCode();

        var created = await createRes.Content.ReadFromJsonAsync<CreatePaymentResponse>();

        Assert.NotNull(created);
        Assert.True(created!.Id > 0);

        // Act
        var res = await client.GetAsync($"/api/payments/{created.Id}");

        // Assert
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

    private async Task<long> GetActiveMemberIdAsync()
    {
        using var scope = _factory.Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var memberId = await db.Members
            .AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.Id)
            .Select(m => m.Id)
            .FirstOrDefaultAsync();

        Assert.True(memberId > 0, "Active member seed data was not found.");

        return memberId;
    }

    private sealed class CreatePaymentResponse
    {
        public long Id { get; set; }
    }
}