namespace InvoiceSystem.Application.Dtos.Payments;

public class PaymentListResultDto
{
    public int Year { get; set; }
    public string Month { get; set; } = "all";     // "all" or "1".."12"
    public string Keyword { get; set; } = "";
    public string Status { get; set; } = "all";    // "all" | "UNALLOCATED" | "PARTIAL" | "ALLOCATED"

    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }

    public List<PaymentListItemDto> Rows { get; set; } = new();

    public SummaryDto Summary { get; set; } = new();

    public class SummaryDto
    {
        public decimal TotalAmount { get; set; }
        public decimal AllocatedTotal { get; set; }
        public decimal UnallocatedTotal { get; set; }
    }
}
