namespace InvoiceSystem.Application.Dtos.AccessExport;

public sealed record AccessInvoiceExportRow(
    long InvoiceId,
    long MemberId,
    string InvoiceNumber,
    DateTime InvoiceDate,
    DateTime DueDate,
    decimal TotalAmount,
    string StatusCode,
    string StatusName,
    bool IsOverdue,
    bool IsClosed,
    string MemberName
);
