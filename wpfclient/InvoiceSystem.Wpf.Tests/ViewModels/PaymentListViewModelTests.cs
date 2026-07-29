using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Tests.Fakes;
using InvoiceSystem.Wpf.ViewModels;
using Xunit;

namespace InvoiceSystem.Wpf.Tests.ViewModels;

public class PaymentListViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitializeDefaultOptions()
    {
        var service = new FakePaymentService();

        var vm = new PaymentListViewModel(service);

        var currentYear = DateTime.Today.Year;

        Assert.Equal(currentYear, vm.SelectedYear);
        Assert.Equal(3, vm.YearOptions.Count);
        Assert.Contains(currentYear - 1, vm.YearOptions);
        Assert.Contains(currentYear, vm.YearOptions);
        Assert.Contains(currentYear + 1, vm.YearOptions);

        Assert.Equal(13, vm.MonthOptions.Count);
        Assert.Equal("all", vm.SelectedMonth?.Value);
        Assert.Equal("すべて", vm.SelectedMonth?.Label);

        Assert.Equal(4, vm.StatusOptions.Count);
        Assert.Equal("all", vm.SelectedStatus?.Value);
        Assert.Equal("すべて", vm.SelectedStatus?.Label);

        Assert.Empty(vm.Payments);
        Assert.Equal(1, vm.CurrentPage);
        Assert.Equal(10, vm.PageSize);
        Assert.Equal("1 / 1 ページ", vm.CurrentPageText);
        Assert.Equal("入金一覧（0件）", vm.ResultCountText);
        Assert.Equal("￥0", vm.TotalAmountText);
        Assert.Equal("￥0", vm.AllocatedTotalText);
        Assert.Equal("￥0", vm.UnallocatedTotalText);
    }

    [Fact]
    public async Task LoadAsync_ShouldLoadPaymentsAndSummary()
    {
        var service = new FakePaymentService
        {
            SearchResultToReturn = new PaymentListResultDto
            {
                Rows = new List<PaymentListItemDto>
                {
                    new()
                    {
                        Id = 1,
                        PayerName = "山田 太郎",
                        Amount = 10000m,
                        Status = "UNALLOCATED"
                    },
                    new()
                    {
                        Id = 2,
                        PayerName = "佐藤 花子",
                        Amount = 20000m,
                        Status = "ALLOCATED"
                    }
                },
                TotalCount = 25,
                Summary = new PaymentSummaryDto
                {
                    TotalAmount = 30000m,
                    AllocatedTotal = 20000m,
                    UnallocatedTotal = 10000m
                }
            }
        };

        var vm = new PaymentListViewModel(service);

        vm.SelectedYear = 2026;
        vm.SelectedMonth = vm.MonthOptions.First(x => x.Value == "4");
        vm.SelectedStatus = vm.StatusOptions.First(x => x.Value == "UNALLOCATED");
        vm.Keyword = " 山田 ";
        vm.CurrentPage = 2;
        vm.PageSize = 10;

        await vm.LoadAsync();

        Assert.Equal(1, service.SearchCallCount);
        Assert.NotNull(service.LastSearchRequest);
        Assert.Equal(2026, service.LastSearchRequest!.Year);
        Assert.Equal("4", service.LastSearchRequest.Month);
        Assert.Equal("UNALLOCATED", service.LastSearchRequest.Status);
        Assert.Equal(" 山田 ", service.LastSearchRequest.Q);
        Assert.Equal(2, service.LastSearchRequest.Page);
        Assert.Equal(10, service.LastSearchRequest.PageSize);

        Assert.Equal(2, vm.Payments.Count);
        Assert.Equal("山田 太郎", vm.Payments[0].PayerName);
        Assert.Equal("佐藤 花子", vm.Payments[1].PayerName);

        Assert.Equal(25, vm.TotalCount);
        Assert.Equal(30000m, vm.TotalAmount);
        Assert.Equal(20000m, vm.AllocatedTotal);
        Assert.Equal(10000m, vm.UnallocatedTotal);

        Assert.Equal("2 / 3 ページ", vm.CurrentPageText);
        Assert.Equal("入金一覧（25件）", vm.ResultCountText);
        Assert.Equal("￥30,000", vm.TotalAmountText);
        Assert.Equal("￥20,000", vm.AllocatedTotalText);
        Assert.Equal("￥10,000", vm.UnallocatedTotalText);

        Assert.False(vm.IsLoading);
        Assert.False(vm.HasError);
        Assert.Equal(string.Empty, vm.ErrorMessage);
    }

    [Fact]
    public async Task LoadAsync_ShouldUseDefaultFilterValues_WhenSelectedOptionsAreNull()
    {
        var service = new FakePaymentService
        {
            SearchResultToReturn = new PaymentListResultDto()
        };

        var vm = new PaymentListViewModel(service)
        {
            SelectedMonth = null,
            SelectedStatus = null,
            Keyword = null!
        };

        await vm.LoadAsync();

        Assert.NotNull(service.LastSearchRequest);
        Assert.Equal("all", service.LastSearchRequest!.Month);
        Assert.Equal("all", service.LastSearchRequest.Status);
        Assert.Equal(string.Empty, service.LastSearchRequest.Q);
    }

    [Fact]
    public async Task LoadAsync_ShouldSetErrorAndClearValues_WhenServiceThrows()
    {
        var service = new FakePaymentService
        {
            ExceptionToThrowOnSearch = new InvalidOperationException("search error")
        };

        var vm = new PaymentListViewModel(service);

        vm.Payments.Add(new PaymentListItemDto { Id = 1, PayerName = "既存データ", Amount = 1000m });
        vm.TotalCount = 10;
        vm.TotalAmount = 1000m;
        vm.AllocatedTotal = 500m;
        vm.UnallocatedTotal = 500m;

        await vm.LoadAsync();

        Assert.True(vm.HasError);
        Assert.Contains("search error", vm.ErrorMessage);
        Assert.Empty(vm.Payments);
        Assert.Equal(0, vm.TotalCount);
        Assert.Equal(0m, vm.TotalAmount);
        Assert.Equal(0m, vm.AllocatedTotal);
        Assert.Equal(0m, vm.UnallocatedTotal);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public void ResetSearch_ShouldResetFiltersButKeepPage()
    {
        var service = new FakePaymentService();
        var vm = new PaymentListViewModel(service);

        vm.SelectedYear = 2025;
        vm.SelectedMonth = vm.MonthOptions.First(x => x.Value == "4");
        vm.SelectedStatus = vm.StatusOptions.First(x => x.Value == "ALLOCATED");
        vm.Keyword = "test";
        vm.CurrentPage = 3;

        vm.ResetSearch();

        Assert.Equal(DateTime.Today.Year, vm.SelectedYear);
        Assert.Equal("all", vm.SelectedMonth?.Value);
        Assert.Equal("all", vm.SelectedStatus?.Value);
        Assert.Equal(string.Empty, vm.Keyword);

        // 現在の実装では CurrentPage はリセットされない
        Assert.Equal(3, vm.CurrentPage);
    }

    [Fact]
    public async Task MovePrevPageAsync_ShouldMovePreviousAndLoad()
    {
        var service = new FakePaymentService
        {
            SearchResultToReturn = new PaymentListResultDto
            {
                Rows = new List<PaymentListItemDto>(),
                TotalCount = 30,
                Summary = new PaymentSummaryDto()
            }
        };

        var vm = new PaymentListViewModel(service)
        {
            CurrentPage = 2,
            PageSize = 10,
            TotalCount = 30
        };

        await vm.MovePrevPageAsync();

        Assert.Equal(1, vm.CurrentPage);
        Assert.Equal(1, service.SearchCallCount);
        Assert.Equal(1, service.LastSearchRequest!.Page);
    }

    [Fact]
    public async Task MovePrevPageAsync_ShouldDoNothing_WhenCannotMovePrev()
    {
        var service = new FakePaymentService();
        var vm = new PaymentListViewModel(service)
        {
            CurrentPage = 1,
            TotalCount = 30
        };

        await vm.MovePrevPageAsync();

        Assert.Equal(1, vm.CurrentPage);
        Assert.Equal(0, service.SearchCallCount);
    }

    [Fact]
    public async Task MoveNextPageAsync_ShouldMoveNextAndLoad()
    {
        var service = new FakePaymentService
        {
            SearchResultToReturn = new PaymentListResultDto
            {
                Rows = new List<PaymentListItemDto>(),
                TotalCount = 30,
                Summary = new PaymentSummaryDto()
            }
        };

        var vm = new PaymentListViewModel(service)
        {
            CurrentPage = 1,
            PageSize = 10,
            TotalCount = 30
        };

        await vm.MoveNextPageAsync();

        Assert.Equal(2, vm.CurrentPage);
        Assert.Equal(1, service.SearchCallCount);
        Assert.Equal(2, service.LastSearchRequest!.Page);
    }

    [Fact]
    public async Task MoveNextPageAsync_ShouldDoNothing_WhenCannotMoveNext()
    {
        var service = new FakePaymentService();
        var vm = new PaymentListViewModel(service)
        {
            CurrentPage = 3,
            PageSize = 10,
            TotalCount = 30
        };

        await vm.MoveNextPageAsync();

        Assert.Equal(3, vm.CurrentPage);
        Assert.Equal(0, service.SearchCallCount);
    }

    [Fact]
    public async Task LoadAsync_ShouldAdjustCurrentPage_WhenCurrentPageExceedsTotalPages()
    {
        var service = new FakePaymentService
        {
            SearchResultToReturn = new PaymentListResultDto
            {
                Rows = new List<PaymentListItemDto>(),
                TotalCount = 15,
                Summary = new PaymentSummaryDto()
            }
        };

        var vm = new PaymentListViewModel(service)
        {
            CurrentPage = 5,
            PageSize = 10
        };

        await vm.LoadAsync();

        Assert.Equal(2, vm.CurrentPage);
        Assert.Equal("2 / 2 ページ", vm.CurrentPageText);
    }
}