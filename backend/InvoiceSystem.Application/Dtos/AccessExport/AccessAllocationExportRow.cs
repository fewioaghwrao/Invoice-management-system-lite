namespace InvoiceSystem.Application.Dtos.AccessExport;

public sealed record AccessAllocationExportRow(
    long AllocationId,
    long PaymentId,
    long InvoiceId,
    decimal Amount
);
