namespace InvoiceSystem.Domain.Entities;

public class Invoice
{
    public long Id { get; set; }

    public long MemberId { get; set; }
    public Member Member { get; set; } = null!;

    public string InvoiceNumber { get; set; } = null!;
    public DateTime InvoiceDate { get; set; }   // 請求日
    public DateTime DueDate { get; set; }       // 支払期限

    public decimal TotalAmount { get; set; }

    public long StatusId { get; set; }
    public InvoiceStatus Status { get; set; } = null!;

    public string? PdfPath { get; set; }
    public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<PaymentAllocation> PaymentAllocations { get; set; } = new List<PaymentAllocation>();
    public ICollection<ReminderHistory> ReminderHistories { get; set; } = new List<ReminderHistory>();

    public ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
}

