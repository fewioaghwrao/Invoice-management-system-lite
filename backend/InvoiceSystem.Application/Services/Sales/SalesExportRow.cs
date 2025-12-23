namespace InvoiceSystem.Application.Services.Sales;

public sealed class SalesExportRow
{
    public long InvoiceId { get; init; }
    public string InvoiceNumber { get; init; } = "";
    public string ClientName { get; init; } = "";
    public DateTime IssuedAt { get; init; }
    public DateTime DueAt { get; init; }
    public string Status { get; init; } = "";
    public decimal InvoiceAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal RemainingAmount { get; init; }
    public DateTime? LastPaidAt { get; init; }
}

