using InvoiceSystem.Application.Dtos.Collections;
using InvoiceSystem.Application.Common.Interfaces;

namespace InvoiceSystem.Application.Services;

public interface ICollectionService
{
    Task<InvoiceSnapshotDto> GetSnapshotAsync(long invoiceId);
    Task<List<DunningLogDto>> GetLogsAsync(long invoiceId);
    Task<long> CreateLogAsync(long invoiceId, CreateDunningLogRequestDto req);

    Task<long> CreateLogAsync(long invoiceId, CreateDunningLogRequestDto req, AuditActor actor);
}
