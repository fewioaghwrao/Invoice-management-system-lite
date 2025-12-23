namespace InvoiceSystem.Application.Dtos.Payments;

public class PaymentListItemDto
{
    public long Id { get; set; }                 // DBのID
    public DateTime PaymentDate { get; set; }
    public string? PayerName { get; set; }
    public decimal Amount { get; set; }

    public decimal AllocatedAmount { get; set; } // 割当済合計
    public List<string> InvoiceIds { get; set; } = new(); // INV-001 等（InvoiceNumber）

    public string Status { get; set; } = null!;  // "UNALLOCATED" | "PARTIAL" | "ALLOCATED"
}

