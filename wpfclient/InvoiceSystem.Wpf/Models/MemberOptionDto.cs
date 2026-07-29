using System.Text.Json.Serialization;

namespace InvoiceSystem.Wpf.Models;

public class MemberOptionDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}