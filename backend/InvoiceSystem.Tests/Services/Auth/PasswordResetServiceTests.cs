using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Application.Common.Interfaces;
using InvoiceSystem.Application.Services.Auth;
using InvoiceSystem.Domain.Entities;
using InvoiceSystem.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace InvoiceSystem.Tests.Services.Auth;

public sealed class PasswordResetServiceTests
{
    [Fact]
    public async Task RequestResetAsync_ActiveMemberExists_CreatesTokenAndSendsEmail()
    {
        // Arrange
        await using var db = CreateDbContext();

        var member = new Member
        {
            Id = 1,
            Name = "テスト太郎",
            Email = "test@example.com",
            PasswordHash = "old-hash",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Members.Add(member);
        await db.SaveChangesAsync();

        var emailSender = new FakeEmailSender();

        var service = CreateService(db, emailSender);

        // Act
        var result = await service.RequestResetAsync("test@example.com");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);

        var token = await db.PasswordResetTokens.SingleAsync();

        Assert.Equal(member.Id, token.MemberId);
        Assert.False(string.IsNullOrWhiteSpace(token.Token));
        Assert.Null(token.UsedAt);
        Assert.True(token.ExpiresAt > DateTime.UtcNow);

        Assert.Single(emailSender.SentEmails);

        var sent = emailSender.SentEmails.Single();
        Assert.Equal("test@example.com", sent.To);
        Assert.Contains("パスワード再設定", sent.Subject);
        Assert.Contains("http://localhost:3000/auth/reset-password?token=", sent.Body);
        Assert.Contains(Uri.EscapeDataString(token.Token), sent.Body);
    }

    [Fact]
    public async Task RequestResetAsync_EmailDoesNotExist_ReturnsOkWithoutSendingEmail()
    {
        // Arrange
        await using var db = CreateDbContext();
        var emailSender = new FakeEmailSender();

        var service = CreateService(db, emailSender);

        // Act
        var result = await service.RequestResetAsync("notfound@example.com");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);

        Assert.Empty(db.PasswordResetTokens);
        Assert.Empty(emailSender.SentEmails);
    }

    [Fact]
    public async Task RequestResetAsync_InactiveMember_ReturnsOkWithoutSendingEmail()
    {
        // Arrange
        await using var db = CreateDbContext();

        db.Members.Add(new Member
        {
            Id = 1,
            Name = "無効ユーザー",
            Email = "inactive@example.com",
            PasswordHash = "old-hash",
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var emailSender = new FakeEmailSender();
        var service = CreateService(db, emailSender);

        // Act
        var result = await service.RequestResetAsync("inactive@example.com");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(db.PasswordResetTokens);
        Assert.Empty(emailSender.SentEmails);
    }

    [Fact]
    public async Task ResetPasswordAsync_ValidToken_UpdatesPasswordAndMarksTokenAsUsed()
    {
        // Arrange
        await using var db = CreateDbContext();

        var member = new Member
        {
            Id = 1,
            Name = "テスト太郎",
            Email = "test@example.com",
            PasswordHash = "old-hash",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var resetToken = new PasswordResetToken
        {
            Id = 1,
            MemberId = member.Id,
            Member = member,
            Token = "valid-token",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            UsedAt = null
        };

        db.Members.Add(member);
        db.PasswordResetTokens.Add(resetToken);
        await db.SaveChangesAsync();

        var emailSender = new FakeEmailSender();
        var service = CreateService(db, emailSender);

        // Act
        var result = await service.ResetPasswordAsync("valid-token", "NewPassword123!");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);

        var updatedMember = await db.Members.SingleAsync();
        var updatedToken = await db.PasswordResetTokens.SingleAsync();

        Assert.NotEqual("old-hash", updatedMember.PasswordHash);
        Assert.NotNull(updatedToken.UsedAt);

        var hasher = new PasswordHasher<Member>();
        var verifyResult = hasher.VerifyHashedPassword(
            updatedMember,
            updatedMember.PasswordHash,
            "NewPassword123!");

        Assert.Equal(PasswordVerificationResult.Success, verifyResult);
    }

    [Fact]
    public async Task ResetPasswordAsync_ExpiredToken_ReturnsFail()
    {
        // Arrange
        await using var db = CreateDbContext();

        var member = new Member
        {
            Id = 1,
            Name = "テスト太郎",
            Email = "test@example.com",
            PasswordHash = "old-hash",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Members.Add(member);
        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = 1,
            MemberId = member.Id,
            Token = "expired-token",
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
            UsedAt = null
        });

        await db.SaveChangesAsync();

        var service = CreateService(db, new FakeEmailSender());

        // Act
        var result = await service.ResetPasswordAsync("expired-token", "NewPassword123!");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("トークンが無効、または有効期限切れです。", result.Error);

        var unchangedMember = await db.Members.SingleAsync();
        Assert.Equal("old-hash", unchangedMember.PasswordHash);
    }

    [Fact]
    public async Task ResetPasswordAsync_UsedToken_ReturnsFail()
    {
        // Arrange
        await using var db = CreateDbContext();

        var member = new Member
        {
            Id = 1,
            Name = "テスト太郎",
            Email = "test@example.com",
            PasswordHash = "old-hash",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Members.Add(member);
        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = 1,
            MemberId = member.Id,
            Token = "used-token",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            UsedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var service = CreateService(db, new FakeEmailSender());

        // Act
        var result = await service.ResetPasswordAsync("used-token", "NewPassword123!");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("トークンが無効、または有効期限切れです。", result.Error);

        var unchangedMember = await db.Members.SingleAsync();
        Assert.Equal("old-hash", unchangedMember.PasswordHash);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static PasswordResetService CreateService(
        IAppDbContext db,
        IEmailSender emailSender)
    {
        var passwordHasher = new PasswordHasher<Member>();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Frontend:BaseUrl"] = "http://localhost:3000"
            })
            .Build();

        return new PasswordResetService(
            db,
            passwordHasher,
            emailSender,
            config);
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public List<SentEmail> SentEmails { get; } = new();

        public Task SendAsync(string to, string subject, string body)
        {
            SentEmails.Add(new SentEmail(to, subject, body));
            return Task.CompletedTask;
        }
    }

    private sealed record SentEmail(string To, string Subject, string Body);
}