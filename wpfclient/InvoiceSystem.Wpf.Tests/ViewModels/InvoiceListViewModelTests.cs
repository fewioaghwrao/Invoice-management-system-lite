using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Tests.Fakes;
using InvoiceSystem.Wpf.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace InvoiceSystem.Wpf.Tests.ViewModels;

public class InvoiceListViewModelTests
{
    [Fact]
    public void Constructor_InitialValues_AreExpected()
    {
        var service = new FakeInvoiceService();

        var vm = new InvoiceListViewModel(service);

        Assert.Equal(6, vm.StatusOptions.Count);
        Assert.Equal("すべて", vm.StatusOptions[0].Name);
        Assert.Null(vm.StatusOptions[0].Id);

        Assert.Empty(vm.Invoices);
        Assert.Equal(string.Empty, vm.InvoiceNumber);
        Assert.Equal(string.Empty, vm.MemberName);
        Assert.Null(vm.SelectedStatus);
        Assert.Null(vm.FromInvoiceDate);
        Assert.Null(vm.ToInvoiceDate);

        Assert.False(vm.IsLoading);
        Assert.False(vm.HasError);
        Assert.Equal(string.Empty, vm.ErrorMessage);

        Assert.Equal("0件", vm.ResultSummary);
        Assert.Null(vm.SelectedInvoice);

        Assert.Equal(1, vm.CurrentPage);
        Assert.Equal(20, vm.PageSize);
        Assert.False(vm.HasNextPage);
        Assert.False(vm.CanMovePrev);
        Assert.False(vm.CanMoveNext);
        Assert.Equal("1 ページ", vm.PageInfoText);
    }

    [Fact]
    public async Task LoadAsync_Success_BuildsRequestAndAppliesItems()
    {
        var service = new FakeInvoiceService
        {
            SearchInvoicesResultToReturn = new List<InvoiceListItemDto>
            {
                new()
                {
                    Id = 1,
                    InvoiceNumber = "INV-001",
                    MemberName = "会員A",
                    StatusId = 1,
                    StatusName = "未入金",
                    InvoiceDate = new DateTime(2026, 4, 1),
                    DueDate = new DateTime(2026, 4, 30),
                    TotalAmount = 10000m
                },
                new()
                {
                    Id = 2,
                    InvoiceNumber = "INV-002",
                    MemberName = "会員B",
                    StatusId = 2,
                    StatusName = "一部入金",
                    InvoiceDate = new DateTime(2026, 4, 2),
                    DueDate = new DateTime(2026, 4, 28),
                    TotalAmount = 20000m
                }
            }
        };

        var vm = new InvoiceListViewModel(service)
        {
            InvoiceNumber = "  INV-ABC  ",
            MemberName = "  山田太郎  ",
            SelectedStatus = new InvoiceStatusOption { Id = 4, Name = "期限超過" },
            FromInvoiceDate = new DateTime(2026, 4, 1),
            ToInvoiceDate = new DateTime(2026, 4, 30),
            CurrentPage = 3,
            PageSize = 20
        };

        await vm.LoadAsync();

        Assert.False(vm.IsLoading);
        Assert.False(vm.HasError);
        Assert.Equal(string.Empty, vm.ErrorMessage);

        Assert.NotNull(service.LastSearchInvoicesRequest);
        Assert.Equal("INV-ABC", service.LastSearchInvoicesRequest!.InvoiceNumber);
        Assert.Equal("山田太郎", service.LastSearchInvoicesRequest.MemberName);
        Assert.Equal(4, service.LastSearchInvoicesRequest.StatusId);
        Assert.Equal(new DateTime(2026, 4, 1), service.LastSearchInvoicesRequest.FromInvoiceDate);
        Assert.Equal(new DateTime(2026, 4, 30), service.LastSearchInvoicesRequest.ToInvoiceDate);
        Assert.Equal(3, service.LastSearchInvoicesRequest.Page);
        Assert.Equal(20, service.LastSearchInvoicesRequest.PageSize);

        Assert.Equal(2, vm.Invoices.Count);
        Assert.Equal("INV-001", vm.Invoices[0].InvoiceNumber);
        Assert.Equal("会員A", vm.Invoices[0].MemberName);
        Assert.Equal("INV-002", vm.Invoices[1].InvoiceNumber);
        Assert.Equal("会員B", vm.Invoices[1].MemberName);

        Assert.False(vm.HasNextPage);
        Assert.False(vm.CanMoveNext);
        Assert.True(vm.CanMovePrev);
        Assert.Equal("3ページ / 2件表示", vm.ResultSummary);
    }

