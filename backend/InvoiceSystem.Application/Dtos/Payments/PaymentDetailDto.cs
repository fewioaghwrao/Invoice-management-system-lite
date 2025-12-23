namespace InvoiceSystem.Application.Dtos.Payments;

public class PaymentDetailDto
{
    public long Id { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string? PayerName { get; set; }
    public string? Method { get; set; }

    public decimal AllocatedAmount { get; set; }
    public decimal UnallocatedAmount { get; set; }
    public string Status { get; set; } = "UNALLOCATED"; // UNALLOCATED / PARTIAL / ALLOCATED

    public List<PaymentAllocationDto> Allocations { get; set; } = new();
}

public class PaymentAllocationDto
{
    public long InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = "";
    public decimal Amount { get; set; }
}
