using InvoiceSystem.Wpf.Models;

namespace InvoiceSystem.Wpf.Services;

public interface IPaymentService
{
    Task<PaymentListResultDto> SearchAsync(PaymentSearchRequest request);
    Task<PaymentDetailDto> GetByIdAsync(long paymentId);
    Task<List<MemberOptionDto>> GetMemberOptionsAsync();
    Task<long> CreateAsync(CreatePaymentRequestDto request);
    Task SaveAllocationsAsync(long paymentId, IEnumerable<PaymentAllocationLineDto> lines);

}