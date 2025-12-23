using InvoiceSystem.Domain.Enums;

namespace InvoiceSystem.Application.Commands.Members;

public sealed class UpdateMemberCommand
{
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? PostalCode { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }

    public MemberRole Role { get; set; }
    public bool IsActive { get; set; }
}

