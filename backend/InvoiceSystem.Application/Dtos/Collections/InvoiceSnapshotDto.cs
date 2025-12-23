namespace InvoiceSystem.Application.Dtos.Collections;

public class InvoiceSnapshotDto
{
    public long InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = null!;
    public string MemberName { get; set; } = null!;
    public string? MemberEmail { get; set; } // MemberにEmailがある前提（無ければnull）

    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }

    public decimal Total { get; set; }
    public decimal PaidTotal { get; set; }
}

