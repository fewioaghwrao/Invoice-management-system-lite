namespace InvoiceSystem.Application.Dtos.Sales;

public record SalesRowDto(
    long InvoiceId,
    string InvoiceNumber,
    string ClientName,
    DateTime IssuedAt,
    DateTime DueAt,
    string Status,          // "UNPAID" | "PARTIAL" | "PAID"
    decimal InvoiceAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    DateTime? LastPaidAt
);

public record SalesSummaryDto(
    decimal InvoiceTotal,
    decimal PaidTotal,
    decimal RemainingTotal,
    decimal RecoveryRate
);

public record SalesListResultDto(
    int Year,
    string Month,           // "all" or "1".."12"
    string Keyword,
    string Status,          // "all"|"unpaid"|"partial"|"paid"
    int Page,
    int PageSize,
    int TotalCount,

    string? MemberName,     // ★追加（memberId 指定時のみ入る）
    IReadOnlyList<SalesRowDto> Rows,
    SalesSummaryDto Summary
);
