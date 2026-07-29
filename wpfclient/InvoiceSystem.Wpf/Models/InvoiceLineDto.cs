using System.Text.Json.Serialization;

namespace InvoiceSystem.Wpf.Models;

public class InvoiceLineDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("lineNo")]
    public int LineNo { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("qty")]
    public decimal Qty { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    public decimal Amount => Qty * UnitPrice;
}