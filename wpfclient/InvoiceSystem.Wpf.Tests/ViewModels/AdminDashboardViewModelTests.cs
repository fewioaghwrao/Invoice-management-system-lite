using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Tests.Fakes;
using InvoiceSystem.Wpf.ViewModels;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace InvoiceSystem.Wpf.Tests.ViewModels;

public class AdminDashboardViewModelTests
{
    [Fact]
    public void Constructor_InitialValues_AreExpected()
    {
        var service = new FakeAdminService();
        var currentUser = new CurrentUser
        {
            Id = 1,
            Name = "Admin User",
            Email = "admin@example.com",
            Role = "Admin",
            Token = "dummy-token"
        };

        var vm = new AdminDashboardViewModel(service, currentUser);

        Assert.Same(currentUser, vm.CurrentUser);
        Assert.False(vm.IsLoading);
        Assert.False(vm.HasError);
        Assert.Equal(string.Empty, vm.ErrorMessage);

        Assert.NotNull(vm.MonthlySalesPlotModel);
        Assert.NotNull(vm.FixedPlotController);

        Assert.Empty(vm.UnpaidInvoices);
        Assert.Empty(vm.MonthlySales);
        Assert.Empty(vm.WorstCustomers);
        Assert.Empty(vm.RecentLogs);

        Assert.Equal($"{vm.SelectedYear} 年", vm.SelectedYearText);
    }

