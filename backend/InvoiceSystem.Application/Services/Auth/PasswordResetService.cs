using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Application.Common.Interfaces;
using InvoiceSystem.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace InvoiceSystem.Application.Services.Auth;

public sealed class PasswordResetService
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher<Member> _passwordHasher;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _config;

    public PasswordResetService(
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

    // 1) 再設定メール送信
    public async Task<Result> RequestResetAsync(string email, CancellationToken ct = default)
    {
        var member = await _db.Members
            .FirstOrDefaultAsync(m => m.Email == email && m.IsActive, ct);

        // セキュリティの観点から「存在しないメールです」とは言わない
        if (member == null)
        {
            // 何もせず成功扱いにする
            return Result.Ok();
        }

        var token = GenerateToken();
        var now = DateTime.UtcNow;
        var expiresAt = now.AddHours(1); // 有効期限 1時間（お好みで）

        var reset = new PasswordResetToken
        {
            MemberId = member.Id,
            Token = token,
            CreatedAt = now,
            ExpiresAt = expiresAt
        };

        _db.PasswordResetTokens.Add(reset);
        await _db.SaveChangesAsync(ct);

        var frontendBaseUrl = _config["Frontend:BaseUrl"] ?? "http://localhost:3000";
        var resetUrl = $"{frontendBaseUrl}/auth/reset-password?token={Uri.EscapeDataString(token)}";

        var subject = "【Invoice Dashboard Lite】パスワード再設定のご案内";
        var body = $"""
            {member.Name} 様

            パスワード再設定のご依頼を受け付けました。
            以下のURLから新しいパスワードを設定してください。

            {resetUrl}

            このリンクは {expiresAt:yyyy/MM/dd HH:mm}（UTC）まで有効です。

            ※本メールに心当たりがない場合は、このメールは破棄してください。
            """;

        await _emailSender.SendAsync(member.Email, subject, body);

        return Result.Ok();
    }

    // 2) 新しいパスワードに更新
    public async Task<Result> ResetPasswordAsync(string token, string newPassword, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var reset = await _db.PasswordResetTokens
            .Include(x => x.Member)
            .FirstOrDefaultAsync(x =>
                x.Token == token &&
                x.ExpiresAt > now &&
                x.UsedAt == null, ct);

        if (reset == null)
        {
            return Result.Fail("トークンが無効、または有効期限切れです。");
        }

        var member = reset.Member;

        member.PasswordHash = _passwordHasher.HashPassword(member, newPassword);
        member.UpdatedAt = now; // プロパティがある前提

        reset.UsedAt = now;

        await _db.SaveChangesAsync(ct);

        return Result.Ok();
    }

    private static string GenerateToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    }

    public sealed record Result(bool IsSuccess, string? Error)
    {
        public static Result Ok() => new(true, null);
        public static Result Fail(string error) => new(false, error);
    }
}

