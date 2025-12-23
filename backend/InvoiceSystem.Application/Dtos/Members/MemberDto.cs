using InvoiceSystem.Domain.Enums;

namespace InvoiceSystem.Application.Dtos.Members;

public class MemberDto
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? PostalCode { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }

    public MemberRole Role { get; set; }   // ★追加

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}