    [Fact]
    public async Task LoadAsync_Success_AppliesSummaryWorstAndLogs()
    {
        var service = new FakeAdminService
        {
            SummaryToReturn = new AdminSummaryDto
            {
                Year = 2026,
                InvoiceTotal = 500000m,
                PaidTotal = 320000m,
                RemainingTotal = 180000m,
                RecoveryRate = 64.0,
                InvoiceCount = 12,
                PaymentCount = 7,
                MonthlySales = new List<AdminMonthlySalesDto>
                {
                    new() { Month = 1, InvoiceTotal = 100000m },
                    new() { Month = 3, InvoiceTotal = 150000m },
                    new() { Month = 12, InvoiceTotal = 250000m }
                },
                UnpaidTop5 = new List<AdminUnpaidInvoiceDto>
                {
                    new()
                    {
                        InvoiceId = 101,
                        InvoiceNumber = "INV-101",
                        ClientName = "A商事",
                        DueDate = new DateTime(2026, 4, 10),
                        InvoiceTotal = 120000m,
                        PaidTotal = 20000m,
                        RemainingTotal = 100000m,
                        IsOverdue = true
                    },
                    new()
                    {
                        InvoiceId = 102,
                        InvoiceNumber = "INV-102",
                        ClientName = "B株式会社",
                        DueDate = new DateTime(2026, 4, 15),
                        InvoiceTotal = 80000m,
                        PaidTotal = 0m,
                        RemainingTotal = 80000m,
                        IsOverdue = false
                    }
                }
            },
            WorstTop5ToReturn = new WorstTop5ResultDto
            {
                Year = 2026,
                Count = 2,
                Rows = new List<WorstCustomerDto>
                {
                    new()
                    {
                        MemberId = 1,
                        MemberName = "会員A",
                        InvoiceTotal = 100000m,
                        PaidTotal = 10000m,
                        RemainingTotal = 90000m,
                        RecoveryRate = 10.0
                    },
                    new()
                    {
                        MemberId = 2,
                        MemberName = "会員B",
                        InvoiceTotal = 200000m,
                        PaidTotal = 100000m,
                        RemainingTotal = 100000m,
                        RecoveryRate = 50.0
                    }
                }
            },
            RecentLogsToReturn = new List<AdminOperationLogDto>
            {
                new()
                {
                    Id = 1,
                    At = new DateTime(2026, 4, 23, 9, 30, 0),
                    ActorUserId = 10,
                    Action = "PAYMENT_ALLOCATION_ADDED",
                    Entity = "PAYMENT",
                    EntityId = "55",
                    Summary = "入金割当を追加"
                },
                new()
                {
                    Id = 2,
                    At = new DateTime(2026, 4, 23, 10, 0, 0),
                    ActorUserId = 11,
                    Action = "PAYMENT_ALLOCATIONS_CLEARED",
                    Entity = "INVOICE",
                    EntityId = "88",
                    Summary = "割当クリア"
                }
            }
        };

        var vm = new AdminDashboardViewModel(service, null)
        {
            SelectedYear = 2026
        };

        await vm.LoadAsync();

        Assert.False(vm.IsLoading);
        Assert.False(vm.HasError);
        Assert.Equal(string.Empty, vm.ErrorMessage);

        Assert.Equal(2026, service.LastYearForSummary);
        Assert.Equal(2026, service.LastYearForWorstTop5);
        Assert.Equal(5, service.LastLimitForRecentLogs);

        Assert.Equal(500000m, vm.TotalSales);
        Assert.Equal(180000m, vm.UnpaidAmount);
        Assert.Equal(12, vm.InvoiceCount);
        Assert.Equal(7, vm.PaymentCount);
        Assert.Equal(64.0, vm.RecoveryRate);

        Assert.Equal("￥500,000", vm.TotalSalesText);
        Assert.Equal("￥180,000", vm.UnpaidAmountText);
        Assert.Equal("12 件", vm.InvoiceCountText);
        Assert.Equal("7 件", vm.PaymentCountText);
        Assert.Equal("64.0%", vm.RecoveryRateText);

        Assert.Equal(2, vm.UnpaidInvoices.Count);
        Assert.Equal("INV-101", vm.UnpaidInvoices[0].InvoiceNumber);
        Assert.Equal("A商事", vm.UnpaidInvoices[0].ClientName);
        Assert.Equal("有", vm.UnpaidInvoices[0].OverdueStatusText);
        Assert.Equal("INV-102", vm.UnpaidInvoices[1].InvoiceNumber);
        Assert.Equal("無", vm.UnpaidInvoices[1].OverdueStatusText);

        Assert.Equal(3, vm.MonthlySales.Count);
        Assert.Equal("1月", vm.MonthlySales[0].MonthLabel);
        Assert.Equal(100000m, vm.MonthlySales[0].Amount);
        Assert.Equal("￥100,000", vm.MonthlySales[0].AmountText);

        Assert.Equal("3月", vm.MonthlySales[1].MonthLabel);
        Assert.Equal(150000m, vm.MonthlySales[1].Amount);

        Assert.Equal("12月", vm.MonthlySales[2].MonthLabel);
        Assert.Equal(250000m, vm.MonthlySales[2].Amount);

        Assert.Equal(2, vm.WorstCustomers.Count);
        Assert.Equal("会員A", vm.WorstCustomers[0].MemberName);
        Assert.Equal("会員B", vm.WorstCustomers[1].MemberName);

        Assert.Equal(2, vm.RecentLogs.Count);
        Assert.Equal("2026/04/23 09:30", vm.RecentLogs[0].AtText);
        Assert.Equal("割当追加", vm.RecentLogs[0].ActionLabel);
        Assert.Equal("入金 #55", vm.RecentLogs[0].TargetLabel);
        Assert.Equal("10", vm.RecentLogs[0].ActorText);

        Assert.Equal("割当クリア", vm.RecentLogs[1].ActionLabel);
        Assert.Equal("請求書 #88", vm.RecentLogs[1].TargetLabel);

        Assert.NotNull(vm.MonthlySalesPlotModel);
        Assert.Equal("月別売上（請求金額ベース）", vm.MonthlySalesPlotModel.Title);
        Assert.Equal("2026年 1月〜12月", vm.MonthlySalesPlotModel.Subtitle);
        Assert.Equal(2, vm.MonthlySalesPlotModel.Axes.Count);
        Assert.Single(vm.MonthlySalesPlotModel.Series);

        var lineSeries = Assert.IsType<LineSeries>(vm.MonthlySalesPlotModel.Series[0]);
        Assert.Equal(12, lineSeries.Points.Count);

        Assert.Equal(100000d, lineSeries.Points[0].Y);  // 1月
        Assert.Equal(0d, lineSeries.Points[1].Y);       // 2月
        Assert.Equal(150000d, lineSeries.Points[2].Y);  // 3月
        Assert.Equal(0d, lineSeries.Points[10].Y);      // 11月
        Assert.Equal(250000d, lineSeries.Points[11].Y); // 12月

        var xAxis = Assert.IsType<LinearAxis>(vm.MonthlySalesPlotModel.Axes[0]);
        var yAxis = Assert.IsType<LinearAxis>(vm.MonthlySalesPlotModel.Axes[1]);

        Assert.Equal(1d, xAxis.Minimum);
        Assert.Equal(12d, xAxis.Maximum);
        Assert.Equal(0d, yAxis.Minimum);
        Assert.True(yAxis.Maximum >= 250000d);
    }

