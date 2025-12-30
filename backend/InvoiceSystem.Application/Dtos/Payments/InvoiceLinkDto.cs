namespace InvoiceSystem.Application.Dtos.Payments;

public class InvoiceLinkDto
{
    public long Id { get; set; }                 // Invoice.Id
    public string InvoiceNumber { get; set; } = null!; // "INV-001"
}

