namespace InvoiceSystem.Application.Dtos.Invoices;

public class InvoiceDetailDto
{
    public long Id { get; set; }

    public long MemberId { get; set; }
    public string MemberName { get; set; } = null!;

    public string InvoiceNumber { get; set; } = null!;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }

    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }

    public long StatusId { get; set; }
    public string StatusName { get; set; } = null!;

    public string? PdfPath { get; set; }
    public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<InvoicePaymentAllocationDto> Allocations { get; set; } = new();
    public List<InvoiceReminderHistoryDto> Reminders { get; set; } = new();

    public List<InvoiceLineDto> Lines { get; set; } = new();
}

public class InvoicePaymentAllocationDto
{
    public long PaymentId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal AllocatedAmount { get; set; }

    public string? PayerName { get; set; }
    public string? Method { get; set; }

    public long? ImportBatchId { get; set; }
}

public class InvoiceReminderHistoryDto
{
    public long Id { get; set; }
    public DateTime RemindedAt { get; set; }
    public string Method { get; set; } = null!;
    public string? Note { get; set; }
}


