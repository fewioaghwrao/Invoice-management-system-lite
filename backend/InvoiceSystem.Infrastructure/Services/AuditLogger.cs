using System.Text.Json;
using InvoiceSystem.Application.Common.Interfaces;
using InvoiceSystem.Domain.Entities;

namespace InvoiceSystem.Infrastructure.Services;

public class AuditLogger : IAuditLogger
{
    private readonly AppDbContext _db;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public AuditLogger(AppDbContext db)
    {
        _db = db;
    }

    public async Task WriteAsync(
        string action,
        string entity,
        string? entityId = null,
        string? summary = null,
        object? data = null,
        AuditActor? actor = null,
        CancellationToken ct = default)
    {
        if (actor is null)
            throw new InvalidOperationException("AuditActor is required (Endpoint에서渡す方針のため).");

        var now = DateTime.UtcNow;

        string? json = null;
        if (data is not null)
            json = JsonSerializer.Serialize(data, JsonOpts);

        var e = new AuditLog
        {
            ActorUserId = actor.UserId,
            ActorRole = actor.Role,

            Action = action,
            Entity = entity,
            EntityId = entityId,

            Summary = summary,
            DataJson = json,

            CorrelationId = actor.CorrelationId,
            IpAddress = actor.IpAddress,
            UserAgent = actor.UserAgent,

            CreatedAt = now
        };

        _db.AuditLogs.Add(e);
        await _db.SaveChangesAsync(ct);
    }
}
