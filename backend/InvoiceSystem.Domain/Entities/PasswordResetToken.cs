using System;

namespace InvoiceSystem.Domain.Entities;

public class PasswordResetToken
{
    public long Id { get; set; }

    public long MemberId { get; set; }
    public Member Member { get; set; } = null!;

    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UsedAt { get; set; }
}


