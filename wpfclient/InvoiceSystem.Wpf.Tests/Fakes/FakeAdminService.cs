using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InvoiceSystem.Wpf.Tests.Fakes;

public sealed class FakeAdminService : IAdminService
{
    public AdminSummaryDto? SummaryToReturn { get; set; }
    public WorstTop5ResultDto? WorstTop5ToReturn { get; set; }
    public List<AdminOperationLogDto> RecentLogsToReturn { get; set; } = new();

    public Exception? ExceptionToThrowOnGetSummary { get; set; }
    public Exception? ExceptionToThrowOnGetWorstTop5 { get; set; }
    public Exception? ExceptionToThrowOnGetRecentLogs { get; set; }

    public int LastYearForSummary { get; private set; }
    public int LastYearForWorstTop5 { get; private set; }
    public int LastLimitForRecentLogs { get; private set; }

    public Task<AdminSummaryDto> GetSummaryAsync(int year)
    {
        LastYearForSummary = year;

        if (ExceptionToThrowOnGetSummary != null)
        {
            return Task.FromException<AdminSummaryDto>(ExceptionToThrowOnGetSummary);
        }

        return Task.FromResult(SummaryToReturn ?? new AdminSummaryDto { Year = year });
    }

    public Task<WorstTop5ResultDto> GetWorstTop5Async(int year)
    {
        LastYearForWorstTop5 = year;

        if (ExceptionToThrowOnGetWorstTop5 != null)
        {
            return Task.FromException<WorstTop5ResultDto>(ExceptionToThrowOnGetWorstTop5);
        }

        return Task.FromResult(WorstTop5ToReturn ?? new WorstTop5ResultDto { Year = year });
    }

    public Task<List<AdminOperationLogDto>> GetRecentOperationLogsAsync(int limit = 5)
    {
        LastLimitForRecentLogs = limit;

        if (ExceptionToThrowOnGetRecentLogs != null)
        {
            return Task.FromException<List<AdminOperationLogDto>>(ExceptionToThrowOnGetRecentLogs);
        }

        return Task.FromResult(RecentLogsToReturn);
    }
}