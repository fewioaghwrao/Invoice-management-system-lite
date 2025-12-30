namespace InvoiceSystem.Application.Common.Interfaces;

public interface IAuditLogger
{
    Task WriteAsync(
        string action,
        string entity,
        string? entityId = null,
        string? summary = null,
        object? data = null,
        AuditActor? actor = null,
        CancellationToken ct = default);
}

public sealed record AuditActor(
    long UserId,
    string? Role = null,          // "ADMIN" / "MEMBER" 等
    string? IpAddress = null,
    string? UserAgent = null,
    string? CorrelationId = null
);
