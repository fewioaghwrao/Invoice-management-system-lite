using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Tests.Fakes;
using InvoiceSystem.Wpf.ViewModels;
using System;
using System.Linq;
using System.Windows;
using Xunit;

namespace InvoiceSystem.Wpf.Tests.ViewModels;

public sealed class MemberInvoiceListViewModelTests
{
    private sealed class TestUser
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
    }

    [StaFact]
    public async Task InitializeAsync_WhenInvoicesExist_BindsRowsAndSummary()
    {
        var service = new FakeInvoiceService
        {
            MemberInvoicesToReturn = new AccountInvoiceListDto
            {
                Page = 1,
                PageSize = 10,
                TotalCount = 2,
                AvailableYears = new() { 2026, 2025 },
                Items = new()
                {
                    new AccountInvoiceListItemDto
                    {
                        Id = 101,
                        InvoiceNumber = "INV-001",
                        IssuedAt = new DateTime(2026, 4, 1),
                        DueAt = new DateTime(2026, 4, 30),
                        TotalAmount = 120000,
                        RemainingAmount = 120000,
                        StatusName = "未入金",
                        IsOverdue = true
                    },
                    new AccountInvoiceListItemDto
                    {
                        Id = 102,
                        InvoiceNumber = "INV-002",
                        IssuedAt = new DateTime(2026, 4, 2),
                        DueAt = new DateTime(2026, 5, 31),
                        TotalAmount = 50000,
                        RemainingAmount = 0,
                        StatusName = "入金済み",
                        IsOverdue = false
                    }
                }
            }
        };

        var vm = new MemberInvoiceListViewModel(
            new TestUser { Name = "山田太郎", Email = "yamada@example.com" },
            service,
            _ => { },
            () => { });

        await vm.InitializeAsync();

        Assert.Equal("ようこそ、山田太郎さん", vm.WelcomeMessage);
        Assert.Equal("yamada@example.com の請求書を表示しています", vm.SubWelcomeMessage);
        Assert.Equal(2, vm.InvoiceRows.Count);
        Assert.Equal("INV-001", vm.InvoiceRows[0].InvoiceNumber);
        Assert.Equal("120,000 円", vm.InvoiceRows[0].TotalAmountText);
        Assert.Equal("2 件", vm.SummaryText);
        Assert.Equal("1 / 1", vm.PageInfoText);
        Assert.True(vm.IsOverdueBannerVisible);
        Assert.Equal(Visibility.Visible, vm.OverdueBannerVisibility);
        Assert.False(vm.IsLoading);
        Assert.Equal(Visibility.Collapsed, vm.LoadingVisibility);
    }

    [StaFact]
    public async Task InitializeAsync_WhenNoCurrentUser_UsesDefaultHeader()
    {
        var service = new FakeInvoiceService
        {
            MemberInvoicesToReturn = new AccountInvoiceListDto()
        };

        var vm = new MemberInvoiceListViewModel(
            null,
            service,
            _ => { },
            () => { });

        await vm.InitializeAsync();

        Assert.Equal("ようこそ、会員ユーザーさん", vm.WelcomeMessage);
        Assert.Equal("メールアドレス未設定 の請求書を表示しています", vm.SubWelcomeMessage);
    }

    [StaFact]
    public async Task InitializeAsync_SendsDefaultSearchCondition()
    {
        var service = new FakeInvoiceService
        {
            MemberInvoicesToReturn = new AccountInvoiceListDto()
        };

        var vm = new MemberInvoiceListViewModel(
            null,
            service,
            _ => { },
            () => { });

        await vm.InitializeAsync();

        Assert.Equal(DateTime.Now.Year, service.LastMemberInvoicesYear);
        Assert.Equal("all", service.LastMemberInvoicesMonth);
        Assert.Equal("all", service.LastMemberInvoicesStatus);
        Assert.Equal("", service.LastMemberInvoicesKeyword);
        Assert.Equal(1, service.LastMemberInvoicesPage);
        Assert.Equal(10, service.LastMemberInvoicesPageSize);
    }

    [StaFact]
    public async Task SearchCommand_WhenFilterChanged_SendsSearchConditionAndResetPage()
    {
        var service = new FakeInvoiceService
        {
            MemberInvoicesToReturn = new AccountInvoiceListDto
            {
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            }
        };

        var vm = new MemberInvoiceListViewModel(
            null,
            service,
            _ => { },
            () => { });

        vm.SelectedYear = 2025;
        vm.SelectedMonthOption = vm.MonthOptions.First(x => x.Value == "4");
        vm.SelectedStatusOption = vm.StatusOptions.First(x => x.Value == "unpaid");
        vm.Keyword = "  INV-001  ";

        vm.SearchCommand.Execute(null);

        await WaitUntilNotLoadingAsync(vm);

        Assert.Equal(2025, service.LastMemberInvoicesYear);
        Assert.Equal("4", service.LastMemberInvoicesMonth);
        Assert.Equal("unpaid", service.LastMemberInvoicesStatus);
        Assert.Equal("INV-001", service.LastMemberInvoicesKeyword);
        Assert.Equal(1, service.LastMemberInvoicesPage);
    }

    [StaFact]
    public async Task ClearCommand_ResetsFiltersAndLoadsFirstPage()
    {
        var service = new FakeInvoiceService
        {
            MemberInvoicesToReturn = new AccountInvoiceListDto()
        };

        var vm = new MemberInvoiceListViewModel(
            null,
            service,
            _ => { },
            () => { });

        vm.SelectedYear = 2025;
        vm.SelectedMonthOption = vm.MonthOptions.First(x => x.Value == "4");
        vm.SelectedStatusOption = vm.StatusOptions.First(x => x.Value == "paid");
        vm.Keyword = "abc";

        vm.ClearCommand.Execute(null);

        await WaitUntilNotLoadingAsync(vm);

        Assert.Equal(DateTime.Now.Year, vm.SelectedYear);
        Assert.Equal("all", vm.SelectedMonthOption?.Value);
        Assert.Equal("all", vm.SelectedStatusOption?.Value);
        Assert.Equal("", vm.Keyword);
        Assert.Equal(1, service.LastMemberInvoicesPage);
    }

    [StaFact]
    public async Task Paging_WhenNextAndPrevious_ChangesPage()
    {
        var service = new FakeInvoiceService
        {
            MemberInvoicesToReturn = new AccountInvoiceListDto
            {
                Page = 1,
                PageSize = 10,
                TotalCount = 25
            }
        };

        var vm = new MemberInvoiceListViewModel(
            null,
            service,
            _ => { },
            () => { });

        await vm.InitializeAsync();

        Assert.True(vm.CanGoNext);
        Assert.False(vm.CanGoPrevious);
        Assert.Equal("1 / 3", vm.PageInfoText);

        service.MemberInvoicesToReturn = new AccountInvoiceListDto
        {
            Page = 2,
            PageSize = 10,
            TotalCount = 25
        };

        vm.NextCommand.Execute(null);
        await WaitUntilNotLoadingAsync(vm);

        Assert.Equal(2, service.LastMemberInvoicesPage);
        Assert.True(vm.CanGoPrevious);
        Assert.True(vm.CanGoNext);
        Assert.Equal("2 / 3", vm.PageInfoText);

        service.MemberInvoicesToReturn = new AccountInvoiceListDto
        {
            Page = 1,
            PageSize = 10,
            TotalCount = 25
        };

        vm.PrevCommand.Execute(null);
        await WaitUntilNotLoadingAsync(vm);

        Assert.Equal(1, service.LastMemberInvoicesPage);
        Assert.False(vm.CanGoPrevious);
        Assert.True(vm.CanGoNext);
        Assert.Equal("1 / 3", vm.PageInfoText);
    }

    [StaFact]
    public async Task InitializeAsync_WhenOnlyPaidOverdueRows_BannerIsHidden()
    {
        var service = new FakeInvoiceService
        {
            MemberInvoicesToReturn = new AccountInvoiceListDto
            {
                Page = 1,
                PageSize = 10,
                TotalCount = 1,
                Items = new()
                {
                    new AccountInvoiceListItemDto
                    {
                        Id = 1,
                        InvoiceNumber = "INV-PAID",
                        IssuedAt = DateTime.Today,
                        DueAt = DateTime.Today.AddDays(-10),
                        TotalAmount = 10000,
                        RemainingAmount = 0,
                        StatusName = "入金済み",
                        IsOverdue = true
                    }
                }
            }
        };

        var vm = new MemberInvoiceListViewModel(
            null,
            service,
            _ => { },
            () => { });

        await vm.InitializeAsync();

        Assert.False(vm.IsOverdueBannerVisible);
        Assert.Equal(Visibility.Collapsed, vm.OverdueBannerVisibility);
    }

    [StaFact]
    public void OpenDetailCommand_WhenRowPassed_CallsOpenDetail()
    {
        long? openedId = null;

        var vm = new MemberInvoiceListViewModel(
            null,
            new FakeInvoiceService(),
            id => openedId = id,
            () => { });

        var row = new MemberInvoiceRowViewModel
        {
            Id = 123,
            InvoiceNumber = "INV-123"
        };

        Assert.True(vm.OpenDetailCommand.CanExecute(row));

        vm.OpenDetailCommand.Execute(row);

        Assert.Equal(123, openedId);
    }

    [StaFact]
    public void BackToDashboardCommand_WhenExecuted_CallsOpenDashboard()
    {
        var called = false;

        var vm = new MemberInvoiceListViewModel(
            null,
            new FakeInvoiceService(),
            _ => { },
            () => called = true);

        vm.BackToDashboardCommand.Execute(null);

        Assert.True(called);
    }

    [StaFact]
    public async Task InitializeAsync_WhenServiceThrows_ClearsRowsAndSummary()
    {
        var service = new FakeInvoiceService
        {
            ExceptionToThrowOnGetMemberInvoices = new InvalidOperationException("API error")
        };

        var vm = new MemberInvoiceListViewModel(
            null,
            service,
            _ => { },
            () => { });

        await vm.InitializeAsync();

        Assert.Empty(vm.InvoiceRows);
        Assert.Equal("0 件", vm.SummaryText);
        Assert.Equal("1 / 1", vm.PageInfoText);
        Assert.False(vm.IsOverdueBannerVisible);
        Assert.False(vm.IsLoading);
    }

    private static async Task WaitUntilNotLoadingAsync(MemberInvoiceListViewModel vm)
    {
        for (var i = 0; i < 50; i++)
        {
            if (!vm.IsLoading)
                return;

            await Task.Delay(10);
        }

        throw new TimeoutException("ViewModel の非同期処理が完了しませんでした。");
    }
}