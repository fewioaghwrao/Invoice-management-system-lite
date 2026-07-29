using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Tests.Fakes;
using InvoiceSystem.Wpf.ViewModels;
using System.Windows;
using Xunit;

namespace InvoiceSystem.Wpf.Tests.ViewModels;

public class MemberPaymentStatusViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitializeUserHeader()
    {
        var service = new FakeInvoiceService();
        var currentUser = new
        {
            Name = "山田太郎",
            Email = "yamada@example.com"
        };

        var vm = new MemberPaymentStatusViewModel(
            currentUser,
            service,
            _ => { },
            () => { });

        Assert.Equal("ようこそ、山田太郎さん", vm.WelcomeMessage);
        Assert.Equal("yamada@example.com の未払い状況を表示しています", vm.SubWelcomeMessage);
    }

    [Fact]
    public void Constructor_ShouldUseDefaultUserHeader_WhenCurrentUserIsNull()
    {
        var service = new FakeInvoiceService();

        var vm = new MemberPaymentStatusViewModel(
            null,
            service,
            _ => { },
            () => { });

        Assert.Equal("ようこそ、会員ユーザーさん", vm.WelcomeMessage);
        Assert.Equal("メールアドレス未設定 の未払い状況を表示しています", vm.SubWelcomeMessage);
    }

    [Fact]
    public async Task InitializeAsync_ShouldLoadUnpaidAndPartialInvoices()
    {
        var service = new FakeInvoiceService
        {
            MemberInvoicesWithBalanceToReturnForUnpaid = new AccountInvoiceListDto
            {
                Items = new List<AccountInvoiceListItemDto>
                {
                    new()
                    {
                        Id = 1,
                        InvoiceNumber = "INV-001",
                        DueAt = new DateTime(2026, 4, 10),
                        RemainingAmount = 10000m,
                        StatusName = "未入金",
                        IsOverdue = true
                    }
                }
            },
            MemberInvoicesWithBalanceToReturnForPartial = new AccountInvoiceListDto
            {
                Items = new List<AccountInvoiceListItemDto>
                {
                    new()
                    {
                        Id = 2,
                        InvoiceNumber = "INV-002",
                        DueAt = new DateTime(2026, 4, 20),
                        RemainingAmount = 5000m,
                        StatusName = "一部入金",
                        IsOverdue = false
                    }
                }
            }
        };

        var vm = new MemberPaymentStatusViewModel(
            new { Name = "Test", Email = "test@example.com" },
            service,
            _ => { },
            () => { });

        await vm.InitializeAsync();

        Assert.Equal(2, vm.Rows.Count);

        Assert.Equal(1, vm.Rows[0].Id);
        Assert.Equal("INV-001", vm.Rows[0].InvoiceNumber);

        Assert.Equal(2, vm.Rows[1].Id);
        Assert.Equal("INV-002", vm.Rows[1].InvoiceNumber);

        Assert.Equal("2 件", vm.SummaryText);
        Assert.Equal("2 件", vm.UnpaidCountText);
        Assert.Equal("15,000 円", vm.RemainingTotalText);
        Assert.Equal("1 件", vm.OverdueCountText);
        Assert.True(vm.IsOverdueBannerVisible);
        Assert.Equal(Visibility.Visible, vm.OverdueBannerVisibility);

        Assert.Contains("unpaid", service.GetMemberInvoicesWithBalanceStatuses);
        Assert.Contains("partial", service.GetMemberInvoicesWithBalanceStatuses);
        Assert.Equal(50, service.LastMemberInvoicesWithBalancePageSize);
    }

    [Fact]
    public async Task InitializeAsync_ShouldSortRowsByDueAt()
    {
        var service = new FakeInvoiceService
        {
            MemberInvoicesWithBalanceToReturnForUnpaid = new AccountInvoiceListDto
            {
                Items = new List<AccountInvoiceListItemDto>
                {
                    new()
                    {
                        Id = 2,
                        InvoiceNumber = "INV-002",
                        DueAt = new DateTime(2026, 5, 20),
                        RemainingAmount = 2000m,
                        StatusName = "未入金",
                        IsOverdue = false
                    }
                }
            },
            MemberInvoicesWithBalanceToReturnForPartial = new AccountInvoiceListDto
            {
                Items = new List<AccountInvoiceListItemDto>
                {
                    new()
                    {
                        Id = 1,
                        InvoiceNumber = "INV-001",
                        DueAt = new DateTime(2026, 4, 10),
                        RemainingAmount = 1000m,
                        StatusName = "一部入金",
                        IsOverdue = false
                    }
                }
            }
        };

        var vm = new MemberPaymentStatusViewModel(
            null,
            service,
            _ => { },
            () => { });

        await vm.InitializeAsync();

        Assert.Equal(2, vm.Rows.Count);
        Assert.Equal(1, vm.Rows[0].Id);
        Assert.Equal(2, vm.Rows[1].Id);
    }

    [Fact]
    public async Task InitializeAsync_ShouldTreatNegativeRemainingAmountAsZeroInSummary()
    {
        var service = new FakeInvoiceService
        {
            MemberInvoicesWithBalanceToReturnForUnpaid = new AccountInvoiceListDto
            {
                Items = new List<AccountInvoiceListItemDto>
                {
                    new()
                    {
                        Id = 1,
                        InvoiceNumber = "INV-001",
                        DueAt = new DateTime(2026, 4, 10),
                        RemainingAmount = -1000m,
                        StatusName = "未入金",
                        IsOverdue = true
                    }
                }
            },
            MemberInvoicesWithBalanceToReturnForPartial = new AccountInvoiceListDto
            {
                Items = new List<AccountInvoiceListItemDto>()
            }
        };

        var vm = new MemberPaymentStatusViewModel(
            null,
            service,
            _ => { },
            () => { });

        await vm.InitializeAsync();

        Assert.Equal("1 件", vm.SummaryText);
        Assert.Equal("0 円", vm.RemainingTotalText);
        Assert.Equal("0 件", vm.OverdueCountText);
        Assert.False(vm.IsOverdueBannerVisible);
        Assert.Equal(Visibility.Collapsed, vm.OverdueBannerVisibility);
    }

    [Fact]
    public void BackToDashboardCommand_ShouldCallOpenDashboard()
    {
        var service = new FakeInvoiceService();
        var called = false;

        var vm = new MemberPaymentStatusViewModel(
            null,
            service,
            _ => { },
            () => called = true);

        vm.BackToDashboardCommand.Execute(null);

        Assert.True(called);
    }

    [Fact]
    public void OpenDetailCommand_ShouldCallOpenDetailWithRowId()
    {
        var service = new FakeInvoiceService();
        long? openedId = null;

        var vm = new MemberPaymentStatusViewModel(
            null,
            service,
            id => openedId = id,
            () => { });

        var row = new MemberPaymentStatusRowViewModel
        {
            Id = 123,
            InvoiceNumber = "INV-123"
        };

        vm.OpenDetailCommand.Execute(row);

        Assert.Equal(123, openedId);
    }
}