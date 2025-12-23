namespace InvoiceSystem.Application.Queries.Sales;

public class SalesSearchRequest
{
    public int? Year { get; set; }
    public int? Month { get; set; }     // null = all
    public string? Q { get; set; }
    public string? Status { get; set; } // all/unpaid/partial/paid
    public int? Page { get; set; }
    public int? PageSize { get; set; }

    public long? MemberId { get; set; }
}
