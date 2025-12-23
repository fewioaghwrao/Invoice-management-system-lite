using InvoiceSystem.Domain.Enums;

namespace InvoiceSystem.Domain.Entities;

public class Member
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? PostalCode { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }

    public string PasswordHash { get; set; } = null!;
    // 退会しても残しておきたいなら IsActive と Role を両方持たせてOK
    public bool IsActive { get; set; } = true;

    // ★ 追加：会員種別（管理者 / 会員 / 退会）
    public MemberRole Role { get; set; } = MemberRole.Customer;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // ナビゲーションプロパティ
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();


    // ★ メール認証用の追加プロパティ
    public bool IsEmailConfirmed { get; set; } = false;

    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationTokenExpiresAt { get; set; }
}

