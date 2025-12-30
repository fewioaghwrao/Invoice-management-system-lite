using InvoiceSystem.Application.Common.Interfaces;
using InvoiceSystem.Application.Dtos.Payments;
using InvoiceSystem.Application.Queries.Payments;
using InvoiceSystem.Application.Services;
using InvoiceSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _db;

    private readonly IAuditLogger _audit;

    public PaymentService(AppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    private static DateTime EnsureUtc(DateTime dt)
    {
        return dt.Kind switch
        {
            DateTimeKind.Utc => dt,
            DateTimeKind.Local => dt.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
            _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc)
        };
    }


    public async Task<PaymentListResultDto> SearchAsync(PaymentSearchQuery query)
    {
        // ✅ ここがポイント：最初から IQueryable<Payment> で受ける
        IQueryable<Domain.Entities.Payment> q = _db.Payments
            .AsNoTracking()
            .Include(p => p.PaymentAllocations)
                .ThenInclude(pa => pa.Invoice);

        // 年/月
        q = q.Where(p => p.PaymentDate.Year == query.Year);

        if (query.Month.HasValue)
            q = q.Where(p => p.PaymentDate.Month == query.Month.Value);

        // keyword（入金ID / 名義 / 請求書番号）
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();

            q = q.Where(p =>
                p.Id.ToString().Contains(kw) ||
                (p.PayerName != null && p.PayerName.Contains(kw)) ||
                p.PaymentAllocations.Any(a => a.Invoice.InvoiceNumber.Contains(kw))
            );
        }

        var all = await q
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.Id)
            .ToListAsync();

        // Status filter（UNALLOCATED / PARTIAL / ALLOCATED）
        IEnumerable<Domain.Entities.Payment> filtered = all;

        string NormalizeStatus(string s) => (s ?? "all").Trim().ToUpperInvariant();
        var status = NormalizeStatus(query.Status);

        filtered = status switch
        {
            "UNALLOCATED" => filtered.Where(p => Allocated(p) <= 0),
            "PARTIAL" => filtered.Where(p => Allocated(p) > 0 && Allocated(p) < p.Amount),
            "ALLOCATED" => filtered.Where(p => Allocated(p) >= p.Amount),
            _ => filtered
        };

        var filteredList = filtered.ToList();

        // summary
        var totalAmount = filteredList.Sum(p => p.Amount);
        var allocatedTotal = filteredList.Sum(p => Allocated(p));
        var unallocatedTotal = totalAmount - allocatedTotal;

        // paging
        var totalCount = filteredList.Count;
        var skip = (query.Page - 1) * query.PageSize;

        var pageRows = filteredList
            .Skip(skip)
            .Take(query.PageSize)
            .Select(p =>
            {
                var allocated = Allocated(p);
                var invoices = p.PaymentAllocations
                    .Select(a => a.Invoice)
                    .Where(i => i != null)
                    .Select(i => new InvoiceLinkDto
                    {
                        Id = i!.Id,
                        InvoiceNumber = i.InvoiceNumber
                    })
                    .DistinctBy(x => x.Id)
                    .OrderBy(x => x.InvoiceNumber)
                    .ToList();

                return new PaymentListItemDto
                {
                    Id = p.Id,
                    PaymentDate = p.PaymentDate,
                    PayerName = p.PayerName,
                    Amount = p.Amount,
                    AllocatedAmount = allocated,
                    Invoices = invoices,
                    Status = allocated <= 0 ? "UNALLOCATED"
                          : allocated >= p.Amount ? "ALLOCATED"
                          : "PARTIAL"
                };
            })
            .ToList();

        return new PaymentListResultDto
        {
            Year = query.Year,
            Month = query.Month?.ToString() ?? "all",
            Keyword = query.Keyword ?? "",
            Status = query.Status,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            Rows = pageRows,
            Summary = new PaymentListResultDto.SummaryDto
            {
                TotalAmount = totalAmount,
                AllocatedTotal = allocatedTotal,
                UnallocatedTotal = unallocatedTotal
            }
        };

        static decimal Allocated(Domain.Entities.Payment p)
            => p.PaymentAllocations?.Sum(a => a.Amount) ?? 0m;
    }

    public async Task<PaymentDetailDto?> GetByIdAsync(long id)
    {
        var payment = await _db.Payments
            .AsNoTracking()
            .Include(p => p.PaymentAllocations)
                .ThenInclude(a => a.Invoice)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment is null) return null;

        var allocated = payment.PaymentAllocations.Sum(a => a.Amount);
        var status = allocated <= 0 ? "UNALLOCATED"
                  : allocated >= payment.Amount ? "ALLOCATED"
                  : "PARTIAL";

        return new PaymentDetailDto
        {
            Id = payment.Id,
            PaymentDate = payment.PaymentDate,
            Amount = payment.Amount,
            PayerName = payment.PayerName,
            Method = payment.Method,
            AllocatedAmount = allocated,
            UnallocatedAmount = Math.Max(0, payment.Amount - allocated),
            Status = status,
            Allocations = payment.PaymentAllocations
                .OrderBy(a => a.Invoice.InvoiceNumber)
                .Select(a => new PaymentAllocationDto
                {
                    InvoiceId = a.InvoiceId,
                    InvoiceNumber = a.Invoice.InvoiceNumber,
                    Amount = a.Amount
                })
                .ToList()
        };
    }

    public async Task SaveAllocationsAsync(long paymentId, IReadOnlyList<SaveAllocationLine> lines)
    {
        // 支払い本体取得
        var payment = await _db.Payments
            .Include(p => p.PaymentAllocations)
            .FirstOrDefaultAsync(p => p.Id == paymentId)
            ?? throw new InvalidOperationException("Payment not found");

        // ★ 既存割当の invoiceIds（置き換え前）
        var beforeInvoiceIds = payment.PaymentAllocations
            .Select(a => a.InvoiceId)
            .Distinct()
            .ToList();

        // 入力バリデーション
        if (lines.Count == 0)
        {
            // 全削除扱い（割当クリア）
            _db.PaymentAllocations.RemoveRange(payment.PaymentAllocations);
            payment.UpdatedAt = DateTime.UtcNow;

            // ★ クリアした請求書を再計算
            foreach (var invId in beforeInvoiceIds)
                await RecalcInvoiceStatusAsync(invId);

            await _db.SaveChangesAsync();
            return;
        }

        if (lines.Any(x => x.Amount <= 0))
            throw new InvalidOperationException("Amount must be > 0");

        var sum = lines.Sum(x => x.Amount);
        if (sum > payment.Amount)
            throw new InvalidOperationException("Allocated sum exceeds payment amount");

        // 請求書存在チェック
        var invoiceIds = lines.Select(x => x.InvoiceId).Distinct().ToList();
        var existing = await _db.Invoices
            .Where(i => invoiceIds.Contains(i.Id))
            .Select(i => i.Id)
            .ToListAsync();

        if (existing.Count != invoiceIds.Count)
            throw new InvalidOperationException("Some invoices not found");

        // ✅ 置き換え：既存割当を消して、入力分を入れる（最小で事故が少ない）
        _db.PaymentAllocations.RemoveRange(payment.PaymentAllocations);

        foreach (var l in lines)
        {
            _db.PaymentAllocations.Add(new Domain.Entities.PaymentAllocation
            {
                PaymentId = payment.Id,
                InvoiceId = l.InvoiceId,
                Amount = l.Amount
            });
        }

        payment.UpdatedAt = DateTime.UtcNow;

        // ★ 変更対象の請求書（前後 union）
        var affected = beforeInvoiceIds
            .Concat(invoiceIds)
            .Distinct()
            .ToList();

        foreach (var invId in affected)
            await RecalcInvoiceStatusAsync(invId);

        await _db.SaveChangesAsync();
    }
    public async Task<long> CreateAsync(CreatePaymentRequestDto req)
    {
        if (req.MemberId <= 0) throw new InvalidOperationException("MemberId is required");
        if (req.PaymentDate == default) throw new InvalidOperationException("PaymentDate is required");
        if (req.Amount <= 0) throw new InvalidOperationException("Amount must be > 0");

        var memberExists = await _db.Members.AnyAsync(m => m.Id == req.MemberId);
        if (!memberExists) throw new InvalidOperationException("Member not found");

        var now = DateTime.UtcNow;

        var payment = new Domain.Entities.Payment
        {
            MemberId = req.MemberId,
            PaymentDate = EnsureUtc(req.PaymentDate),
            Amount = req.Amount,
            PayerName = string.IsNullOrWhiteSpace(req.PayerName) ? null : req.PayerName.Trim(),
            Method = string.IsNullOrWhiteSpace(req.Method) ? null : req.Method.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            // 手動登録なら ImportBatchId は null のままでOK
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        return payment.Id;
    }

    public async Task<long> AddAllocationAsync(long paymentId, long invoiceId, decimal amount)
    {
        if (paymentId <= 0) throw new InvalidOperationException("PaymentId is required");
        if (invoiceId <= 0) throw new InvalidOperationException("InvoiceId is required");
        if (amount <= 0) throw new InvalidOperationException("Amount must be > 0");

        // 支払い本体 + 既存割当取得（残額計算のため）
        var payment = await _db.Payments
            .Include(p => p.PaymentAllocations)
            .FirstOrDefaultAsync(p => p.Id == paymentId)
            ?? throw new InvalidOperationException("Payment not found");

        // 請求書存在確認
        var invoiceExists = await _db.Invoices.AnyAsync(i => i.Id == invoiceId);
        if (!invoiceExists) throw new InvalidOperationException("Invoice not found");

        // 重複割当の禁止（ユニーク制約があるなら尚よし）
        if (payment.PaymentAllocations.Any(a => a.InvoiceId == invoiceId))
            throw new InvalidOperationException("This invoice is already allocated for this payment");

        var allocated = payment.PaymentAllocations.Sum(a => a.Amount);
        var remaining = payment.Amount - allocated;

        if (amount > remaining)
            throw new InvalidOperationException($"Allocation exceeds remaining amount. Remaining={remaining}");

        var alloc = new Domain.Entities.PaymentAllocation
        {
            PaymentId = paymentId,
            InvoiceId = invoiceId,
            Amount = amount
        };

        _db.PaymentAllocations.Add(alloc);

        payment.UpdatedAt = DateTime.UtcNow;

        try
        {
            // ★ ここで請求書ステータスを更新
            await RecalcInvoiceStatusAsync(invoiceId);

            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // (PaymentId, InvoiceId) のユニーク制約に当たった等の可能性
            throw new InvalidOperationException("Failed to add allocation. It may already exist.", ex);
        }

        return alloc.Id;
    }

    public async Task DeleteAllocationAsync(long paymentId, long allocationId)
    {
        if (paymentId <= 0) throw new InvalidOperationException("PaymentId is required");
        if (allocationId <= 0) throw new InvalidOperationException("AllocationId is required");

        var alloc = await _db.PaymentAllocations
            .FirstOrDefaultAsync(a => a.Id == allocationId && a.PaymentId == paymentId)
            ?? throw new InvalidOperationException("Allocation not found");

        var invoiceId = alloc.InvoiceId;

        _db.PaymentAllocations.Remove(alloc);

        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId);
        if (payment is not null) payment.UpdatedAt = DateTime.UtcNow;

        await RecalcInvoiceStatusAsync(invoiceId);

        await _db.SaveChangesAsync();
    }


    public async Task SaveAllocationsAsync(long paymentId, IReadOnlyList<SaveAllocationLine> lines, AuditActor actor)
    {
        var payment = await _db.Payments
            .Include(p => p.PaymentAllocations)
            .FirstOrDefaultAsync(p => p.Id == paymentId)
            ?? throw new InvalidOperationException("Payment not found");

        var before = payment.PaymentAllocations
            .Select(a => new { a.Id, a.InvoiceId, a.Amount })
            .OrderBy(x => x.InvoiceId)
            .ToList();

        // 入力バリデーション（あなたの既存）
        if (lines.Count == 0)
        {
            _db.PaymentAllocations.RemoveRange(payment.PaymentAllocations);
            payment.UpdatedAt = DateTime.UtcNow;

            await _audit.WriteAsync(
                action: "PAYMENT_ALLOCATIONS_CLEARED",
                entity: "Payment",
                entityId: paymentId.ToString(),
                summary: $"PaymentId={paymentId} allocations cleared.",
                data: new { paymentId, before },
                actor: actor
            );

            var beforeInvoiceIds = before.Select(x => x.InvoiceId).Distinct().ToList();
            foreach (var invId in beforeInvoiceIds)
                await RecalcInvoiceStatusAsync(invId);

            await _db.SaveChangesAsync();
            return;
        }

        if (lines.Any(x => x.Amount <= 0))
            throw new InvalidOperationException("Amount must be > 0");

        var sum = lines.Sum(x => x.Amount);
        if (sum > payment.Amount)
            throw new InvalidOperationException("Allocated sum exceeds payment amount");

        var invoiceIds = lines.Select(x => x.InvoiceId).Distinct().ToList();
        var existing = await _db.Invoices
            .Where(i => invoiceIds.Contains(i.Id))
            .Select(i => i.Id)
            .ToListAsync();

        if (existing.Count != invoiceIds.Count)
            throw new InvalidOperationException("Some invoices not found");

        _db.PaymentAllocations.RemoveRange(payment.PaymentAllocations);

        foreach (var l in lines)
        {
            _db.PaymentAllocations.Add(new Domain.Entities.PaymentAllocation
            {
                PaymentId = payment.Id,
                InvoiceId = l.InvoiceId,
                Amount = l.Amount
            });
        }

        payment.UpdatedAt = DateTime.UtcNow;

        await _audit.WriteAsync(
            action: "PAYMENT_ALLOCATIONS_REPLACED",
            entity: "Payment",
            entityId: paymentId.ToString(),
            summary: $"PaymentId={paymentId} allocations replaced.",
            data: new
            {
                paymentId,
                before,
                after = lines.Select(x => new { x.InvoiceId, x.Amount }).OrderBy(x => x.InvoiceId).ToList()
            },
            actor: actor
        );

        var affected = before.Select(x => x.InvoiceId)
            .Concat(invoiceIds)
            .Distinct()
            .ToList();

        foreach (var invId in affected)
            await RecalcInvoiceStatusAsync(invId);

        await _db.SaveChangesAsync();
    }

    public async Task<long> AddAllocationAsync(long paymentId, long invoiceId, decimal amount, AuditActor actor)
    {
        if (paymentId <= 0) throw new InvalidOperationException("PaymentId is required");
        if (invoiceId <= 0) throw new InvalidOperationException("InvoiceId is required");
        if (amount <= 0) throw new InvalidOperationException("Amount must be > 0");

        var payment = await _db.Payments
            .Include(p => p.PaymentAllocations)
            .FirstOrDefaultAsync(p => p.Id == paymentId)
            ?? throw new InvalidOperationException("Payment not found");

        var invoiceExists = await _db.Invoices.AnyAsync(i => i.Id == invoiceId);
        if (!invoiceExists) throw new InvalidOperationException("Invoice not found");

        if (payment.PaymentAllocations.Any(a => a.InvoiceId == invoiceId))
            throw new InvalidOperationException("This invoice is already allocated for this payment");

        var allocated = payment.PaymentAllocations.Sum(a => a.Amount);
        var remaining = payment.Amount - allocated;
        if (amount > remaining)
            throw new InvalidOperationException($"Allocation exceeds remaining amount. Remaining={remaining}");

        var alloc = new Domain.Entities.PaymentAllocation
        {
            PaymentId = paymentId,
            InvoiceId = invoiceId,
            Amount = amount
        };

        _db.PaymentAllocations.Add(alloc);
        payment.UpdatedAt = DateTime.UtcNow;

        await _audit.WriteAsync(
            action: "PAYMENT_ALLOCATION_ADDED",
            entity: "PaymentAllocation",
            entityId: "(new)",
            summary: $"PaymentId={paymentId} -> InvoiceId={invoiceId} Amount={amount}",
            data: new { paymentId, invoiceId, amount },
            actor: actor
        );

        await RecalcInvoiceStatusAsync(invoiceId);
        await _db.SaveChangesAsync();

        return alloc.Id;
    }

    public async Task DeleteAllocationAsync(long paymentId, long allocationId, AuditActor actor)
    {
        if (paymentId <= 0) throw new InvalidOperationException("PaymentId is required");
        if (allocationId <= 0) throw new InvalidOperationException("AllocationId is required");

        var alloc = await _db.PaymentAllocations
            .FirstOrDefaultAsync(a => a.Id == allocationId && a.PaymentId == paymentId)
            ?? throw new InvalidOperationException("Allocation not found");

        var invoiceId = alloc.InvoiceId;
        var amount = alloc.Amount;

        _db.PaymentAllocations.Remove(alloc);

        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId);
        if (payment is not null) payment.UpdatedAt = DateTime.UtcNow;

        await _audit.WriteAsync(
            action: "PAYMENT_ALLOCATION_DELETED",
            entity: "PaymentAllocation",
            entityId: allocationId.ToString(),
            summary: $"AllocationId={allocationId} deleted. PaymentId={paymentId} InvoiceId={invoiceId} Amount={amount}",
            data: new { allocationId, paymentId, invoiceId, amount },
            actor: actor
        );

        await RecalcInvoiceStatusAsync(invoiceId);
        await _db.SaveChangesAsync();
    }

    private async Task RecalcInvoiceStatusAsync(long invoiceId)
    {
        // ① Invoice を取得（ステータス更新するので tracking で）
        var invoice = await _db.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice is null) return;

        // キャンセルは変更しない（業務的に安全）
        // ※あなたの Seed: CANCELLED = 5
        if (invoice.StatusId == 5) return;

        // ② この請求書に割り当てられている入金合計
        var paid = await _db.PaymentAllocations
            .Where(a => a.InvoiceId == invoiceId)
            .SumAsync(a => (decimal?)a.Amount) ?? 0m;

        // ③ 判定
        long newStatusId;

        // 完全入金（超過入金は PAID 扱い）
        if (paid >= invoice.TotalAmount)
        {
            newStatusId = 3; // PAID
        }
        else if (paid > 0)
        {
            newStatusId = 2; // PARTIAL
        }
        else
        {
            // 期限超過を使うならここで OVERDUE を優先
            // ※不要なら常に UNPAID にしてOK
            var today = DateTime.UtcNow.Date;
            newStatusId = (invoice.DueDate.Date < today) ? 4 : 1; // OVERDUE or UNPAID
        }

        if (invoice.StatusId != newStatusId)
        {
            invoice.StatusId = newStatusId;
            invoice.UpdatedAt = DateTime.UtcNow;
        }
    }


}



