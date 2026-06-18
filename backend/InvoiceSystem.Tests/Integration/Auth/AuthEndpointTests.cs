using InvoiceSystem.Domain.Entities;
using InvoiceSystem.Domain.Enums;
using InvoiceSystem.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace InvoiceSystem.Tests.Integration.Auth;

public class AuthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_ConfirmedUser_ReturnsToken()
    {
        // Arrange
        using var client = _factory.CreateClient();

        await SeedMemberAsync(
            email: "login-ok@example.com",
            password: "Password123!",
            isEmailConfirmed: true,
            role: MemberRole.Customer
        );

        // Act
        var res = await client.PostAsJsonAsync("/auth/login", new
        {
            email = "login-ok@example.com",
            password = "Password123!"
        });

        var raw = await res.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;

        Assert.Equal("login-ok@example.com", root.GetProperty("email").GetString());
        Assert.Equal("Member", root.GetProperty("role").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("token").GetString()));
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        // Arrange
        using var client = _factory.CreateClient();

        await SeedMemberAsync(
            email: "wrong-password@example.com",
            password: "Password123!",
            isEmailConfirmed: true,
            role: MemberRole.Customer
        );

        // Act
        var res = await client.PostAsJsonAsync("/auth/login", new
        {
            email = "wrong-password@example.com",
            password = "WrongPassword!"
        });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Login_UnconfirmedUser_Returns400()
    {
        // Arrange
        using var client = _factory.CreateClient();

        await SeedMemberAsync(
            email: "unconfirmed@example.com",
            password: "Password123!",
            isEmailConfirmed: false,
            role: MemberRole.Customer
        );

        // Act
        var res = await client.PostAsJsonAsync("/auth/login", new
        {
            email = "unconfirmed@example.com",
            password = "Password123!"
        });

        var raw = await res.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Contains("メールアドレスの確認が完了していません。", raw);
    }

    private async Task SeedMemberAsync(
        string email,
        string password,
        bool isEmailConfirmed,
        MemberRole role)
    {
        using var scope = _factory.Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Member>>();

        var existing = db.Members.FirstOrDefault(x => x.Email == email);
        if (existing is not null)
        {
            db.Members.Remove(existing);
            await db.SaveChangesAsync();
        }

        var member = new Member
        {
            Name = "Test User",
            Email = email,
            PasswordHash = "",
            Role = role,
            IsActive = true,
            IsEmailConfirmed = isEmailConfirmed,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        member.PasswordHash = hasher.HashPassword(member, password);

        db.Members.Add(member);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task VerifyEmail_ValidToken_Returns200AndConfirmsEmail()
    {
        // Arrange
        using var client = _factory.CreateClient();

        var token = "verify-token-001";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            db.Members.Add(new Member
            {
                Name = "Verify User",
                Email = "verify@example.com",
                PasswordHash = "dummy",
                Role = MemberRole.Customer,
                IsActive = true,
                IsEmailConfirmed = false,
                EmailVerificationToken = token,
                EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(1),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }

        // Act
        var res = await client.PostAsJsonAsync("/auth/verify-email", new
        {
            token
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();

        var member = await db2.Members
            .FirstAsync(x => x.Email == "verify@example.com");

        Assert.True(member.IsEmailConfirmed);
        Assert.Null(member.EmailVerificationToken);
        Assert.Null(member.EmailVerificationTokenExpiresAt);
    }
}