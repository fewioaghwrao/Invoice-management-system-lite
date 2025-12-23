using InvoiceSystem.Application.Dtos.Collections;
using InvoiceSystem.Application.Services;
using InvoiceSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Infrastructure.Services
{
    public class CollectionService : ICollectionService
    {
        private readonly AppDbContext _db;

        public CollectionService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<InvoiceSnapshotDto> GetSnapshotAsync(long invoiceId)
        {
            var invoice = await _db.Invoices
                .AsNoTracking()
                .Include(x => x.Member)
                .Include(x => x.PaymentAllocations)
                .ThenInclude(a => a.Payment)
                .FirstOrDefaultAsync(x => x.Id == invoiceId);

            if (invoice is null) throw new KeyNotFoundException("Invoice not found");

            var paidTotal = invoice.PaymentAllocations.Sum(a => a.Amount);

            return new InvoiceSnapshotDto
            {
                InvoiceId = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                MemberName = invoice.Member.Name,            // Memberのプロパティ名に合わせて修正
                MemberEmail = invoice.Member.Email,          // 無ければ null に
                InvoiceDate = invoice.InvoiceDate,
                DueDate = invoice.DueDate,
                Total = invoice.TotalAmount,
                PaidTotal = paidTotal
            };
        }

        public async Task<List<DunningLogDto>> GetLogsAsync(long invoiceId)
        {
            var logs = await _db.Set<ReminderHistory>()
                .AsNoTracking()
                .Where(x => x.InvoiceId == invoiceId)
                .OrderByDescending(x => x.RemindedAt)
                .Select(x => new DunningLogDto
                {
                    Id = x.Id,
                    At = x.RemindedAt,
                    Channel = x.Method,
                    Title = x.Title ?? "",
                    Memo = x.Note,
                    Tone = x.Tone,
                    NextActionDate = x.NextActionDate
                })
                .ToListAsync();

            return logs;
        }

        public async Task<long> CreateLogAsync(long invoiceId, CreateDunningLogRequestDto req)
        {
            // invoice存在確認（Status も見るので Include）
            var invoice = await _db.Invoices
                .Include(x => x.Status)
                .FirstOrDefaultAsync(x => x.Id == invoiceId);

            if (invoice is null) throw new KeyNotFoundException("Invoice not found");

            // ① ReminderHistory を追加
            var now = DateTime.Now; // 国内運用なら Now 推奨（UTCに統一したいならUtcNowに統一）
            var entity = new ReminderHistory
            {
                InvoiceId = invoiceId,
                RemindedAt = now,
                Method = req.Channel,          // "EMAIL" | "PHONE" | "LETTER"
                Tone = req.Tone,               // "SOFT" | "NORMAL" | "STRONG"
                Title = req.Title,
                Note = req.Memo,
                NextActionDate = req.NextActionDate,
                Subject = req.Subject,
                BodyText = req.BodyText,
                CreatedAt = now,
            };

            _db.Add(entity);

            // ② ステータス更新：未入金/一部入金のときだけ DUNNING にする
            // すでに DUNNING の場合は何もしない
            var code = invoice.Status?.Code;

            if (code is "PENDING" or "PARTIAL")
            {
                var dunningStatusId = await _db.InvoiceStatuses
                    .Where(s => s.Code == "DUNNING")
                    .Select(s => (long?)s.Id)
                    .FirstOrDefaultAsync();

                if (dunningStatusId is null)
                    throw new InvalidOperationException("InvoiceStatus 'DUNNING' is not found. Seed InvoiceStatuses first.");

                // 既に DUNNING なら変更しない（不要だが安全）
                if (invoice.StatusId != dunningStatusId.Value)
                {
                    invoice.StatusId = dunningStatusId.Value;
                    invoice.UpdatedAt = now;
                }
            }

            await _db.SaveChangesAsync();
            return entity.Id;
        }

    }
}