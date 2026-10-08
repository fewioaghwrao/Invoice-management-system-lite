using InvoiceSystem.Application.Dtos.AccessExport;
using InvoiceSystem.Application.Queries.AccessExport;
using InvoiceSystem.Application.Services.AccessExport;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Infrastructure.Services.AccessExport;

public sealed class AccessExportService : IAccessExportService
{
    private readonly AppDbContext _db;

    public AccessExportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AccessExportDataDto> ExportAsync(
        AccessExportQuery query)
    {
        var nextMonthStart = new DateTime(
            query.Year,
            query.Month,
            1,
            0,
            0,
            0,
            DateTimeKind.Utc)
            .AddMonths(1);

        // -----------------------------
        // Invoice
        // -----------------------------
        var invoices = await _db.Invoices
            .AsNoTracking()
            .Where(i => i.InvoiceDate < nextMonthStart)
            .OrderBy(i => i.Id)
            .Select(i => new AccessInvoiceExportRow(
                i.Id,
                i.MemberId,
                i.InvoiceNumber,
                i.InvoiceDate,
                i.DueDate,
                i.TotalAmount,
                i.Status.Code,
                i.Status.Name,
                i.Status.IsOverdue,
                i.Status.IsClosed,
                i.Member.Name))
            .ToListAsync();

        // -----------------------------
        // Payment
        // -----------------------------
        var payments = await _db.Payments
            .AsNoTracking()
            .Where(p => p.PaymentDate < nextMonthStart)
            .OrderBy(p => p.Id)
            .Select(p => new AccessPaymentExportRow(
                p.Id,
                p.MemberId,
                p.PaymentDate,
                p.Amount,
                p.PayerName,
                p.Method))
            .ToListAsync();

        // -----------------------------
        // PaymentAllocation
        // -----------------------------
        var allocations = await _db.PaymentAllocations
            .AsNoTracking()
            .Where(a =>
                a.Invoice.InvoiceDate < nextMonthStart &&
                a.Payment.PaymentDate < nextMonthStart)
            .OrderBy(a => a.Id)
            .Select(a => new AccessAllocationExportRow(
                a.Id,
                a.PaymentId,
                a.InvoiceId,
                a.Amount))
            .ToListAsync();

        return new AccessExportDataDto(
            query.Year,
            query.Month,
            invoices,
            payments,
            allocations);
    }
}