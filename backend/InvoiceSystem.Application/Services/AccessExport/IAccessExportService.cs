using InvoiceSystem.Application.Dtos.AccessExport;
using InvoiceSystem.Application.Queries.AccessExport;

namespace InvoiceSystem.Application.Services.AccessExport;

public interface IAccessExportService
{
    Task<AccessExportDataDto> ExportAsync(
        AccessExportQuery query);
}