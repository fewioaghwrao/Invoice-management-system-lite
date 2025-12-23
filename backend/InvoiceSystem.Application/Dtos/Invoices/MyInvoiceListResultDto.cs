namespace InvoiceSystem.Application.Dtos.Invoices;

public record MyInvoiceListResultDto(
    int Year,
    IReadOnlyList<int> AvailableYears,
    string Month,     // "all" or "1".."12"
    string Status,    // "all" | "unpaid" | "partial" | "paid"
    string Q,
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<MyInvoiceListItemDto> Items
);

