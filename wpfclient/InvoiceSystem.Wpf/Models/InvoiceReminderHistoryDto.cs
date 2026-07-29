using System;
using System.Text.Json.Serialization;

namespace InvoiceSystem.Wpf.Models;

public class InvoiceReminderHistoryDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("remindedAt")]
    public DateTime RemindedAt { get; set; }

    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    [JsonPropertyName("note")]
    public string? Note { get; set; }
}