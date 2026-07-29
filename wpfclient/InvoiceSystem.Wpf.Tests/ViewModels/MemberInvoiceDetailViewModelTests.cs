using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Tests.Fakes;
using InvoiceSystem.Wpf.ViewModels;
using System;
using System.Windows;
using Xunit;

namespace InvoiceSystem.Wpf.Tests.ViewModels;

public sealed class MemberInvoiceDetailViewModelTests
{
    [StaFact]
    public async Task InitializeAsync_WhenDetailExists_SetsProperties()
    {
        var service = new FakeInvoiceService
        {
            MemberInvoiceDetailToReturn = new InvoiceDetailDto
            {
                Id = 100,
                InvoiceNumber = "INV-2026-001",
                StatusName = "未入金",
                InvoiceDate = new DateTime(2026, 4, 1),
                DueDate = new DateTime(2026, 4, 30),
                TotalAmount = 120000,
                PaidAmount = 20000,
                RemainingAmount = 100000,
                Remarks = "テスト備考"
            }
        };

        var vm = new MemberInvoiceDetailViewModel(
            invoiceId: 100,
            invoiceService: service,
            openInvoiceList: () => { });

        await vm.InitializeAsync();

        Assert.Equal(100, service.LastInvoiceIdForMemberDetail);
        Assert.Equal("INV-2026-001", vm.InvoiceNumber);
        Assert.Equal("未入金", vm.StatusName);
        Assert.Equal("2026/04/01", vm.InvoiceDateText);
        Assert.Equal("2026/04/30", vm.DueDateText);
        Assert.Equal("120,000 円", vm.TotalAmountText);
        Assert.Equal("20,000 円", vm.PaidAmountText);
        Assert.Equal("100,000 円", vm.RemainingAmountText);
        Assert.Equal("テスト備考", vm.Remarks);
        Assert.True(vm.IsPdfEnabled);
        Assert.False(vm.IsLoading);
        Assert.Equal(Visibility.Collapsed, vm.LoadingVisibility);
    }

    [StaFact]
    public async Task InitializeAsync_WhenRemarksIsEmpty_SetsDefaultRemarks()
    {
        var service = new FakeInvoiceService
        {
            MemberInvoiceDetailToReturn = new InvoiceDetailDto
            {
                InvoiceNumber = "INV-001",
                StatusName = "入金済み",
                InvoiceDate = new DateTime(2026, 4, 1),
                DueDate = new DateTime(2026, 4, 30),
                TotalAmount = 50000,
                PaidAmount = 50000,
                RemainingAmount = 0,
                Remarks = ""
            }
        };

        var vm = new MemberInvoiceDetailViewModel(1, service, () => { });

        await vm.InitializeAsync();

        Assert.Equal("備考はありません。", vm.Remarks);
    }

    [StaFact]
    public async Task InitializeAsync_WhenDueDatePastAndRemainingAmountExists_IsOverdueTrue()
    {
        var service = new FakeInvoiceService
        {
            MemberInvoiceDetailToReturn = new InvoiceDetailDto
            {
                InvoiceNumber = "INV-OVERDUE",
                StatusName = "未入金",
                InvoiceDate = DateTime.Today.AddDays(-40),
                DueDate = DateTime.Today.AddDays(-10),
                TotalAmount = 100000,
                PaidAmount = 0,
                RemainingAmount = 100000
            }
        };

        var vm = new MemberInvoiceDetailViewModel(1, service, () => { });

        await vm.InitializeAsync();

        Assert.True(vm.IsOverdue);
        Assert.Equal(Visibility.Visible, vm.OverdueVisibility);
    }

    [StaFact]
    public async Task InitializeAsync_WhenRemainingAmountIsZero_IsOverdueFalse()
    {
        var service = new FakeInvoiceService
        {
            MemberInvoiceDetailToReturn = new InvoiceDetailDto
            {
                InvoiceNumber = "INV-PAID",
                StatusName = "入金済み",
                InvoiceDate = DateTime.Today.AddDays(-40),
                DueDate = DateTime.Today.AddDays(-10),
                TotalAmount = 100000,
                PaidAmount = 100000,
                RemainingAmount = 0
            }
        };

        var vm = new MemberInvoiceDetailViewModel(1, service, () => { });

        await vm.InitializeAsync();

        Assert.False(vm.IsOverdue);
        Assert.Equal(Visibility.Collapsed, vm.OverdueVisibility);
    }

    [StaFact]
    public async Task InitializeAsync_WhenServiceThrows_SetsErrorMessage()
    {
        var service = new FakeInvoiceService
        {
            ExceptionToThrowOnGetMemberInvoiceDetail = new InvalidOperationException("API error")
        };

        var vm = new MemberInvoiceDetailViewModel(1, service, () => { });

        await vm.InitializeAsync();

        Assert.Equal("API error", vm.ErrorMessage);
        Assert.False(vm.IsPdfEnabled);
        Assert.False(vm.IsLoading);
    }

    [StaFact]
    public void BackCommand_WhenExecuted_CallsOpenInvoiceList()
    {
        var called = false;

        var vm = new MemberInvoiceDetailViewModel(
            invoiceId: 1,
            invoiceService: new FakeInvoiceService(),
            openInvoiceList: () => called = true);

        vm.BackCommand.Execute(null);

        Assert.True(called);
    }

    [StaFact]
    public async Task OpenPdfCommand_AfterInitialize_CanExecuteTrue()
    {
        var service = new FakeInvoiceService
        {
            MemberInvoiceDetailToReturn = new InvoiceDetailDto
            {
                InvoiceNumber = "INV-001",
                StatusName = "未入金",
                InvoiceDate = new DateTime(2026, 4, 1),
                DueDate = new DateTime(2026, 4, 30),
                TotalAmount = 10000,
                PaidAmount = 0,
                RemainingAmount = 10000
            }
        };

        var vm = new MemberInvoiceDetailViewModel(1, service, () => { });

        await vm.InitializeAsync();

        Assert.True(vm.OpenPdfCommand.CanExecute(null));
    }
}