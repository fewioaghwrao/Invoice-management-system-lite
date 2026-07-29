using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace InvoiceSystem.Wpf.Models;

public class InvoiceUpsertRequestDto
{
    [JsonPropertyName("memberId")]
    public long MemberId { get; set; }

    [JsonPropertyName("invoiceNumber")]
    public string InvoiceNumber { get; set; } = string.Empty;

    [JsonPropertyName("invoiceDate")]
    public DateTime InvoiceDate { get; set; }

    [JsonPropertyName("dueDate")]
    public DateTime DueDate { get; set; }

    [JsonPropertyName("statusId")]
    public long StatusId { get; set; }

    [JsonPropertyName("remarks")]
    public string? Remarks { get; set; }

    [JsonPropertyName("lines")]
    public List<InvoiceUpsertLineDto> Lines { get; set; } = new();
}