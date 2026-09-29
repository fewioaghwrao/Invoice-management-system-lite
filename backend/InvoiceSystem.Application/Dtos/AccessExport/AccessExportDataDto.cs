namespace InvoiceSystem.Application.Dtos.AccessExport;

public sealed record AccessExportDataDto(
    int Year,
    int Month,
    IReadOnlyList<AccessInvoiceExportRow> Invoices,
    IReadOnlyList<AccessPaymentExportRow> Payments,
    IReadOnlyList<AccessAllocationExportRow> Allocations
);