namespace InvoiceSystem.Wpf.Models;

public class SalesSearchRequest
{
    public int Year { get; set; }
    public int? Month { get; set; }   // null = 全月
    public string Status { get; set; } = "all"; // all / unpaid / partial / paid
    public string Keyword { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? MemberId { get; set; }
}
