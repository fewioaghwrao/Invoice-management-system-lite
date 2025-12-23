using InvoiceSystem.Application.Dtos.Admin;

namespace InvoiceSystem.Application.Common.Interfaces;

public interface IAdminSummaryService
{
    Task<AdminSummaryDto> GetSummaryAsync(int year, CancellationToken ct = default);
}
