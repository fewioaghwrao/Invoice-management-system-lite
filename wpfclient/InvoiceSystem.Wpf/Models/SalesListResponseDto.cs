using System.Collections.Generic;

namespace InvoiceSystem.Wpf.Models;

public class SalesListResponseDto
{
    public int Year { get; set; }
    public object? Month { get; set; } // API都合で "all" または数値が来る想定
    public string Keyword { get; set; } = string.Empty;
    public string Status { get; set; } = "all";
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public string? MemberName { get; set; }

    public List<SalesListItemDto> Rows { get; set; } = new();
    public SalesSummaryDto Summary { get; set; } = new();
}