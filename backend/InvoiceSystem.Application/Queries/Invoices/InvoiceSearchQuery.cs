namespace InvoiceSystem.Application.Queries.Invoices;

public class InvoiceSearchQuery
{
    public string? InvoiceNumber { get; set; }
    public string? MemberName { get; set; }

    public DateTime? FromInvoiceDate { get; set; }
    public DateTime? ToInvoiceDate { get; set; }

    public long? StatusId { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

