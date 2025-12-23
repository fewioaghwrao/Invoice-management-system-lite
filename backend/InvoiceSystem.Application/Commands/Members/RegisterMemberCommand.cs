using InvoiceSystem.Domain.Enums;

namespace InvoiceSystem.Application.Commands.Members;

public class RegisterMemberCommand
{
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? PostalCode { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }

    public string Password { get; set; } = null!;

    // ★ 追加：管理画面から管理者を登録したいときなどに指定
    // null の場合は MemberService 側で Customer をセット
    public MemberRole? Role { get; set; }

}
