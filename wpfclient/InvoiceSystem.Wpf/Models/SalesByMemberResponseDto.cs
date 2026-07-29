using System.Collections.Generic;

namespace InvoiceSystem.Wpf.Models;

public class SalesByMemberResponseDto
{
    public int Year { get; set; }
    public object? Month { get; set; }
    public string Keyword { get; set; } = string.Empty;
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }

    public List<SalesByMemberRowDto> Rows { get; set; } = new();
    public SalesByMemberSummaryDto Summary { get; set; } = new();
}
