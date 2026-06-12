namespace InvoiceSystem.Domain.Entities;

public sealed class ReminderJob
{
    public int Id { get; set; }

    public long InvoiceId { get; set; }
    public string ToEmail { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string Body { get; set; } = null!;

    public string Status { get; set; } = "Pending";
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public Invoice Invoice { get; set; } = null!;
}