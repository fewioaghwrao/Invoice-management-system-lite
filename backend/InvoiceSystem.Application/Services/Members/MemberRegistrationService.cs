using System;
using System.Security.Cryptography;
using InvoiceSystem.Application.Commands.Members;
using InvoiceSystem.Application.Common.Interfaces;
using InvoiceSystem.Domain.Entities;
using InvoiceSystem.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace InvoiceSystem.Application.Services.Members;

public sealed class MemberRegistrationService
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher<Member> _passwordHasher;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _config;

    public MemberRegistrationService(
        IAppDbContext db,
        IPasswordHasher<Member> passwordHasher,
        IEmailSender emailSender,
        IConfiguration config)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _emailSender = emailSender;
        _config = config;
    }

    public async Task<Result> RegisterAsync(RegisterMemberCommand command, CancellationToken ct = default)
    {
        var exists = await _db.Members
            .AnyAsync(m => m.Email == command.Email, ct);

        if (exists)
        {
            return Result.Fail("このメールアドレスは既に登録されています。");
        }

        // ★ トークン生成
        var token = GenerateToken();
        var expiresAt = DateTime.UtcNow.AddHours(24); // 24時間有効

        var member = new Member
        {
            Name = command.Name,
            Email = command.Email,
            PostalCode = command.PostalCode,
            Address = command.Address,
            Phone = command.Phone,
            Role = command.Role ?? MemberRole.Customer,
            IsActive = true,
            IsEmailConfirmed = false,
            EmailVerificationToken = token,
            EmailVerificationTokenExpiresAt = expiresAt
        };

        member.PasswordHash = _passwordHasher.HashPassword(member, command.Password);

        _db.Members.Add(member);
        await _db.SaveChangesAsync(ct);

        // ★ メール送信
        var frontendBaseUrl = _config["Frontend:BaseUrl"] ?? "http://localhost:3000";
        var verifyUrl = $"{frontendBaseUrl}/auth/verify-email?token={Uri.EscapeDataString(token)}";

        var subject = "【Invoice Dashboard Lite】メールアドレスの確認";
        var body = $"""
            {member.Name} 様

            請求書・入金ステータスダッシュボード（Lite版）へのご登録ありがとうございます。

            以下のURLをクリックして、メールアドレスの確認を完了してください。

            {verifyUrl}

            このリンクは {expiresAt:yyyy/MM/dd HH:mm}（UTC）まで有効です。

            ※本メールに心当たりがない場合は、このメールは破棄してください。
            """;

        await _emailSender.SendAsync(member.Email, subject, body);

        return Result.Ok();
    }

    private static string GenerateToken()
    {
        // URL に載せやすいトークン
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    }

    public sealed record Result(bool IsSuccess, string? Error)
    {
        public static Result Ok() => new(true, null);
        public static Result Fail(string error) => new(false, error);
    }
}




