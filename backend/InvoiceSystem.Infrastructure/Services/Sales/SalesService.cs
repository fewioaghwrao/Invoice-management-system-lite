using InvoiceSystem.Application.Dtos.Sales;
using InvoiceSystem.Application.Queries.Sales;
using InvoiceSystem.Application.Services.Sales;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Infrastructure.Services.Sales;

public class SalesService : ISalesService
{
    private readonly AppDbContext _db;

    public SalesService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<SalesListResultDto> SearchAsync(SalesSearchRequest req)
    {
        string? memberName = null;
        var year = req.Year ?? DateTime.Today.Year;
        var month = req.Month; // null=all
        var keyword = (req.Q ?? "").Trim();
        var status = (req.Status ?? "all").Trim().ToLowerInvariant();

        var page = Math.Max(1, req.Page ?? 1);
        var pageSize = Math.Clamp(req.PageSize ?? 10, 1, 100);

        // Base：請求 + 顧客
        var baseQuery =
            from i in _db.Invoices.AsNoTracking()
            join m in _db.Members.AsNoTracking() on i.MemberId equals m.Id
            where i.InvoiceDate.Year == year
            where month == null || i.InvoiceDate.Month == month.Value
            select new { i, m };

        // ★ 顧客ドリルダウン
        if (req.MemberId.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.m.Id == req.MemberId.Value);
        }

        if (req.MemberId.HasValue)
        {
            memberName = await _db.Members
                .Where(m => m.Id == req.MemberId.Value)
                .Select(m => m.Name)
                .FirstOrDefaultAsync();
        }

        if (!string.IsNullOrEmpty(keyword))
        {
            baseQuery = baseQuery.Where(x =>
                x.i.InvoiceNumber.Contains(keyword) ||
                x.m.Name.Contains(keyword));
        }

        // 集計（請求額 / 入金額 / 最終入金日）
        var query =
            from x in baseQuery
            let invoiceAmount = x.i.TotalAmount
            let paidAmount = x.i.PaymentAllocations.Sum(a => (decimal?)a.Amount) ?? 0m
            let lastPaidAt = x.i.PaymentAllocations.Max(a => (DateTime?)a.Payment.PaymentDate)
            let statusCode =
                paidAmount <= 0m ? "UNPAID"
                : (paidAmount < invoiceAmount ? "PARTIAL" : "PAID")
            select new
            {
                InvoiceId = x.i.Id,
                x.i.InvoiceNumber,
                ClientName = x.m.Name,
                IssuedAt = x.i.InvoiceDate,
                DueAt = x.i.DueDate,
                InvoiceAmount = invoiceAmount,
                PaidAmount = paidAmount,
                LastPaidAt = lastPaidAt,
                StatusCode = statusCode
            };

        if (status != "all")
        {
            var want = status switch
            {
                "paid" => "PAID",
                "partial" => "PARTIAL",
                "unpaid" => "UNPAID",
                _ => "ALL"
            };
            if (want != "ALL")
                query = query.Where(x => x.StatusCode == want);
        }

        // TotalCount
        var totalCount = await query.CountAsync();

        // Summary（①②③④）
        var agg = await query
            .GroupBy(_ => 1)
            .Select(g => new
            {
                InvoiceTotal = g.Sum(x => x.InvoiceAmount),
                PaidTotal = g.Sum(x => x.PaidAmount)
            })
            .FirstOrDefaultAsync();

        var invoiceTotal = agg?.InvoiceTotal ?? 0m;
        var paidTotal = agg?.PaidTotal ?? 0m;
        var remainingTotal = invoiceTotal - paidTotal;
        var recoveryRate = invoiceTotal <= 0m ? 0m : Math.Round(paidTotal / invoiceTotal * 100m, 1);

        // Page rows
        var rows = await query
            .OrderByDescending(x => x.IssuedAt)
            .ThenByDescending(x => x.InvoiceId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SalesRowDto(
                x.InvoiceId,
                x.InvoiceNumber,
                x.ClientName,
                x.IssuedAt,
                x.DueAt,
                x.StatusCode,
                x.InvoiceAmount,
                x.PaidAmount,
                x.InvoiceAmount - x.PaidAmount,
                x.LastPaidAt
            ))
            .ToListAsync();

