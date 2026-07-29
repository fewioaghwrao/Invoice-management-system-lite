using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Services;

namespace InvoiceSystem.Wpf.Tests.Fakes;

public sealed class FakePaymentService : IPaymentService
{
    // =========================
    // PaymentCreateViewModel 用
    // =========================
    public List<MemberOptionDto> MemberOptionsToReturn { get; set; } = new();
    public Exception? ExceptionToThrowOnGetMemberOptions { get; set; }

    public long CreateResultToReturn { get; set; } = 1;
    public Exception? ExceptionToThrowOnCreate { get; set; }

    public int GetMemberOptionsCallCount { get; private set; }
    public int CreateCallCount { get; private set; }

    public CreatePaymentRequestDto? LastCreateRequest { get; private set; }

    public Task<List<MemberOptionDto>> GetMemberOptionsAsync()
    {
        GetMemberOptionsCallCount++;

        if (ExceptionToThrowOnGetMemberOptions != null)
        {
            return Task.FromException<List<MemberOptionDto>>(
                ExceptionToThrowOnGetMemberOptions);
        }

        return Task.FromResult(MemberOptionsToReturn);
    }

    public Task<long> CreateAsync(CreatePaymentRequestDto request)
    {
        CreateCallCount++;
        LastCreateRequest = request;

        if (ExceptionToThrowOnCreate != null)
        {
            return Task.FromException<long>(ExceptionToThrowOnCreate);
        }

        return Task.FromResult(CreateResultToReturn);
    }

    // =========================
    // PaymentDetailViewModel 用
    // =========================
    public PaymentDetailDto? PaymentDetailToReturn { get; set; }
    public Exception? ExceptionToThrowOnGetById { get; set; }
    public Exception? ExceptionToThrowOnSaveAllocations { get; set; }

    public int GetByIdCallCount { get; private set; }
    public int SaveAllocationsCallCount { get; private set; }

    public long LastGetByIdPaymentId { get; private set; }
    public long LastSaveAllocationsPaymentId { get; private set; }

    public List<PaymentAllocationLineDto> LastSavedLines { get; private set; } = new();

    public Task<PaymentDetailDto> GetByIdAsync(long paymentId)
    {
        GetByIdCallCount++;
        LastGetByIdPaymentId = paymentId;

        if (ExceptionToThrowOnGetById != null)
        {
            return Task.FromException<PaymentDetailDto>(
                ExceptionToThrowOnGetById);
        }

        return Task.FromResult(PaymentDetailToReturn ?? new PaymentDetailDto
        {
            Id = paymentId,
            PaymentDate = new DateTime(2026, 4, 25),
            PayerName = "テスト入金者",
            Method = "BANK_TRANSFER",
            Amount = 10000m,
            AllocatedAmount = 0m,
            UnallocatedAmount = 10000m,
            Status = "UNALLOCATED",
            Allocations = new List<PaymentAllocationDto>()
        });
    }

    public Task SaveAllocationsAsync(
        long paymentId,
        IEnumerable<PaymentAllocationLineDto> lines)
    {
        SaveAllocationsCallCount++;
        LastSaveAllocationsPaymentId = paymentId;
        LastSavedLines = lines?.ToList() ?? new List<PaymentAllocationLineDto>();

        if (ExceptionToThrowOnSaveAllocations != null)
        {
            return Task.FromException(ExceptionToThrowOnSaveAllocations);
        }

        return Task.CompletedTask;
    }

    // =========================
    // PaymentListViewModel 用
    // 今後一覧テストでも使えるように用意
    // =========================
    public PaymentListResultDto? SearchResultToReturn { get; set; }
    public Exception? ExceptionToThrowOnSearch { get; set; }

    public int SearchCallCount { get; private set; }
    public PaymentSearchRequest? LastSearchRequest { get; private set; }

    public Task<PaymentListResultDto> SearchAsync(PaymentSearchRequest request)
    {
        SearchCallCount++;
        LastSearchRequest = request;

        if (ExceptionToThrowOnSearch != null)
        {
            return Task.FromException<PaymentListResultDto>(
                ExceptionToThrowOnSearch);
        }

        return Task.FromResult(SearchResultToReturn ?? new PaymentListResultDto
        {
            Rows = new List<PaymentListItemDto>(),
            Summary = new PaymentSummaryDto(),
            Month = "all",
            Keyword = string.Empty,
            Status = "all"
        });
    }
}