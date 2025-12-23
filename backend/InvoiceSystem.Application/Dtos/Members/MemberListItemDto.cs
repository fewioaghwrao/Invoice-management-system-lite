using InvoiceSystem.Domain.Enums;

namespace InvoiceSystem.Application.Dtos.Members;

public class MemberListItemDto
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;

    public MemberRole Role { get; set; }   // 追加したならここ

    public bool IsActive { get; set; }
}

