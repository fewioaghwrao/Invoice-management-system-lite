namespace InvoiceSystem.Application.Dtos.Sales;

public record SalesByMemberRowDto(
    long MemberId,
    string MemberName,
    decimal InvoiceTotal,
    decimal PaidTotal,
    decimal RemainingTotal,
    decimal RecoveryRate
);

public record SalesByMemberResultDto(
    int Year,
    string Month,           // "all" or "1".."12"
    string Keyword,
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<SalesByMemberRowDto> Rows,
    SalesSummaryDto Summary // ← /api/sales と共通
);

