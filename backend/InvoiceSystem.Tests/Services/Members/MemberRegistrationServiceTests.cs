using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InvoiceSystem.Application.Commands.Members;
using InvoiceSystem.Application.Common.Interfaces;
using InvoiceSystem.Application.Services.Members;
using InvoiceSystem.Domain.Entities;
using InvoiceSystem.Domain.Enums;
using InvoiceSystem.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace InvoiceSystem.Tests.Services.Members;

public sealed class MemberRegistrationServiceTests
{
    [Fact]
    public async Task RegisterAsync_NewEmail_CreatesMemberAndSendsVerificationEmail()
    {
        // Arrange
        await using var db = CreateDbContext();

        var emailSender = new FakeEmailSender();
        var service = CreateService(db, emailSender);

        var command = new RegisterMemberCommand
        {
            Name = "テスト太郎",
            Email = "test@example.com",
            Password = "Password123!",
            PostalCode = "100-0001",
            Address = "東京都千代田区",
            Phone = "090-0000-0000",
            Role = null
        };

        // Act
        var result = await service.RegisterAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);

        var member = await db.Members.SingleAsync();

        Assert.Equal("テスト太郎", member.Name);
        Assert.Equal("test@example.com", member.Email);
        Assert.Equal("100-0001", member.PostalCode);
        Assert.Equal("東京都千代田区", member.Address);
        Assert.Equal("090-0000-0000", member.Phone);

        Assert.Equal(MemberRole.Customer, member.Role);
        Assert.True(member.IsActive);
        Assert.False(member.IsEmailConfirmed);

        Assert.False(string.IsNullOrWhiteSpace(member.EmailVerificationToken));
        Assert.NotNull(member.EmailVerificationTokenExpiresAt);
        Assert.True(member.EmailVerificationTokenExpiresAt > DateTime.UtcNow);

        Assert.False(string.IsNullOrWhiteSpace(member.PasswordHash));
        Assert.NotEqual("Password123!", member.PasswordHash);

        var verifyResult = new PasswordHasher<Member>()
            .VerifyHashedPassword(member, member.PasswordHash, "Password123!");

        Assert.Equal(PasswordVerificationResult.Success, verifyResult);

        Assert.Single(emailSender.SentEmails);

        var sent = emailSender.SentEmails.Single();
        Assert.Equal("test@example.com", sent.To);
        Assert.Contains("メールアドレスの確認", sent.Subject);
        Assert.Contains("http://localhost:3000/auth/verify-email?token=", sent.Body);
        Assert.Contains(Uri.EscapeDataString(member.EmailVerificationToken!), sent.Body);
    }

    [Fact]
    public async Task RegisterAsync_RoleSpecified_UsesSpecifiedRole()
    {
        // Arrange
        await using var db = CreateDbContext();

        var service = CreateService(db, new FakeEmailSender());

        var command = new RegisterMemberCommand
        {
            Name = "管理者",
            Email = "admin@example.com",
            Password = "Password123!",
            Role = MemberRole.Admin
        };

        // Act
        var result = await service.RegisterAsync(command);

        // Assert
        Assert.True(result.IsSuccess);

        var member = await db.Members.SingleAsync();
        Assert.Equal(MemberRole.Admin, member.Role);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ReturnsFailAndDoesNotSendEmail()
    {
        // Arrange
        await using var db = CreateDbContext();

        db.Members.Add(new Member
        {
            Id = 1,
            Name = "既存ユーザー",
            Email = "duplicate@example.com",
            PasswordHash = "old-hash",
            Role = MemberRole.Customer,
            IsActive = true,
            IsEmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var emailSender = new FakeEmailSender();
        var service = CreateService(db, emailSender);

        var command = new RegisterMemberCommand
        {
            Name = "新規ユーザー",
            Email = "duplicate@example.com",
            Password = "Password123!",
            Role = MemberRole.Customer
        };

        // Act
        var result = await service.RegisterAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("このメールアドレスは既に登録されています。", result.Error);

        Assert.Equal(1, await db.Members.CountAsync());
        Assert.Empty(emailSender.SentEmails);
    }

    [Fact]
    public async Task RegisterAsync_NewEmail_UsesConfiguredFrontendBaseUrl()
    {
        // Arrange
        await using var db = CreateDbContext();

        var emailSender = new FakeEmailSender();

        var service = CreateService(
            db,
            emailSender,
            frontendBaseUrl: "https://example-frontend.com"
        );

        var command = new RegisterMemberCommand
        {
            Name = "テスト太郎",
            Email = "test@example.com",
            Password = "Password123!",
            Role = null
        };

        // Act
        var result = await service.RegisterAsync(command);

        // Assert
        Assert.True(result.IsSuccess);

        var sent = emailSender.SentEmails.Single();
        Assert.Contains(
            "https://example-frontend.com/auth/verify-email?token=",
            sent.Body
        );
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static MemberRegistrationService CreateService(
        IAppDbContext db,
        IEmailSender emailSender,
        string frontendBaseUrl = "http://localhost:3000")
    {
        var passwordHasher = new PasswordHasher<Member>();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Frontend:BaseUrl"] = frontendBaseUrl
            })
            .Build();

        return new MemberRegistrationService(
            db,
            passwordHasher,
            emailSender,
            config
        );
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