using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Tests.Fakes;
using InvoiceSystem.Wpf.ViewModels;
using Xunit;

namespace InvoiceSystem.Wpf.Tests.ViewModels;

public class PaymentDetailViewModelTests
{
    [Fact]
    public async Task LoadAsync_ShouldLoadPaymentDetail()
    {
        var paymentService = new FakePaymentService
        {
            PaymentDetailToReturn = new PaymentDetailDto
            {
                Id = 10,
                PaymentDate = new DateTime(2026, 4, 25),
                PayerName = "山田 太郎",
                Method = "BANK_TRANSFER",
                Amount = 10000m,
                AllocatedAmount = 3000m,
                UnallocatedAmount = 7000m,
                Status = "PARTIAL",
                Allocations = new List<PaymentAllocationDto>
                {
                    new()
                    {
                        InvoiceId = 101,
                        InvoiceNumber = "INV-101",
                        Amount = 3000m
                    }
                }
            }
        };

        var invoiceService = new FakeInvoiceService();

        var vm = new PaymentDetailViewModel(
            paymentId: 10,
            paymentService,
            invoiceService);

        await vm.LoadAsync();

        Assert.Equal(10, paymentService.LastGetByIdPaymentId);
        Assert.Equal(10, vm.Id);
        Assert.Equal("PAY-010", vm.PaymentIdText);
        Assert.Equal("2026/04/25", vm.PaymentDateText);
        Assert.Equal("山田 太郎", vm.PayerName);
        Assert.Equal("BANK_TRANSFER", vm.Method);
        Assert.Equal(10000m, vm.Amount);
        Assert.Equal(3000m, vm.AllocatedAmount);
        Assert.Equal(7000m, vm.UnallocatedAmount);
        Assert.Equal("一部割当", vm.StatusText);

        Assert.Single(vm.AllocationRows);
        Assert.Equal(101, vm.AllocationRows[0].InvoiceId);
        Assert.Equal("INV-101", vm.AllocationRows[0].InvoiceNumber);
        Assert.Equal("3000", vm.AllocationRows[0].AmountText);

        Assert.False(vm.IsLoading);
        Assert.False(vm.HasError);
        Assert.Equal(string.Empty, vm.ErrorMessage);
    }

    [Fact]
    public async Task LoadAsync_ShouldAddEmptyRow_WhenNoAllocations()
    {
        var paymentService = new FakePaymentService
        {
            PaymentDetailToReturn = new PaymentDetailDto
            {
                Id = 20,
                PaymentDate = new DateTime(2026, 4, 25),
                PayerName = "佐藤 花子",
                Method = "CASH",
                Amount = 5000m,
                AllocatedAmount = 0m,
                UnallocatedAmount = 5000m,
                Status = "UNALLOCATED",
                Allocations = new List<PaymentAllocationDto>()
            }
        };

        var invoiceService = new FakeInvoiceService();

        var vm = new PaymentDetailViewModel(
            20,
            paymentService,
            invoiceService);

        await vm.LoadAsync();

        Assert.Single(vm.AllocationRows);
        Assert.Null(vm.AllocationRows[0].InvoiceId);
        Assert.Equal(string.Empty, vm.AllocationRows[0].InvoiceNumber);
        Assert.Equal(string.Empty, vm.AllocationRows[0].AmountText);
        Assert.Equal("未割当", vm.StatusText);
    }

