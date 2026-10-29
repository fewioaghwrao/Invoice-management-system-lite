using System.Text.Json.Serialization;

namespace InvoiceAvaloniaClient.Models;

public class MemberOptionDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}