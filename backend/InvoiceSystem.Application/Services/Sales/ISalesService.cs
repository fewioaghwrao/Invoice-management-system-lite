using InvoiceSystem.Application.Dtos.Sales;
using InvoiceSystem.Application.Queries.Sales;

namespace InvoiceSystem.Application.Services.Sales;

public interface ISalesService
{
    Task<SalesListResultDto> SearchAsync(SalesSearchRequest request);

    Task<SalesByMemberResultDto> SearchByMemberAsync(SalesSearchRequest request);

    Task<IReadOnlyList<SalesExportRow>> ExportAsync(SalesSearchQuery query);
}
