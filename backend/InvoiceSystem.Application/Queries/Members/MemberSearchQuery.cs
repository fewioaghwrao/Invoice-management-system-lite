using InvoiceSystem.Domain.Enums;

namespace InvoiceSystem.Application.Queries.Members;

public class MemberSearchQuery
{
    public string? Keyword { get; set; }   // 名前 or メールアドレス
    public MemberRole? Role { get; set; }  // 管理者/会員/退会
    public bool? IsActive { get; set; }    // 有効/無効

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

