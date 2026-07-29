namespace InvoiceSystem.Wpf.Models;

public class CollectionSnapshotDto
{
    public long InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public string MemberEmail { get; set; } = string.Empty;

    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }

    public decimal Total { get; set; }
    public decimal PaidTotal { get; set; }

    public decimal Remaining => Math.Max(0, Total - PaidTotal);
}