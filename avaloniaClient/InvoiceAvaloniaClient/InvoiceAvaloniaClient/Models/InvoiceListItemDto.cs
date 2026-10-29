using System;
using Avalonia.Media;

namespace InvoiceAvaloniaClient.Models;

public class InvoiceListItemDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime? InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal TotalAmount { get; set; }

    private string StatusKey => (StatusName ?? string.Empty).Trim();

    public IBrush StatusBackground => StatusKey switch
    {
        "未入金" => new SolidColorBrush(Color.Parse("#3F1D1D")),
        "一部入金" => new SolidColorBrush(Color.Parse("#3B2A12")),
        "入金済み" => new SolidColorBrush(Color.Parse("#0F2F24")),
        "期限超過" => new SolidColorBrush(Color.Parse("#4C0519")),
        "キャンセル" => new SolidColorBrush(Color.Parse("#1F2937")),
        _ => new SolidColorBrush(Color.Parse("#1E293B"))
    };

    public IBrush StatusForeground => StatusKey switch
    {
        "未入金" => new SolidColorBrush(Color.Parse("#FCA5A5")),
        "一部入金" => new SolidColorBrush(Color.Parse("#FCD34D")),
        "入金済み" => new SolidColorBrush(Color.Parse("#86EFAC")),
        "期限超過" => new SolidColorBrush(Color.Parse("#FDA4AF")),
        "キャンセル" => new SolidColorBrush(Color.Parse("#CBD5E1")),
        _ => Brushes.White
    };

    public IBrush StatusBorderBrush => StatusKey switch
    {
        "未入金" => new SolidColorBrush(Color.Parse("#B91C1C")),
        "一部入金" => new SolidColorBrush(Color.Parse("#D97706")),
        "入金済み" => new SolidColorBrush(Color.Parse("#059669")),
        "期限超過" => new SolidColorBrush(Color.Parse("#E11D48")),
        "キャンセル" => new SolidColorBrush(Color.Parse("#475569")),
        _ => new SolidColorBrush(Color.Parse("#334155"))
    };

    public bool CanRemind =>
        StatusKey == "未入金" ||
        StatusKey == "一部入金" ||
        StatusKey == "期限超過";
}