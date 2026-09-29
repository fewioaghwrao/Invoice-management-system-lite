namespace InvoiceSystem.Application.Dtos.AccessExport;

public sealed record AccessPaymentExportRow(
    long PaymentId,
    long MemberId,
    DateTime PaymentDate,
    decimal Amount,
    string? PayerName,
    string? Method
);
