namespace InvoiceSystem.Domain.Entities;

public class AuditLog
{
    public long Id { get; set; }

    // だれが？
    public long ActorUserId { get; set; }      // ★ userid
    public string? ActorRole { get; set; }     // 任意

    // なにを？
    public string Action { get; set; } = null!;
    public string Entity { get; set; } = null!;
    public string? EntityId { get; set; }

    // どんな内容？
    public string? Summary { get; set; }
    public string? DataJson { get; set; }

    // 追跡（任意）
    public string? CorrelationId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; }   // UTC
}

