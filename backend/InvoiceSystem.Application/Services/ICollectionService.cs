using InvoiceSystem.Application.Dtos.Collections;

namespace InvoiceSystem.Application.Services;

public interface ICollectionService
{
    Task<InvoiceSnapshotDto> GetSnapshotAsync(long invoiceId);
    Task<List<DunningLogDto>> GetLogsAsync(long invoiceId);
    Task<long> CreateLogAsync(long invoiceId, CreateDunningLogRequestDto req);
}
