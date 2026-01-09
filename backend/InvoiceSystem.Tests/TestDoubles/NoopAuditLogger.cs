using InvoiceSystem.Application.Common.Interfaces;

public sealed class NoopAuditLogger : IAuditLogger
{
    public Task WriteAsync(
        string action,
        string entity,
        string? entityId = null,
        string? summary = null,
        object? data = null,
        AuditActor? actor = null,
        CancellationToken ct = default)
        => Task.CompletedTask;
}
