using InvoiceAvaloniaClient.Models;

namespace InvoiceAvaloniaClient.Services;

public interface IPaymentService
{
    Task<PaymentListResultDto> SearchAsync(PaymentSearchRequest request);
    Task<PaymentDetailDto> GetByIdAsync(long paymentId);
    Task<List<MemberOptionDto>> GetMemberOptionsAsync();
    Task<long> CreateAsync(CreatePaymentRequestDto request);
    Task SaveAllocationsAsync(long paymentId, IEnumerable<PaymentAllocationLineDto> lines);

}