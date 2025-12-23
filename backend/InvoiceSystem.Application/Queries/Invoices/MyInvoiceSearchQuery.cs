namespace InvoiceSystem.Application.Queries.Invoices;

public class MyInvoiceSearchQuery
{
    public long MemberId { get; set; }          // ★必須（/me で確定）
    public int Year { get; set; }               // 必須扱い（未指定はAPI側で補完）
    public string? Month { get; set; }          // "all" or "1".."12" (null=allでもOK)
    public string? Status { get; set; }         // "all"|"unpaid"|"partial"|"paid"
    public string? Q { get; set; }              // 検索（請求書番号/備考など）
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
