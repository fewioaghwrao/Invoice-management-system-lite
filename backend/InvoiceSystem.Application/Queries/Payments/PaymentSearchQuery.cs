namespace InvoiceSystem.Application.Queries.Payments;

public class PaymentSearchQuery
{
    public int Year { get; set; }
    public int? Month { get; set; } // null=all
    public string? Keyword { get; set; }
    public string Status { get; set; } = "all"; // all/UNALLOCATED/PARTIAL/ALLOCATED

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

