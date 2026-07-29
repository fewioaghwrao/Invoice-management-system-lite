using System;
using System.Text.Json.Serialization;

namespace InvoiceSystem.Wpf.Models;

public class AccountInvoiceListItemDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("invoiceNumber")]
    public string InvoiceNumber { get; set; } = string.Empty;

    [JsonPropertyName("issuedAt")]
    public DateTime IssuedAt { get; set; }

    [JsonPropertyName("dueAt")]
    public DateTime DueAt { get; set; }

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("remainingAmount")]
    public decimal RemainingAmount { get; set; }

    [JsonPropertyName("statusName")]
    public string StatusName { get; set; } = string.Empty;

    [JsonPropertyName("isOverdue")]
    public bool IsOverdue { get; set; }

}