        return new SalesListResultDto(
            Year: year,
            Month: month?.ToString() ?? "all",
            Keyword: keyword,
            Status: status,
            Page: page,
            PageSize: pageSize,
            TotalCount: totalCount,

    MemberName: memberName,
            Rows: rows,
            Summary: new SalesSummaryDto(invoiceTotal, paidTotal, remainingTotal, recoveryRate)
        );
    }

    public async Task<SalesByMemberResultDto> SearchByMemberAsync(SalesSearchRequest req)
    {
        var year = req.Year ?? DateTime.Today.Year;
        var month = req.Month; // null=all
        var keyword = (req.Q ?? "").Trim();

        var page = Math.Max(1, req.Page ?? 1);
        var pageSize = Math.Clamp(req.PageSize ?? 10, 1, 100);

        // ===============================
        // ベース：請求 × 会員
        // ===============================
        var baseQuery =
            from i in _db.Invoices.AsNoTracking()
            join m in _db.Members.AsNoTracking() on i.MemberId equals m.Id
            where i.InvoiceDate.Year == year
            where month == null || i.InvoiceDate.Month == month.Value
            select new { i, m };

        if (!string.IsNullOrEmpty(keyword))
        {
            baseQuery = baseQuery.Where(x => x.m.Name.Contains(keyword));
        }

        // ===============================
        // 請求単位で金額確定
        // ===============================
        var invoiceAgg =
            from x in baseQuery
            let invoiceAmount = x.i.TotalAmount
            let paidAmount = x.i.PaymentAllocations.Sum(a => (decimal?)a.Amount) ?? 0m
            select new
            {
                x.m.Id,
                x.m.Name,
                InvoiceAmount = invoiceAmount,
                PaidAmount = paidAmount
            };

        // ===============================
        // 顧客（Member）単位で集計
        // ===============================
        var memberAgg =
            from x in invoiceAgg
            group x by new { x.Id, x.Name } into g
            select new
            {
                MemberId = g.Key.Id,
                MemberName = g.Key.Name,
                InvoiceTotal = g.Sum(x => x.InvoiceAmount),
                PaidTotal = g.Sum(x => x.PaidAmount)
            };

        // ===============================
        // 全体サマリー（①〜④）
        // ===============================
        var summaryAgg = await memberAgg
            .GroupBy(_ => 1)
            .Select(g => new
            {
                InvoiceTotal = g.Sum(x => x.InvoiceTotal),
                PaidTotal = g.Sum(x => x.PaidTotal)
            })
            .FirstOrDefaultAsync();

        var invoiceTotalAll = summaryAgg?.InvoiceTotal ?? 0m;
        var paidTotalAll = summaryAgg?.PaidTotal ?? 0m;
        var remainingTotalAll = invoiceTotalAll - paidTotalAll;
        var recoveryRateAll =
            invoiceTotalAll <= 0m
                ? 0m
                : Math.Round(paidTotalAll / invoiceTotalAll * 100m, 1);

        // ===============================
        // 件数（顧客数）
        // ===============================
        var totalCount = await memberAgg.CountAsync();

        // ===============================
        // ページング
        // ===============================
        var rows = await memberAgg
            .OrderByDescending(x => x.InvoiceTotal)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SalesByMemberRowDto(
                x.MemberId,
                x.MemberName,
                x.InvoiceTotal,
                x.PaidTotal,
                x.InvoiceTotal - x.PaidTotal,
                x.InvoiceTotal <= 0m
                    ? 0m
                    : Math.Round(x.PaidTotal / x.InvoiceTotal * 100m, 1)
            ))
            .ToListAsync();

        return new SalesByMemberResultDto(
            Year: year,
            Month: month?.ToString() ?? "all",
            Keyword: keyword,
            Page: page,
            PageSize: pageSize,
            TotalCount: totalCount,
            Rows: rows,
            Summary: new SalesSummaryDto(
                invoiceTotalAll,
                paidTotalAll,
                remainingTotalAll,
                recoveryRateAll
            )
        );
    }

    public async Task<IReadOnlyList<SalesExportRow>> ExportAsync(SalesSearchQuery query)
    {
        var inv = _db.Invoices
            .AsNoTracking()
            .Include(i => i.Member)
            .Include(i => i.Status) // ここはあなたのInvoiceの状態参照に合わせて
            .AsQueryable();

        inv = inv.Where(i => i.InvoiceDate.Year == query.Year);

        if (query.Month is not null)
            inv = inv.Where(i => i.InvoiceDate.Month == query.Month.Value);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            inv = inv.Where(i => i.InvoiceNumber.Contains(kw) || i.Member.Name.Contains(kw));
        }

        if (!string.Equals(query.Status, "all", StringComparison.OrdinalIgnoreCase))
        {
            var st = query.Status.ToUpperInvariant(); // "UNPAID"/"PARTIAL"/"PAID" 想定
            inv = inv.Where(i => i.Status.Code == st);
        }

        if (query.MemberId is not null)
            inv = inv.Where(i => i.MemberId == query.MemberId);

        var rows = await inv
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.Id)
            .Select(i => new SalesExportRow
            {
                InvoiceId = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                ClientName = i.Member.Name,
                IssuedAt = i.InvoiceDate,
                DueAt = i.DueDate,
                Status = i.Status.Code,
                InvoiceAmount = i.TotalAmount,

                PaidAmount =
                    _db.PaymentAllocations
                      .Where(a => a.InvoiceId == i.Id)
                      .Sum(a => (decimal?)a.Amount) ?? 0m,

                RemainingAmount = i.TotalAmount - (
                    _db.PaymentAllocations
                      .Where(a => a.InvoiceId == i.Id)
                      .Sum(a => (decimal?)a.Amount) ?? 0m
                ),

                LastPaidAt =
                    (from a in _db.PaymentAllocations
                     join p in _db.Payments on a.PaymentId equals p.Id
                     where a.InvoiceId == i.Id
                     select (DateTime?)p.PaymentDate
                    ).Max(),
            })
            .ToListAsync();

        return rows;
    }

}