    [Fact]
    public async Task LoadAsync_WhenSummaryFails_SetsError_AndResetsIsLoading()
    {
        var service = new FakeAdminService
        {
            ExceptionToThrowOnGetSummary = new InvalidOperationException("summary failed")
        };

        var vm = new AdminDashboardViewModel(service, null)
        {
            SelectedYear = 2026
        };

        await vm.LoadAsync();

        Assert.False(vm.IsLoading);
        Assert.True(vm.HasError);
        Assert.Contains("管理者ダッシュボードの読込に失敗しました。", vm.ErrorMessage);
        Assert.Contains("summary failed", vm.ErrorMessage);
    }

    [Fact]
    public async Task LoadAsync_WhenWorstFails_SetsError_AndResetsIsLoading()
    {
        var service = new FakeAdminService
        {
            SummaryToReturn = new AdminSummaryDto
            {
                Year = 2026,
                InvoiceTotal = 10000m,
                RemainingTotal = 2000m,
                InvoiceCount = 1,
                PaymentCount = 1,
                RecoveryRate = 80.0
            },
            ExceptionToThrowOnGetWorstTop5 = new InvalidOperationException("worst failed")
        };

        var vm = new AdminDashboardViewModel(service, null)
        {
            SelectedYear = 2026
        };

        await vm.LoadAsync();

        Assert.False(vm.IsLoading);
        Assert.True(vm.HasError);
        Assert.Contains("管理者ダッシュボードの読込に失敗しました。", vm.ErrorMessage);
        Assert.Contains("worst failed", vm.ErrorMessage);
    }

    [Fact]
    public async Task LoadAsync_WhenRecentLogsFail_SetsError_AndResetsIsLoading()
    {
        var service = new FakeAdminService
        {
            SummaryToReturn = new AdminSummaryDto
            {
                Year = 2026,
                InvoiceTotal = 10000m,
                RemainingTotal = 2000m,
                InvoiceCount = 1,
                PaymentCount = 1,
                RecoveryRate = 80.0
            },
            WorstTop5ToReturn = new WorstTop5ResultDto
            {
                Year = 2026,
                Rows = new List<WorstCustomerDto>()
            },
            ExceptionToThrowOnGetRecentLogs = new InvalidOperationException("logs failed")
        };

        var vm = new AdminDashboardViewModel(service, null)
        {
            SelectedYear = 2026
        };

        await vm.LoadAsync();

        Assert.False(vm.IsLoading);
        Assert.True(vm.HasError);
        Assert.Contains("管理者ダッシュボードの読込に失敗しました。", vm.ErrorMessage);
        Assert.Contains("logs failed", vm.ErrorMessage);
    }

    [Fact]
    public void MovePreviousYear_DecrementsSelectedYear()
    {
        var service = new FakeAdminService();
        var vm = new AdminDashboardViewModel(service, null)
        {
            SelectedYear = 2026
        };

        vm.MovePreviousYear();

        Assert.Equal(2025, vm.SelectedYear);
        Assert.Equal("2025 年", vm.SelectedYearText);
    }

    [Fact]
    public void MoveNextYear_IncrementsSelectedYear()
    {
        var service = new FakeAdminService();
        var vm = new AdminDashboardViewModel(service, null)
        {
            SelectedYear = 2026
        };

        vm.MoveNextYear();

        Assert.Equal(2027, vm.SelectedYear);
        Assert.Equal("2027 年", vm.SelectedYearText);
    }

    [Fact]
    public void AdminOperationLogItemViewModel_FormatsLabels_AsExpected()
    {
        var dto = new AdminOperationLogDto
        {
            Id = 1,
            At = new DateTime(2026, 4, 23, 11, 45, 0),
            ActorUserId = 77,
            Action = "PAYMENT_ALLOCATIONS_REPLACED",
            Entity = "MEMBER",
            EntityId = "999",
            Summary = "保存"
        };

        var vm = new AdminOperationLogItemViewModel(dto);

        Assert.Equal("2026/04/23 11:45", vm.AtText);
        Assert.Equal("割当保存（置換）", vm.ActionLabel);
        Assert.Equal("会員 #999", vm.TargetLabel);
        Assert.Equal("77", vm.ActorText);
    }

    [Fact]
    public void AdminOperationLogItemViewModel_UnknownValues_AreReturnedAsIs()
    {
        var dto = new AdminOperationLogDto
        {
            Id = 2,
            At = new DateTime(2026, 4, 23, 12, 0, 0),
            ActorUserId = 88,
            Action = "CUSTOM_ACTION",
            Entity = "CUSTOM_ENTITY",
            EntityId = null,
            Summary = "unknown"
        };

        var vm = new AdminOperationLogItemViewModel(dto);

        Assert.Equal("CUSTOM_ACTION", vm.ActionLabel);
        Assert.Equal("CUSTOM_ENTITY", vm.TargetLabel);
    }
}
