using InvoiceSystem.Wpf.Models;
using System;
using System.Windows;
using System.Windows.Media;

namespace InvoiceSystem.Wpf.ViewModels;

public sealed class MemberPaymentStatusRowViewModel
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime DueAt { get; set; }
    public decimal RemainingAmount { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public bool IsOverdue { get; set; }

    public string DueAtText => DueAt == default ? string.Empty : DueAt.ToString("yyyy/MM/dd");
    public string RemainingAmountText => $"{Math.Max(0m, RemainingAmount):N0} 円";

    public bool IsPaid =>
        !string.IsNullOrWhiteSpace(StatusName) &&
        StatusName.Contains("入金済");

    public Visibility OverdueBadgeVisibility =>
        IsOverdue && RemainingAmount > 0 && !IsPaid ? Visibility.Visible : Visibility.Collapsed;

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

    public static MemberPaymentStatusRowViewModel FromDto(AccountInvoiceListItemDto dto)
    {
        return new MemberPaymentStatusRowViewModel
        {
            Id = dto.Id,
            InvoiceNumber = dto.InvoiceNumber ?? string.Empty,
            DueAt = dto.DueAt,
            RemainingAmount = dto.RemainingAmount,
            StatusName = dto.StatusName ?? string.Empty,
            IsOverdue = dto.IsOverdue
        };
    }

    private static Brush CreateBrush(string hex)
    {
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
    }
}