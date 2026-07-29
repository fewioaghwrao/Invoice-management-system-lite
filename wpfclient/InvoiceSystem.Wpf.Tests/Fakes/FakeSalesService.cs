using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Services;

namespace InvoiceSystem.Wpf.Tests.Fakes;

public sealed class FakeSalesService : ISalesService
{
    // =========================
    // SalesListViewModel 用
    // =========================
    public SalesListResponseDto? SalesListToReturn { get; set; }
    public Exception? ExceptionToThrowOnGetSalesList { get; set; }

    public int GetSalesListCallCount { get; private set; }
    public SalesSearchRequest? LastSalesSearchRequest { get; private set; }

    public Task<SalesListResponseDto> GetSalesListAsync(SalesSearchRequest request)
    {
        GetSalesListCallCount++;
        LastSalesSearchRequest = request;

        if (ExceptionToThrowOnGetSalesList != null)
        {
            return Task.FromException<SalesListResponseDto>(
                ExceptionToThrowOnGetSalesList);
        }

        return Task.FromResult(SalesListToReturn ?? new SalesListResponseDto
        {
            Rows = new List<SalesListItemDto>(),
            Summary = new SalesSummaryDto()
        });
    }

    // =========================
    // SalesByMemberViewModel 用
    // =========================
    public SalesByMemberResponseDto? SalesByMemberToReturn { get; set; }
    public Exception? ExceptionToThrowOnGetSalesByMember { get; set; }

    public int GetSalesByMemberCallCount { get; private set; }

    public int LastSalesByMemberYear { get; private set; }
    public int? LastSalesByMemberMonth { get; private set; }
    public string? LastSalesByMemberKeyword { get; private set; }
    public int LastSalesByMemberPage { get; private set; }
    public int LastSalesByMemberPageSize { get; private set; }

    public Task<SalesByMemberResponseDto> GetSalesByMemberAsync(
        int year,
        int? month,
        string keyword,
        int page,
        int pageSize)
    {
        GetSalesByMemberCallCount++;
        LastSalesByMemberYear = year;
        LastSalesByMemberMonth = month;
        LastSalesByMemberKeyword = keyword;
        LastSalesByMemberPage = page;
        LastSalesByMemberPageSize = pageSize;

        if (ExceptionToThrowOnGetSalesByMember != null)
        {
            return Task.FromException<SalesByMemberResponseDto>(
                ExceptionToThrowOnGetSalesByMember);
        }

        return Task.FromResult(SalesByMemberToReturn ?? new SalesByMemberResponseDto
        {
            Rows = new List<SalesByMemberRowDto>(),
            Summary = new SalesByMemberSummaryDto()
        });
    }

    // =========================
    // CSV出力用
    // 今回のViewModelテストでは基本未使用
    // =========================
    public byte[] ExportSalesCsvToReturn { get; set; } = Array.Empty<byte>();
    public byte[] ExportSalesByMemberCsvToReturn { get; set; } = Array.Empty<byte>();

    public Exception? ExceptionToThrowOnExportSalesCsv { get; set; }
    public Exception? ExceptionToThrowOnExportSalesByMemberCsv { get; set; }

    public int ExportSalesCsvCallCount { get; private set; }
    public int ExportSalesByMemberCsvCallCount { get; private set; }

    public SalesSearchRequest? LastExportSalesCsvRequest { get; private set; }

    public int LastExportSalesByMemberYear { get; private set; }
    public int? LastExportSalesByMemberMonth { get; private set; }
    public string? LastExportSalesByMemberKeyword { get; private set; }

    public Task<byte[]> ExportSalesCsvAsync(SalesSearchRequest request)
    {
        ExportSalesCsvCallCount++;
        LastExportSalesCsvRequest = request;

        if (ExceptionToThrowOnExportSalesCsv != null)
        {
            return Task.FromException<byte[]>(ExceptionToThrowOnExportSalesCsv);
        }

        return Task.FromResult(ExportSalesCsvToReturn);
    }

    public Task<byte[]> ExportSalesByMemberCsvAsync(
        int year,
        int? month,
        string keyword)
    {
        ExportSalesByMemberCsvCallCount++;
        LastExportSalesByMemberYear = year;
        LastExportSalesByMemberMonth = month;
        LastExportSalesByMemberKeyword = keyword;

        if (ExceptionToThrowOnExportSalesByMemberCsv != null)
        {
            return Task.FromException<byte[]>(ExceptionToThrowOnExportSalesByMemberCsv);
        }

        return Task.FromResult(ExportSalesByMemberCsvToReturn);
    }
}