using InvoiceSystem.Wpf.Models;

namespace InvoiceSystem.Wpf.Services;

public interface ISalesService
{
    Task<SalesListResponseDto> GetSalesListAsync(SalesSearchRequest request);

    Task<SalesByMemberResponseDto> GetSalesByMemberAsync(
        int year,
        int? month,
        string keyword,
        int page,
        int pageSize);

    Task<byte[]> ExportSalesCsvAsync(SalesSearchRequest request);

    Task<byte[]> ExportSalesByMemberCsvAsync(
        int year,
        int? month,
        string keyword);
}