using System;
using System.Collections.Generic;
using InvoiceSystem.Application.Dtos.Admin;

namespace InvoiceSystem.Application.Services;

public interface IAdminOperationLogService
{
    Task<IReadOnlyList<AdminOperationLogDto>> GetRecentAsync(int limit, CancellationToken ct = default);

    Task<AdminOperationLogListResultDto> SearchAsync(
     int page,
     int pageSize,
     CancellationToken ct = default);
}
