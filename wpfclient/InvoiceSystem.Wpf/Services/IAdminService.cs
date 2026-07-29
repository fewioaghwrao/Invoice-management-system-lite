using InvoiceSystem.Wpf.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InvoiceSystem.Wpf.Services;

public interface IAdminService
{
    Task<AdminSummaryDto> GetSummaryAsync(int year);
    Task<WorstTop5ResultDto> GetWorstTop5Async(int year);
    Task<List<AdminOperationLogDto>> GetRecentOperationLogsAsync(int limit = 5);
}