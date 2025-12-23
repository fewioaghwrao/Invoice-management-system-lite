namespace InvoiceSystem.Application.Commands.Invoices;

public class CreateInvoiceCommand
{
    public long MemberId { get; set; }

    public string InvoiceNumber { get; set; } = null!;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Remarks { get; set; }
}

