namespace InvoiceSystem.Application.Dtos.Account;

// 一覧 1行（フロントが期待する命名に寄せる）
public record AccountInvoiceListItemDto(
    long Id,
    string InvoiceNumber,
    DateTime IssuedAt,
    DateTime DueAt,
    decimal TotalAmount,
    string StatusName,
    bool IsOverdue
);

public record AccountInvoiceListDto(
    int Year,
    IReadOnlyList<int> AvailableYears,
    string Month,      // "all" or "1".."12"
    string Status,     // "all"|"unpaid"|"partial"|"paid"
    string Q,
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<AccountInvoiceListItemDto> Items
);

