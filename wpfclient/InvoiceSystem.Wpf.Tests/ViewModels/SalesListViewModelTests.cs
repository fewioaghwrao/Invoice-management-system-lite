using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Tests.Fakes;
using InvoiceSystem.Wpf.ViewModels;
using Xunit;

namespace InvoiceSystem.Wpf.Tests.ViewModels;

public class SalesListViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitializeDefaultOptions()
    {
        var service = new FakeSalesService();

        var vm = new SalesListViewModel(service);

        var currentYear = DateTime.Now.Year;

        Assert.Equal(currentYear, vm.SelectedYear);
        Assert.Equal($"{currentYear}年", vm.SelectedYearText);

        Assert.Equal(13, vm.MonthOptions.Count);
        Assert.Equal("全月", vm.SelectedMonth?.Label);
        Assert.Null(vm.SelectedMonth?.Value);

        Assert.Equal(4, vm.StatusOptions.Count);
        Assert.Equal("all", vm.SelectedStatus?.Value);
        Assert.Equal("すべて", vm.SelectedStatus?.Label);

        Assert.Equal(1, vm.CurrentPage);
        Assert.Equal(10, vm.PageSize);
        Assert.Equal("1 / 1 ページ", vm.PageText);

        Assert.False(vm.IsLoading);
        Assert.False(vm.HasError);
        Assert.Equal(string.Empty, vm.ErrorMessage);
    }

    [Fact]
    public async Task LoadAsync_ShouldLoadRowsAndSummary()
    {
        var service = new FakeSalesService
        {
            SalesListToReturn = new SalesListResponseDto
            {
                Rows = new List<SalesListItemDto>
                {
                    new()
                    {
                        InvoiceId = 1,
                        InvoiceNumber = "INV-001",
                        ClientName = "山田 太郎",
                        InvoiceAmount = 10000m,
                        PaidAmount = 5000m,
                        RemainingAmount = 5000m
                    },
                    new()
                    {
                        InvoiceId = 2,
                        InvoiceNumber = "INV-002",
                        ClientName = "佐藤 花子",
                        InvoiceAmount = 20000m,
                        PaidAmount = 20000m,
                        RemainingAmount = 0m
                    }
                },
                TotalCount = 25,
                Summary = new SalesSummaryDto
                {
                    InvoiceTotal = 30000m,
                    PaidTotal = 25000m,
                    RemainingTotal = 5000m,
                    RecoveryRate = 83.3
                }
            }
        };

        var vm = new SalesListViewModel(service);

        vm.SelectedYear = 2026;
        vm.SelectedMonth = vm.MonthOptions.First(x => x.Value == 4);
        vm.SelectedStatus = vm.StatusOptions.First(x => x.Value == "partial");
        vm.Keyword = " 山田 ";
        vm.MemberId = 10;
        vm.CurrentPage = 2;
        vm.PageSize = 10;

        await vm.LoadAsync();

        Assert.Equal(1, service.GetSalesListCallCount);
        Assert.NotNull(service.LastSalesSearchRequest);

        Assert.Equal(2026, service.LastSalesSearchRequest!.Year);
        Assert.Equal(4, service.LastSalesSearchRequest.Month);
        Assert.Equal("partial", service.LastSalesSearchRequest.Status);
        Assert.Equal("山田", service.LastSalesSearchRequest.Keyword);
        Assert.Equal(10, service.LastSalesSearchRequest.MemberId);
        Assert.Equal(2, service.LastSalesSearchRequest.Page);
        Assert.Equal(10, service.LastSalesSearchRequest.PageSize);

        Assert.Equal(2, vm.Rows.Count);
        Assert.Equal("INV-001", vm.Rows[0].InvoiceNumber);
        Assert.Equal("INV-002", vm.Rows[1].InvoiceNumber);

        Assert.Equal(25, vm.TotalCount);
        Assert.Equal(30000m, vm.InvoiceTotal);
        Assert.Equal(25000m, vm.PaidTotal);
        Assert.Equal(5000m, vm.RemainingTotal);
        Assert.Equal(83.3, vm.RecoveryRate);

        Assert.Equal("￥30,000", vm.InvoiceTotalText);
        Assert.Equal("￥25,000", vm.PaidTotalText);
        Assert.Equal("￥5,000", vm.RemainingTotalText);
        Assert.Equal("83.3%", vm.RecoveryRateText);
        Assert.Equal("2 / 3 ページ", vm.PageText);

        Assert.False(vm.IsLoading);
        Assert.False(vm.HasError);
    }

    [Fact]
    public async Task LoadAsync_ShouldAdjustCurrentPage_WhenCurrentPageExceedsTotalPages()
    {
        var service = new FakeSalesService
        {
            SalesListToReturn = new SalesListResponseDto
            {
                Rows = new List<SalesListItemDto>(),
                TotalCount = 15,
                Summary = new SalesSummaryDto()
            }
        };

        var vm = new SalesListViewModel(service)
        {
            CurrentPage = 5,
            PageSize = 10
        };

        await vm.LoadAsync();

        Assert.Equal(2, vm.CurrentPage);
        Assert.Equal("2 / 2 ページ", vm.PageText);
    }

    [Fact]
    public async Task LoadAsync_ShouldSetError_WhenServiceThrows()
    {
        var service = new FakeSalesService
        {
            ExceptionToThrowOnGetSalesList = new InvalidOperationException("sales list error")
        };

        var vm = new SalesListViewModel(service);

        await vm.LoadAsync();

        Assert.True(vm.HasError);
        Assert.Contains("売上一覧の読込に失敗しました。", vm.ErrorMessage);
        Assert.Contains("sales list error", vm.ErrorMessage);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public void MovePreviousYear_ShouldDecrementSelectedYear()
    {
        var vm = new SalesListViewModel(new FakeSalesService())
        {
            SelectedYear = 2026
        };

        vm.MovePreviousYear();

        Assert.Equal(2025, vm.SelectedYear);
        Assert.Equal("2025年", vm.SelectedYearText);
    }

    [Fact]
    public void MoveNextYear_ShouldIncrementSelectedYear()
    {
        var vm = new SalesListViewModel(new FakeSalesService())
        {
            SelectedYear = 2026
        };

        vm.MoveNextYear();

        Assert.Equal(2027, vm.SelectedYear);
        Assert.Equal("2027年", vm.SelectedYearText);
    }

    [Fact]
    public async Task SearchAsync_ShouldResetCurrentPageAndLoad()
    {
        var service = new FakeSalesService
        {
            SalesListToReturn = new SalesListResponseDto
            {
                Rows = new List<SalesListItemDto>(),
                TotalCount = 1,
                Summary = new SalesSummaryDto()
            }
        };

        var vm = new SalesListViewModel(service)
        {
            CurrentPage = 3
        };

        await vm.SearchAsync();

        Assert.Equal(1, vm.CurrentPage);
        Assert.Equal(1, service.GetSalesListCallCount);
    }

    [Fact]
    public async Task MovePreviousPageAsync_ShouldMovePreviousAndLoad()
    {
        var service = new FakeSalesService
        {
            SalesListToReturn = new SalesListResponseDto
            {
                Rows = new List<SalesListItemDto>(),
                TotalCount = 30,
                Summary = new SalesSummaryDto()
            }
        };

        var vm = new SalesListViewModel(service)
        {
            CurrentPage = 2,
            PageSize = 10,
            TotalCount = 30
        };

        await vm.MovePreviousPageAsync();

        Assert.Equal(1, vm.CurrentPage);
        Assert.Equal(1, service.GetSalesListCallCount);
    }

    [Fact]
    public async Task MovePreviousPageAsync_ShouldDoNothing_WhenFirstPage()
    {
        var service = new FakeSalesService();

        var vm = new SalesListViewModel(service)
        {
            CurrentPage = 1,
            TotalCount = 30
        };

        await vm.MovePreviousPageAsync();

        Assert.Equal(1, vm.CurrentPage);
        Assert.Equal(0, service.GetSalesListCallCount);
    }

    [Fact]
    public async Task MoveNextPageAsync_ShouldMoveNextAndLoad()
    {
        var service = new FakeSalesService
        {
            SalesListToReturn = new SalesListResponseDto
            {
                Rows = new List<SalesListItemDto>(),
                TotalCount = 30,
                Summary = new SalesSummaryDto()
            }
        };

        var vm = new SalesListViewModel(service)
        {
            CurrentPage = 1,
            PageSize = 10,
            TotalCount = 30
        };

        await vm.MoveNextPageAsync();

        Assert.Equal(2, vm.CurrentPage);
        Assert.Equal(1, service.GetSalesListCallCount);
    }

    [Fact]
    public async Task MoveNextPageAsync_ShouldDoNothing_WhenLastPage()
    {
        var service = new FakeSalesService();

        var vm = new SalesListViewModel(service)
        {
            CurrentPage = 3,
            PageSize = 10,
            TotalCount = 30
        };

        await vm.MoveNextPageAsync();

        Assert.Equal(3, vm.CurrentPage);
        Assert.Equal(0, service.GetSalesListCallCount);
    }

    [Fact]
    public void MemberId_ShouldUpdateIsMemberFiltered()
    {
        var vm = new SalesListViewModel(new FakeSalesService());

        Assert.False(vm.IsMemberFiltered);

        vm.MemberId = 123;

        Assert.True(vm.IsMemberFiltered);

        vm.MemberId = null;

        Assert.False(vm.IsMemberFiltered);
    }
}