    [Fact]
    public async Task LoadAsync_ShouldSetError_WhenServiceThrows()
    {
        var paymentService = new FakePaymentService
        {
            ExceptionToThrowOnGetById = new InvalidOperationException("load error")
        };

        var invoiceService = new FakeInvoiceService();

        var vm = new PaymentDetailViewModel(
            1,
            paymentService,
            invoiceService);

        await vm.LoadAsync();

        Assert.True(vm.HasError);
        Assert.Contains("load error", vm.ErrorMessage);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public void AddRow_ShouldAddAllocationRow()
    {
        var vm = new PaymentDetailViewModel(
            1,
            new FakePaymentService(),
            new FakeInvoiceService());

        vm.AddRow();

        Assert.Single(vm.AllocationRows);
    }

    [Fact]
    public void RemoveRow_ShouldRemoveRow()
    {
        var vm = new PaymentDetailViewModel(
            1,
            new FakePaymentService(),
            new FakeInvoiceService());

        var row1 = new PaymentAllocationEditRow();
        var row2 = new PaymentAllocationEditRow();

        vm.AllocationRows.Add(row1);
        vm.AllocationRows.Add(row2);

        vm.RemoveRow(row1);

        Assert.Single(vm.AllocationRows);
        Assert.Same(row2, vm.AllocationRows[0]);
    }

    [Fact]
    public void RemoveRow_ShouldKeepOneEmptyRow_WhenLastRowRemoved()
    {
        var vm = new PaymentDetailViewModel(
            1,
            new FakePaymentService(),
            new FakeInvoiceService());

        var row = new PaymentAllocationEditRow();

        vm.AllocationRows.Add(row);

        vm.RemoveRow(row);

        Assert.Single(vm.AllocationRows);
        Assert.Null(vm.AllocationRows[0].InvoiceId);
    }

    [Fact]
    public void ClearRowInvoice_ShouldClearInvoiceInfo()
    {
        var vm = new PaymentDetailViewModel(
            1,
            new FakePaymentService(),
            new FakeInvoiceService());

        var row = new PaymentAllocationEditRow
        {
            InvoiceId = 100,
            InvoiceNumber = "INV-100",
            MemberName = "山田 太郎",
            AmountText = "3000"
        };

        vm.ClearRowInvoice(row);

        Assert.Null(row.InvoiceId);
        Assert.Equal(string.Empty, row.InvoiceNumber);
        Assert.Equal(string.Empty, row.MemberName);
        Assert.Equal("3000", row.AmountText);
    }

    [Fact]
    public async Task SearchCandidatesAsync_ShouldLoadCandidateInvoices()
    {
        var paymentService = new FakePaymentService();

        var invoiceService = new FakeInvoiceService
        {
            SearchInvoicesResultToReturn = new List<InvoiceListItemDto>
            {
                new()
                {
                    Id = 101,
                    InvoiceNumber = "INV-101"
                },
                new()
                {
                    Id = 102,
                    InvoiceNumber = "INV-102"
                }
            }
        };

        var vm = new PaymentDetailViewModel(
            1,
            paymentService,
            invoiceService);

        vm.SearchInvoiceNumber = " INV ";
        vm.SearchMemberName = " 山田 ";

        await vm.SearchCandidatesAsync();

        Assert.Equal(2, vm.CandidateInvoices.Count);
        Assert.Equal("INV-101", vm.CandidateInvoices[0].InvoiceNumber);

        Assert.NotNull(invoiceService.LastSearchInvoicesRequest);
        Assert.Equal("INV", invoiceService.LastSearchInvoicesRequest!.InvoiceNumber);
        Assert.Equal("山田", invoiceService.LastSearchInvoicesRequest.MemberName);
        Assert.Equal(1, invoiceService.LastSearchInvoicesRequest.Page);
        Assert.Equal(50, invoiceService.LastSearchInvoicesRequest.PageSize);
    }

    [Fact]
    public async Task SearchCandidatesAsync_ShouldSetError_WhenServiceThrows()
    {
        var paymentService = new FakePaymentService();

        var invoiceService = new FakeInvoiceService
        {
            ExceptionToThrowOnSearchInvoices = new InvalidOperationException("search error")
        };

        var vm = new PaymentDetailViewModel(
            1,
            paymentService,
            invoiceService);

        await vm.SearchCandidatesAsync();

        Assert.True(vm.HasError);
        Assert.Contains("search error", vm.ErrorMessage);
        Assert.Empty(vm.CandidateInvoices);
    }

    [Fact]
    public void ApplyCandidateToSelectedRow_ShouldApplyInvoice()
    {
        var vm = new PaymentDetailViewModel(
            1,
            new FakePaymentService(),
            new FakeInvoiceService());

        var row = new PaymentAllocationEditRow();

        var candidate = new InvoiceListItemDto
        {
            Id = 101,
            InvoiceNumber = "INV-101"
        };

        vm.SelectedAllocationRow = row;
        vm.SelectedCandidate = candidate;

        vm.ApplyCandidateToSelectedRow();

        Assert.Equal(101, row.InvoiceId);
        Assert.Equal("INV-101", row.InvoiceNumber);
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenNoUsedRows()
    {
        var vm = new PaymentDetailViewModel(
            1,
            new FakePaymentService(),
            new FakeInvoiceService());

        vm.Amount = 10000m;
        vm.AllocationRows.Add(new PaymentAllocationEditRow());

        var error = vm.Validate();

        Assert.NotNull(error);
        Assert.Contains("割当行を1件以上入力してください。", error);
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenInvoiceIsNotSelected()
    {
        var vm = new PaymentDetailViewModel(
            1,
            new FakePaymentService(),
            new FakeInvoiceService());

        vm.Amount = 10000m;
        vm.AllocationRows.Add(new PaymentAllocationEditRow
        {
            AmountText = "1000"
        });

        var error = vm.Validate();

        Assert.NotNull(error);
        Assert.Contains("割当行1: 請求書を選択してください。", error);
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenAmountIsInvalid()
    {
        var vm = new PaymentDetailViewModel(
            1,
            new FakePaymentService(),
            new FakeInvoiceService());

        vm.Amount = 10000m;
        vm.AllocationRows.Add(new PaymentAllocationEditRow
        {
            InvoiceId = 101,
            InvoiceNumber = "INV-101",
            AmountText = "abc"
        });

        var error = vm.Validate();

        Assert.NotNull(error);
        Assert.Contains("割当行1: 金額は1以上の数値で入力してください。", error);
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenDuplicateInvoiceExists()
    {
        var vm = new PaymentDetailViewModel(
            1,
            new FakePaymentService(),
            new FakeInvoiceService());

        vm.Amount = 10000m;
        vm.AllocationRows.Add(new PaymentAllocationEditRow
        {
            InvoiceId = 101,
            InvoiceNumber = "INV-101",
            AmountText = "1000"
        });
        vm.AllocationRows.Add(new PaymentAllocationEditRow
        {
            InvoiceId = 101,
            InvoiceNumber = "INV-101",
            AmountText = "2000"
        });

        var error = vm.Validate();

        Assert.NotNull(error);
        Assert.Contains("同じ請求書が複数行で選択されています。", error);
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenInputAllocatedSumExceedsAmount()
    {
        var vm = new PaymentDetailViewModel(
            1,
            new FakePaymentService(),
            new FakeInvoiceService());

        vm.Amount = 1000m;
        vm.AllocationRows.Add(new PaymentAllocationEditRow
        {
            InvoiceId = 101,
            InvoiceNumber = "INV-101",
            AmountText = "2000"
        });

        var error = vm.Validate();

        Assert.NotNull(error);
        Assert.Contains("割当合計が入金額を超えています。", error);
    }

    [Fact]
    public void Validate_ShouldReturnNull_WhenValid()
    {
        var vm = new PaymentDetailViewModel(
            1,
            new FakePaymentService(),
            new FakeInvoiceService());

        vm.Amount = 10000m;
        vm.AllocationRows.Add(new PaymentAllocationEditRow
        {
            InvoiceId = 101,
            InvoiceNumber = "INV-101",
            AmountText = "3000"
        });

        var error = vm.Validate();

        Assert.Null(error);
    }

    [Fact]
    public async Task SaveAsync_ShouldSaveAllocations_WhenValid()
    {
        var paymentService = new FakePaymentService
        {
            PaymentDetailToReturn = new PaymentDetailDto
            {
                Id = 1,
                PaymentDate = new DateTime(2026, 4, 25),
                PayerName = "山田 太郎",
                Method = "BANK_TRANSFER",
                Amount = 10000m,
                AllocatedAmount = 3000m,
                UnallocatedAmount = 7000m,
                Status = "PARTIAL",
                Allocations = new List<PaymentAllocationDto>()
            }
        };

        var invoiceService = new FakeInvoiceService();

        var vm = new PaymentDetailViewModel(
            1,
            paymentService,
            invoiceService);

        vm.Amount = 10000m;
        vm.AllocationRows.Add(new PaymentAllocationEditRow
        {
            InvoiceId = 101,
            InvoiceNumber = "INV-101",
            AmountText = "3000"
        });

        await vm.SaveAsync();

        Assert.Equal(1, paymentService.LastSaveAllocationsPaymentId);
        Assert.Single(paymentService.LastSavedLines);
        Assert.Equal(101, paymentService.LastSavedLines[0].InvoiceId);
        Assert.Equal(3000m, paymentService.LastSavedLines[0].Amount);

        Assert.False(vm.IsSaving);
        Assert.False(vm.HasError);
    }

    [Fact]
    public async Task SaveAsync_ShouldThrow_WhenInvalid()
    {
        var paymentService = new FakePaymentService();
        var invoiceService = new FakeInvoiceService();

        var vm = new PaymentDetailViewModel(
            1,
            paymentService,
            invoiceService);

        vm.Amount = 10000m;
        vm.AllocationRows.Add(new PaymentAllocationEditRow());

        var ex = await Assert.ThrowsAsync<Exception>(() => vm.SaveAsync());

        Assert.Contains("割当行を1件以上入力してください。", ex.Message);
        Assert.Empty(paymentService.LastSavedLines);
    }

    [Fact]
    public async Task SaveAsync_ShouldSetError_WhenServiceThrows()
    {
        var paymentService = new FakePaymentService
        {
            ExceptionToThrowOnSaveAllocations = new InvalidOperationException("save error")
        };

        var invoiceService = new FakeInvoiceService();

        var vm = new PaymentDetailViewModel(
            1,
            paymentService,
            invoiceService);

        vm.Amount = 10000m;
        vm.AllocationRows.Add(new PaymentAllocationEditRow
        {
            InvoiceId = 101,
            InvoiceNumber = "INV-101",
            AmountText = "3000"
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => vm.SaveAsync());

        Assert.Equal("save error", ex.Message);
        Assert.True(vm.HasError);
        Assert.Contains("save error", vm.ErrorMessage);
        Assert.False(vm.IsSaving);
    }
}