using InvoiceSystem.Wpf.Models;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace InvoiceSystem.Wpf.Services;

public class SalesService : ISalesService
{
    private readonly HttpClient _httpClient;

    public SalesService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<SalesListResponseDto> GetSalesListAsync(SalesSearchRequest request)
    {
        var url = BuildUrl(request);

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SalesListResponseDto>();

        return result ?? new SalesListResponseDto();
    }

    private static string BuildUrl(SalesSearchRequest request)
    {
        var sb = new StringBuilder();
        sb.Append("/api/sales?");
        sb.Append($"year={request.Year}");

        if (request.Month.HasValue)
        {
            sb.Append($"&month={request.Month.Value}");
        }

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            !string.Equals(request.Status, "all", StringComparison.OrdinalIgnoreCase))
        {
            sb.Append($"&status={Uri.EscapeDataString(request.Status.ToLowerInvariant())}");
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            sb.Append($"&q={Uri.EscapeDataString(request.Keyword)}");
        }

        if (request.MemberId.HasValue)
        {
            sb.Append($"&memberId={request.MemberId.Value}");
        }

        sb.Append($"&page={request.Page}");
        sb.Append($"&pageSize={request.PageSize}");

        return sb.ToString();
    }
    public async Task<SalesByMemberResponseDto> GetSalesByMemberAsync(
    int year,
    int? month,
    string keyword,
    int page,
    int pageSize)
    {
        var url = BuildByMemberUrl(year, month, keyword, page, pageSize);

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SalesByMemberResponseDto>();
        return result ?? new SalesByMemberResponseDto();
    }

    private static string BuildByMemberUrl(
        int year,
        int? month,
        string keyword,
        int page,
        int pageSize)
    {
        var sb = new StringBuilder();
        sb.Append("/api/sales/by-member?");
        sb.Append($"year={year}");

        if (month.HasValue)
        {
            sb.Append($"&month={month.Value}");
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            sb.Append($"&q={Uri.EscapeDataString(keyword)}");
        }

        sb.Append($"&page={page}");
        sb.Append($"&pageSize={pageSize}");

        return sb.ToString();
    }

    public async Task<byte[]> ExportSalesCsvAsync(SalesSearchRequest request)
    {
        var url = BuildExportUrl(request);

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task<byte[]> ExportSalesByMemberCsvAsync(
        int year,
        int? month,
        string keyword)
    {
        var url = BuildByMemberExportUrl(year, month, keyword);

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsByteArrayAsync();
    }

    private static string BuildExportUrl(SalesSearchRequest request)
    {
        var sb = new StringBuilder();
        sb.Append("/api/sales/export?");
        sb.Append($"year={request.Year}");

        if (request.Month.HasValue)
            sb.Append($"&month={request.Month.Value}");

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            !string.Equals(request.Status, "all", StringComparison.OrdinalIgnoreCase))
        {
            sb.Append($"&status={Uri.EscapeDataString(request.Status)}");
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
            sb.Append($"&q={Uri.EscapeDataString(request.Keyword)}");

        if (request.MemberId.HasValue)
            sb.Append($"&memberId={request.MemberId.Value}");

        return sb.ToString();
    }

    private static string BuildByMemberExportUrl(int year, int? month, string keyword)
    {
        var sb = new StringBuilder();
        sb.Append("/api/sales/by-member/export?");
        sb.Append($"year={year}");

        if (month.HasValue)
            sb.Append($"&month={month.Value}");

        if (!string.IsNullOrWhiteSpace(keyword))
            sb.Append($"&q={Uri.EscapeDataString(keyword)}");

        return sb.ToString();
    }
}
