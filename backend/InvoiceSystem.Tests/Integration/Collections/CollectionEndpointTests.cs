using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
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

namespace InvoiceSystem.Tests.Integration.Collections;

public sealed class CollectionEndpointTests
{
    [Fact]
    public async Task NoToken_GetSnapshot_Returns401()
    {
        using var factory = new TestingFactory();
        using var client = factory.CreateClient();

        var res = await client.GetAsync("/api/collections/1/snapshot");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task MemberToken_GetSnapshot_Returns403()
    {
        using var factory = new TestingFactory();
        using var client = factory.CreateClient();

        var token = CreateJwt(
            factory.Services.GetRequiredService<IConfiguration>(),
            role: "Member"
        );

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/collections/1/snapshot");

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task AdminToken_GetSnapshot_Returns200()
    {
        using var factory = new TestingFactory();
        using var client = factory.CreateClient();

        var invoiceId = await SeedInvoiceAsync(factory);

        var token = CreateJwt(
            factory.Services.GetRequiredService<IConfiguration>(),
            role: "Admin"
        );

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync($"/api/collections/{invoiceId}/snapshot");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var raw = await res.Content.ReadAsStringAsync();
        Assert.Contains("INV-COL-001", raw);
    }

    [Fact]
    public async Task AdminToken_PostLog_Returns200AndId()
    {
        using var factory = new TestingFactory();
        using var client = factory.CreateClient();

        var invoiceId = await SeedInvoiceAsync(factory);

        var token = CreateJwt(
            factory.Services.GetRequiredService<IConfiguration>(),
            role: "Admin"
        );

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var res = await client.PostAsJsonAsync($"/api/collections/{invoiceId}/logs", new
        {
            channel = "PHONE",
            tone = "NORMAL",
            title = "電話催促",
            memo = "電話で支払い状況を確認しました。",
            nextActionDate = DateTime.UtcNow.AddDays(3),
            subject = (string?)null,
            bodyText = (string?)null
        });

        var raw = await res.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        using var doc = JsonDocument.Parse(raw);
        Assert.True(doc.RootElement.TryGetProperty("id", out var id));
        Assert.True(id.GetInt64() > 0);
    }

    [Fact]
    public async Task AdminToken_GetLogs_Returns200()
    {
        using var factory = new TestingFactory();
        using var client = factory.CreateClient();

        var invoiceId = await SeedInvoiceAsync(factory);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            db.ReminderHistories.Add(new ReminderHistory
            {
                InvoiceId = invoiceId,
                RemindedAt = DateTime.UtcNow,
                Method = "PHONE",
                Tone = "NORMAL",
                Title = "電話催促",
                Note = "確認済み",
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }

        var token = CreateJwt(
            factory.Services.GetRequiredService<IConfiguration>(),
            role: "Admin"
        );

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync($"/api/collections/{invoiceId}/logs");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var raw = await res.Content.ReadAsStringAsync();
        Assert.Contains("電話催促", raw);
    }

    private static async Task<long> SeedInvoiceAsync(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var existing = await db.Invoices
            .FirstOrDefaultAsync(x => x.InvoiceNumber == "INV-COL-001");

        if (existing is not null)
            return existing.Id;

        var member = new Member
        {
            Name = "Collection Test Customer",
            Email = "collection@example.com",
            PasswordHash = "dummy-hash",
            Role = MemberRole.Customer,
            IsActive = true,
            IsEmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Members.Add(member);
        await db.SaveChangesAsync();

        EnsureDunningStatus(db);

        var invoice = new Invoice
        {
            MemberId = member.Id,
            InvoiceNumber = "INV-COL-001",
            InvoiceDate = DateTime.UtcNow.AddDays(-10),
            DueDate = DateTime.UtcNow.AddDays(10),
            TotalAmount = 10000m,
            StatusId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        return invoice.Id;
    }

    private static void EnsureDunningStatus(AppDbContext db)
    {
        if (db.InvoiceStatuses.Any(x => x.Code == "DUNNING"))
            return;

        db.InvoiceStatuses.Add(new InvoiceStatus
        {
            Id = 6,
            Code = "DUNNING",
            Name = "催促中",
            IsOverdue = false,
            IsClosed = false,
            SortOrder = 60
        });

        db.SaveChanges();
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