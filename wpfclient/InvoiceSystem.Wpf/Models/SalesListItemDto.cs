using System;

namespace InvoiceSystem.Wpf.Models;

public class SalesListItemDto
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public DateTime DueAt { get; set; }
    public string Status { get; set; } = string.Empty; // UNPAID / PARTIAL / PAID
    public decimal InvoiceAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateTime? LastPaidAt { get; set; }

    public string StatusText => Status?.ToUpperInvariant() switch
    {
        "PAID" => "入金済",
        "PARTIAL" => "一部入金",
        _ => "未入金"
    };

    public string IssuedAtText => IssuedAt.ToString("yyyy/MM/dd");
    public string DueAtText => DueAt.ToString("yyyy/MM/dd");
    public string LastPaidAtText => LastPaidAt?.ToString("yyyy/MM/dd") ?? "—";
}