    [Fact]
    public async Task LoadAsync_WhenItemCountEqualsPageSize_SetsHasNextPageTrue()
    {
        var service = new FakeInvoiceService
        {
            SearchInvoicesResultToReturn = Enumerable.Range(1, 20)
                .Select(i => new InvoiceListItemDto
                {
                    Id = i,
                    InvoiceNumber = $"INV-{i:000}",
                    MemberName = $"会員{i}",
                    StatusName = "未入金",
                    TotalAmount = i * 1000m
                })
                .ToList()
        };

        var vm = new InvoiceListViewModel(service)
        {
            CurrentPage = 1,
            PageSize = 20
        };

        await vm.LoadAsync();

        Assert.Equal(20, vm.Invoices.Count);
        Assert.True(vm.HasNextPage);
        Assert.True(vm.CanMoveNext);
        Assert.False(vm.CanMovePrev);
        Assert.Equal("1ページ / 20件表示", vm.ResultSummary);
    }

    [Fact]
    public async Task LoadAsync_WhenInputsAreWhitespace_SendsNulls()
    {
        var service = new FakeInvoiceService
        {
            SearchInvoicesResultToReturn = new List<InvoiceListItemDto>()
        };

        var vm = new InvoiceListViewModel(service)
        {
            InvoiceNumber = "   ",
            MemberName = "   ",
            SelectedStatus = null,
            FromInvoiceDate = null,
            ToInvoiceDate = null
        };

        await vm.LoadAsync();

        Assert.NotNull(service.LastSearchInvoicesRequest);
        Assert.Null(service.LastSearchInvoicesRequest!.InvoiceNumber);
        Assert.Null(service.LastSearchInvoicesRequest.MemberName);
        Assert.Null(service.LastSearchInvoicesRequest.StatusId);
        Assert.Null(service.LastSearchInvoicesRequest.FromInvoiceDate);
        Assert.Null(service.LastSearchInvoicesRequest.ToInvoiceDate);
    }

    [Fact]
    public async Task LoadAsync_Failure_SetsError_ClearsInvoices_AndResetsSummary()
    {
        var service = new FakeInvoiceService
        {
            ExceptionToThrowOnSearchInvoices = new InvalidOperationException("search failed")
        };

        var vm = new InvoiceListViewModel(service);
        vm.Invoices.Add(new InvoiceListItemDto { Id = 99, InvoiceNumber = "OLD" });
        vm.ResultSummary = "1ページ / 1件表示";

        await vm.LoadAsync();

        Assert.False(vm.IsLoading);
        Assert.True(vm.HasError);
        Assert.Contains("請求書一覧の取得に失敗しました。", vm.ErrorMessage);
        Assert.Contains("search failed", vm.ErrorMessage);

        Assert.Empty(vm.Invoices);
        Assert.Equal("0件", vm.ResultSummary);
    }

    [Fact]
    public void ResetSearch_ResetsAllConditions()
    {
        var service = new FakeInvoiceService();
        var vm = new InvoiceListViewModel(service)
        {
            InvoiceNumber = "INV-001",
            MemberName = "会員A",
            SelectedStatus = new InvoiceStatusOption { Id = 2, Name = "一部入金" },
            FromInvoiceDate = new DateTime(2026, 4, 1),
            ToInvoiceDate = new DateTime(2026, 4, 30)
        };

        vm.ResetSearch();

        Assert.Equal(string.Empty, vm.InvoiceNumber);
        Assert.Equal(string.Empty, vm.MemberName);
        Assert.NotNull(vm.SelectedStatus);
        Assert.Equal("すべて", vm.SelectedStatus!.Name);
        Assert.Null(vm.SelectedStatus.Id);
        Assert.Null(vm.FromInvoiceDate);
        Assert.Null(vm.ToInvoiceDate);
    }

