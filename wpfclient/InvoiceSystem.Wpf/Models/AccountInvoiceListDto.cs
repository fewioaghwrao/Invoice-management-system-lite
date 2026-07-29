using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace InvoiceSystem.Wpf.Models;

public class AccountInvoiceListDto
{
    [JsonPropertyName("year")]
    public int Year { get; set; }

    [JsonPropertyName("availableYears")]
    public List<int> AvailableYears { get; set; } = new();

    [JsonPropertyName("month")]
    public string Month { get; set; } = "all";

    [JsonPropertyName("status")]
    public string Status { get; set; } = "all";

    [JsonPropertyName("q")]
    public string Q { get; set; } = string.Empty;

    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 10;

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    [JsonPropertyName("items")]
    public List<AccountInvoiceListItemDto> Items { get; set; } = new();
}