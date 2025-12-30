using InvoiceSystem.Application.Dtos.Admin;
using InvoiceSystem.Application.Services;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Infrastructure.Services;

public class AdminOperationLogService : IAdminOperationLogService
{
    private readonly AppDbContext _db;

    public AdminOperationLogService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AdminOperationLogDto>> GetRecentAsync(int limit, CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 50);

        return await _db.AuditLogs
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Take(limit)
            .Select(x => new AdminOperationLogDto(
                x.Id,                // Id
                x.CreatedAt,         // At
                x.ActorUserId,       // ActorUserId
                x.Action,            // Action
                x.Entity,            // Entity
                x.EntityId,          // EntityId
                x.Summary ?? ""      // Summary
            ))
            .ToListAsync(ct);

    }
}

