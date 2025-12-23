using InvoiceSystem.Application.Common.Interfaces;
using InvoiceSystem.Application.Dtos.Admin;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Infrastructure.Services;

public class AdminSummaryService : IAdminSummaryService
{
    private readonly AppDbContext _db;

    public AdminSummaryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AdminSummaryDto> GetSummaryAsync(int year, CancellationToken ct = default)
    {
        var start = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddYears(1);

        // 対象：請求日が year の請求書（売上＝請求金額）
        var invoicesInYear = _db.Invoices
            .AsNoTracking()
            .Where(i => i.InvoiceDate >= start && i.InvoiceDate < end);

        // paid：割当（PaymentAllocation）の合計（対象invoiceに紐づく割当全て）
        // ※「入金日が年内だけ」等にしたい場合は PaymentDate で絞る（後述）
        var allocByInvoice = _db.PaymentAllocations
            .AsNoTracking()
            .GroupBy(a => a.InvoiceId)
            .Select(g => new
            {
                InvoiceId = g.Key,
                PaidTotal = g.Sum(x => x.Amount)
            });

        var invoiceAgg = await invoicesInYear
            .Select(i => new
            {
                i.Id,
                i.InvoiceNumber,
                ClientName = i.Member.Name, // MemberName相当
                i.InvoiceDate,
                i.DueDate,
                InvoiceTotal = i.TotalAmount
            })
            .GroupJoin(
                allocByInvoice,
                i => i.Id,
                a => a.InvoiceId,
                (i, a) => new
                {
                    i.Id,
                    i.InvoiceNumber,
                    i.ClientName,
                    i.InvoiceDate,
                    i.DueDate,
                    i.InvoiceTotal,
                    PaidTotal = a.Select(x => x.PaidTotal).FirstOrDefault()
                }
            )
            .ToListAsync(ct);

        var invoiceTotal = invoiceAgg.Sum(x => x.InvoiceTotal);
        var paidTotal = invoiceAgg.Sum(x => x.PaidTotal);
        var remainingTotal = invoiceAgg.Sum(x => Math.Max(0m, x.InvoiceTotal - x.PaidTotal));

        var recoveryRate = invoiceTotal == 0m ? 0m : (paidTotal / invoiceTotal) * 100m;

        var invoiceCount = invoiceAgg.Count;

        // paymentCount：年内入金の件数（入金数カード用）
        var paymentCount = await _db.Payments
            .AsNoTracking()
            .CountAsync(p => p.PaymentDate >= start && p.PaymentDate < end, ct);

        // 月別売上（12ヶ月）
        var monthlySalesRaw = invoiceAgg
            .GroupBy(x => x.InvoiceDate.Month)
            .Select(g => new { Month = g.Key, InvoiceTotal = g.Sum(x => x.InvoiceTotal) })
            .ToDictionary(x => x.Month, x => x.InvoiceTotal);

        var monthlySales = Enumerable.Range(1, 12)
            .Select(m => new MonthlySalesDto(m, monthlySalesRaw.TryGetValue(m, out var v) ? v : 0m))
            .ToList();

        // 未回収TOP5（期限超過優先 → 残額大きい順）
        var today = DateTime.UtcNow.Date;

        var unpaidTop5 = invoiceAgg
            .Select(x =>
            {
                var remaining = Math.Max(0m, x.InvoiceTotal - x.PaidTotal);
                var overdue = remaining > 0m && x.DueDate.Date < today;
                return new { x, remaining, overdue };
            })
            .Where(z => z.remaining > 0m)
            .OrderByDescending(z => z.overdue)
            .ThenByDescending(z => z.remaining)
            .ThenBy(z => z.x.DueDate)
            .Take(5)
            .Select(z => new UnpaidInvoiceDto(
                z.x.Id,
                z.x.InvoiceNumber,
                z.x.ClientName,
                z.x.DueDate,
                z.x.InvoiceTotal,
                z.x.PaidTotal,
                z.remaining,
                z.overdue
            ))
            .ToList();

        return new AdminSummaryDto(
            Year: year,
            InvoiceTotal: invoiceTotal,
            PaidTotal: paidTotal,
            RemainingTotal: remainingTotal,
            RecoveryRate: Math.Round(recoveryRate, 2),
            InvoiceCount: invoiceCount,
            PaymentCount: paymentCount,
            MonthlySales: monthlySales,
            UnpaidTop5: unpaidTop5
        );
    }
}

