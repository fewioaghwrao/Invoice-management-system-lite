using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace InvoiceSystem.Wpf.Models;

public class InvoiceDetailDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("memberId")]
    public long MemberId { get; set; }

    [JsonPropertyName("memberName")]
    public string MemberName { get; set; } = string.Empty;

    [JsonPropertyName("invoiceNumber")]
    public string InvoiceNumber { get; set; } = string.Empty;

    [JsonPropertyName("invoiceDate")]
    public DateTime InvoiceDate { get; set; }

    [JsonPropertyName("dueDate")]
    public DateTime DueDate { get; set; }

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("paidAmount")]
    public decimal PaidAmount { get; set; }

    [JsonPropertyName("remainingAmount")]
    public decimal RemainingAmount { get; set; }

    [JsonPropertyName("statusId")]
    public long StatusId { get; set; }

    [JsonPropertyName("statusName")]
    public string StatusName { get; set; } = string.Empty;

    [JsonPropertyName("pdfPath")]
    public string? PdfPath { get; set; }

    [JsonPropertyName("remarks")]
    public string? Remarks { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("lines")]
    public List<InvoiceLineDto> Lines { get; set; } = new();

    [JsonPropertyName("allocations")]
    public List<InvoicePaymentAllocationDto> Allocations { get; set; } = new();

    [JsonPropertyName("reminders")]
    public List<InvoiceReminderHistoryDto> Reminders { get; set; } = new();
}