    [Theory]
    [InlineData(0, "￥0")]
    [InlineData(1000, "￥1,000")]
    [InlineData(123456, "￥123,456")]
    public void FormatCurrency_ReturnsJaJpCurrency(decimal value, string expected)
    {
        var service = new FakeInvoiceService();
        var vm = new InvoiceListViewModel(service);

        var result = vm.FormatCurrency(value);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task MovePrevPageAsync_WhenCurrentPageIsOne_DoesNothing()
    {
        var service = new FakeInvoiceService();
        var vm = new InvoiceListViewModel(service)
        {
            CurrentPage = 1
        };

        await vm.MovePrevPageAsync();

        Assert.Equal(1, vm.CurrentPage);
        Assert.Null(service.LastSearchInvoicesRequest);
    }

    [Fact]
    public async Task MovePrevPageAsync_WhenCurrentPageIsGreaterThanOne_MovesAndLoads()
    {
        var service = new FakeInvoiceService
        {
            SearchInvoicesResultToReturn = new List<InvoiceListItemDto>
            {
                new() { Id = 1, InvoiceNumber = "INV-001", MemberName = "会員A" }
            }
        };

        var vm = new InvoiceListViewModel(service)
        {
            CurrentPage = 3,
            PageSize = 20
        };

        await vm.MovePrevPageAsync();

        Assert.Equal(2, vm.CurrentPage);
        Assert.NotNull(service.LastSearchInvoicesRequest);
        Assert.Equal(2, service.LastSearchInvoicesRequest!.Page);
        Assert.Equal("2 ページ", vm.PageInfoText);
        Assert.True(vm.CanMovePrev);
    }

    [Fact]
    public async Task MoveNextPageAsync_WhenHasNextPageFalse_DoesNothing()
    {
        var service = new FakeInvoiceService();
        var vm = new InvoiceListViewModel(service)
        {
            CurrentPage = 1,
            HasNextPage = false
        };

        await vm.MoveNextPageAsync();

        Assert.Equal(1, vm.CurrentPage);
        Assert.Null(service.LastSearchInvoicesRequest);
    }

    [Fact]
    public async Task MoveNextPageAsync_WhenHasNextPageTrue_MovesAndLoads()
    {
        var service = new FakeInvoiceService
        {
            SearchInvoicesResultToReturn = new List<InvoiceListItemDto>
            {
                new() { Id = 1, InvoiceNumber = "INV-021", MemberName = "会員X" }
            }
        };

        var vm = new InvoiceListViewModel(service)
        {
            CurrentPage = 1,
            HasNextPage = true,
            PageSize = 20
        };

        await vm.MoveNextPageAsync();

        Assert.Equal(2, vm.CurrentPage);
        Assert.NotNull(service.LastSearchInvoicesRequest);
        Assert.Equal(2, service.LastSearchInvoicesRequest!.Page);
        Assert.Equal("2 ページ", vm.PageInfoText);
        Assert.True(vm.CanMovePrev);
    }

    [Fact]
    public void PageInfo_And_CanMovePrev_ReflectCurrentPage()
    {
        var service = new FakeInvoiceService();
        var vm = new InvoiceListViewModel(service);

        Assert.False(vm.CanMovePrev);
        Assert.Equal("1 ページ", vm.PageInfoText);

        vm.CurrentPage = 5;

        Assert.True(vm.CanMovePrev);
        Assert.Equal("5 ページ", vm.PageInfoText);
    }

    [Fact]
    public void CanMoveNext_ReflectsHasNextPage()
    {
        var service = new FakeInvoiceService();
        var vm = new InvoiceListViewModel(service);

        vm.HasNextPage = false;
        Assert.False(vm.CanMoveNext);

        vm.HasNextPage = true;
        Assert.True(vm.CanMoveNext);
    }
}