namespace InvoiceSystem.Application.Services.Sales;

public sealed class SalesSearchQuery
{
    public int Year { get; init; }
    public int? Month { get; init; } // ★int?（null=all）
    public string? Keyword { get; init; }
    public string Status { get; init; } = "all"; // all/unpaid/partial/paid
    public long? MemberId { get; init; }
}

