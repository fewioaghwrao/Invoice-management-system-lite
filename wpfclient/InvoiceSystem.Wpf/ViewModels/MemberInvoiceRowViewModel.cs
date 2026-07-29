using InvoiceSystem.Wpf.Models;
using System;
using System.Windows;
using System.Windows.Media;

namespace InvoiceSystem.Wpf.ViewModels;

public sealed class MemberInvoiceRowViewModel
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public DateTime DueAt { get; set; }
    public decimal TotalAmount { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public bool IsOverdue { get; set; }
    public string? PdfUrl { get; set; }

    public string IssuedAtText => FormatDate(IssuedAt);
    public string DueAtText => FormatDate(DueAt);
    public string TotalAmountText => $"{TotalAmount:N0} 円";

    public bool IsPaid =>
        !string.IsNullOrWhiteSpace(StatusName) &&
        StatusName.Contains("入金済");

    public Visibility OverdueBadgeVisibility =>
        IsOverdue && !IsPaid ? Visibility.Visible : Visibility.Collapsed;

    public Brush StatusForeground =>
        StatusName switch
        {
            var s when s.Contains("入金済") => CreateBrush("#86EFAC"),
            var s when s.Contains("一部") => CreateBrush("#FCD34D"),
            var s when s.Contains("未入金") => CreateBrush("#FDA4AF"),
            _ => CreateBrush("#E2E8F0")
        };

    public Brush StatusBackground =>
        StatusName switch
        {
            var s when s.Contains("入金済") => CreateBrush("#0B2A1E"),
            var s when s.Contains("一部") => CreateBrush("#3B2506"),
            var s when s.Contains("未入金") => CreateBrush("#3A1118"),
            _ => CreateBrush("#132032")
        };

    public Brush StatusBorderBrush =>
        StatusName switch
        {
            var s when s.Contains("入金済") => CreateBrush("#166534"),
            var s when s.Contains("一部") => CreateBrush("#92400E"),
            var s when s.Contains("未入金") => CreateBrush("#9F1239"),
            _ => CreateBrush("#334155")
        };

    public static MemberInvoiceRowViewModel FromDto(AccountInvoiceListItemDto dto)
    {
        return new MemberInvoiceRowViewModel
        {
            Id = dto.Id,
            InvoiceNumber = dto.InvoiceNumber ?? string.Empty,
            IssuedAt = dto.IssuedAt,
            DueAt = dto.DueAt,
            TotalAmount = dto.TotalAmount,
            StatusName = dto.StatusName ?? string.Empty,
            IsOverdue = dto.IsOverdue,
            PdfUrl = TryReadPdfUrl(dto)
        };
    }

    private static string FormatDate(DateTime value)
    {
        if (value == default)
            return string.Empty;

        return value.ToString("yyyy/MM/dd");
    }

    private static string? TryReadPdfUrl(AccountInvoiceListItemDto dto)
    {
        var type = dto.GetType();

        var pdfUrlProp = type.GetProperty("PdfUrl");
        var pdfPathProp = type.GetProperty("PdfPath");

        var pdfUrl = pdfUrlProp?.GetValue(dto)?.ToString();
        if (!string.IsNullOrWhiteSpace(pdfUrl))
            return pdfUrl;

        var pdfPath = pdfPathProp?.GetValue(dto)?.ToString();
        return string.IsNullOrWhiteSpace(pdfPath) ? null : pdfPath;
    }

    private static Brush CreateBrush(string hex)
    {
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
    }
}