namespace InvoiceSystem.Application.Dtos.Invoices;

public class InvoiceDto
{
    public long Id { get; set; }

    public long MemberId { get; set; }
    public string MemberName { get; set; } = null!;

    public string InvoiceNumber { get; set; } = null!;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }

    public decimal TotalAmount { get; set; }

    public long StatusId { get; set; }
    public string StatusName { get; set; } = null!;

    public string? PdfPath { get; set; }
    public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; }
}

