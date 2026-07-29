namespace InvoiceSystem.Wpf.Models;

public class InvoiceSearchRequest
{
    public string? InvoiceNumber { get; set; }
    public string? MemberName { get; set; }
    public int? StatusId { get; set; }
    public DateTime? FromInvoiceDate { get; set; }
    public DateTime? ToInvoiceDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
