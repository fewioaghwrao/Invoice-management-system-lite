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
    public async Task<AdminOperationLogListResultDto> SearchAsync(
    int page,
    int pageSize,
    CancellationToken ct = default)
    {
        page = page <= 0 ? 1 : page;
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.AuditLogs
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id);

        var totalCount = await query.CountAsync(ct);
        var totalPages = totalCount == 0
            ? 1
            : (int)Math.Ceiling(totalCount / (double)pageSize);

        if (page > totalPages)
            page = totalPages;

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AdminOperationLogDto(
                x.Id,
                x.CreatedAt,
                x.ActorUserId,
                x.Action,
                x.Entity,
                x.EntityId,
                x.Summary ?? ""
            ))
            .ToListAsync(ct);

        return new AdminOperationLogListResultDto(
            Page: page,
            PageSize: pageSize,
            TotalCount: totalCount,
            TotalPages: totalPages,
            Items: items
        );
    }
}

