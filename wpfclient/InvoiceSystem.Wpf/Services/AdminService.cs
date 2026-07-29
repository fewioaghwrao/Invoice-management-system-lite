using InvoiceSystem.Wpf.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace InvoiceSystem.Wpf.Services;

public class AdminService : IAdminService
{
    private readonly HttpClient _httpClient;

    public AdminService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AdminSummaryDto> GetSummaryAsync(int year)
    {
        var response = await _httpClient.GetAsync($"/api/admin/summary?year={year}");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AdminSummaryDto>();
        return result ?? new AdminSummaryDto { Year = year };
    }

    public async Task<WorstTop5ResultDto> GetWorstTop5Async(int year)
    {
        var response = await _httpClient.GetAsync($"/api/sales/by-member/worst-top5?year={year}");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<WorstTop5ResultDto>();
        return result ?? new WorstTop5ResultDto { Year = year };
    }

    public async Task<List<AdminOperationLogDto>> GetRecentOperationLogsAsync(int limit = 5)
    {
        var response = await _httpClient.GetAsync($"/api/admin/operation-logs/recent?limit={limit}");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<AdminOperationLogDto>>();
        return result ?? new List<AdminOperationLogDto>();
    }
}