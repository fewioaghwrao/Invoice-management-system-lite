using InvoiceSystem.Application.Common.Interfaces;
using InvoiceSystem.Application.Dtos.Payments;
using InvoiceSystem.Application.Queries.Payments;


namespace InvoiceSystem.Application.Services;

public interface IPaymentService
{
    Task SaveAllocationsAsync(long paymentId, IReadOnlyList<SaveAllocationLine> lines, AuditActor actor);
    Task<long> AddAllocationAsync(long paymentId, long invoiceId, decimal amount, AuditActor actor);
    Task DeleteAllocationAsync(long paymentId, long allocationId, AuditActor actor);

    Task<PaymentListResultDto> SearchAsync(PaymentSearchQuery query);

    Task<PaymentDetailDto?> GetByIdAsync(long id);

    Task<long> CreateAsync(CreatePaymentRequestDto request);

    Task<long> AddAllocationAsync(long paymentId, long invoiceId, decimal amount);
    Task DeleteAllocationAsync(long paymentId, long allocationId);

    // 割当を「置き換え」で保存
    Task SaveAllocationsAsync(long paymentId, IReadOnlyList<SaveAllocationLine> lines);
}

public record SaveAllocationLine(long InvoiceId, decimal Amount);
