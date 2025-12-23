namespace InvoiceSystem.Application.Dtos.Invoices;

public class InvoiceListItemDto
{
    public long Id { get; set; }

    public string InvoiceNumber { get; set; } = null!;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string MemberName { get; set; } = null!;
    public string StatusName { get; set; } = null!;
}

