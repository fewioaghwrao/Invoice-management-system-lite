using System;
using System.Windows.Media;

namespace InvoiceSystem.Wpf.Models;

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

    public Brush StatusBackground => StatusKey switch
    {
        "未入金" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F1D1D")),
        "一部入金" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B2A12")),
        "入金済み" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F2F24")),
        "期限超過" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4C0519")),
        "キャンセル" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F2937")),
        _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"))
    };

    public Brush StatusForeground => StatusKey switch
    {
        "未入金" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCA5A5")),
        "一部入金" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCD34D")),
        "入金済み" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#86EFAC")),
        "期限超過" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDA4AF")),
        "キャンセル" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1")),
        _ => Brushes.White
    };

    public Brush StatusBorderBrush => StatusKey switch
    {
        "未入金" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C")),
        "一部入金" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706")),
        "入金済み" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669")),
        "期限超過" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E11D48")),
        "キャンセル" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569")),
        _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"))
    };

    public bool CanRemind =>
        StatusKey == "未入金" ||
        StatusKey == "一部入金" ||
        StatusKey == "期限超過";
}