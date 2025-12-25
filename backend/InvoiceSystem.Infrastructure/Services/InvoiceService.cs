using InvoiceSystem.Application.Commands.Invoices;
using InvoiceSystem.Application.Dtos.Invoices;
using InvoiceSystem.Application.Queries.Invoices;
using InvoiceSystem.Application.Services;
using InvoiceSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Text;
using QuestPDF.Fluent;


namespace InvoiceSystem.Infrastructure.Services;

public class InvoiceService : IInvoiceService
{
    private readonly AppDbContext _db;

    public InvoiceService(AppDbContext db)
    {
        _db = db;
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

    public async Task<InvoiceDto> CreateAsync(CreateInvoiceCommand command)
    {
        // Member が存在するか簡単にチェック
        var member = await _db.Members.FindAsync(command.MemberId)
                     ?? throw new InvalidOperationException("Member not found");

        var defaultStatus = await _db.InvoiceStatuses
          .FirstOrDefaultAsync(s => s.Code == "UNPAID")
          ?? throw new InvalidOperationException("Default status 'UNPAID' not found");


        var invoice = new Invoice
        {
            MemberId = member.Id,
            InvoiceNumber = command.InvoiceNumber,
            InvoiceDate = EnsureUtc(command.InvoiceDate),
            DueDate = EnsureUtc(command.DueDate),
            TotalAmount = command.TotalAmount,
            StatusId = defaultStatus.Id,
            Remarks = command.Remarks,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        // Member / Status を読み直して DTO へ
        await _db.Entry(invoice).Reference(i => i.Member).LoadAsync();
        await _db.Entry(invoice).Reference(i => i.Status).LoadAsync();

        return ToInvoiceDto(invoice);
    }

    public async Task<InvoiceDto?> GetByIdAsync(long id)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Member)
            .Include(i => i.Status)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);

        return invoice is null ? null : ToInvoiceDto(invoice);
    }

    public async Task<IReadOnlyList<InvoiceListItemDto>> SearchAsync(InvoiceSearchQuery query)
    {
        var q = _db.Invoices
            .Include(i => i.Member)
            .Include(i => i.Status)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.InvoiceNumber))
        {
            var num = query.InvoiceNumber.Trim();
            q = q.Where(i => i.InvoiceNumber.Contains(num));
        }

        if (!string.IsNullOrWhiteSpace(query.MemberName))
        {
            var name = query.MemberName.Trim();
            q = q.Where(i => i.Member.Name.Contains(name));
        }

        if (query.FromInvoiceDate.HasValue)
        {
            var fromUtc = EnsureUtc(query.FromInvoiceDate.Value);
            q = q.Where(i => i.InvoiceDate >= fromUtc);
        }

        if (query.ToInvoiceDate.HasValue)
        {
            // 「日付だけ指定」の場合、23:59:59扱いにしたいなら EndOfDay にするのが親切
            // ただし timestamp with time zone なので “翌日0時未満” が安全
            var toUtc = EnsureUtc(query.ToInvoiceDate.Value);

            // ✅ to を「その日の終わり」扱いにしたい場合はこれ（推奨）
            // 例: 2026-01-08 を指定したら 2026-01-09 00:00 未満まで含める
            if (toUtc.TimeOfDay == TimeSpan.Zero)
            {
                var nextDay = toUtc.Date.AddDays(1);
                q = q.Where(i => i.InvoiceDate < nextDay);
            }
            else
            {
                q = q.Where(i => i.InvoiceDate <= toUtc);
            }
        }

        if (query.StatusId.HasValue)
        {
            q = q.Where(i => i.StatusId == query.StatusId.Value);
        }

        var skip = (query.Page - 1) * query.PageSize;
        q = q
            .OrderByDescending(i => i.InvoiceDate)
            .ThenBy(i => i.InvoiceNumber)
            .Skip(skip)
            .Take(query.PageSize);

        var list = await q.ToListAsync();

        return list.Select(ToInvoiceListItemDto).ToList();
    }

    public async Task UpdateStatusAsync(UpdateInvoiceStatusCommand command)
    {
        var invoice = await _db.Invoices.FindAsync(command.InvoiceId)
                      ?? throw new InvalidOperationException("Invoice not found");

        // ステータス存在チェック
        var statusExists = await _db.InvoiceStatuses
            .AnyAsync(s => s.Id == command.StatusId);

        if (!statusExists)
        {
            throw new InvalidOperationException("InvoiceStatus not found");
        }

        invoice.StatusId = command.StatusId;
        invoice.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    public async Task<InvoiceDto> CreateWithLinesAsync(UpdateInvoiceRequestDto req)
    {
        var member = await _db.Members.FindAsync(req.MemberId)
            ?? throw new InvalidOperationException("Member not found");

        var statusExists = await _db.InvoiceStatuses.AnyAsync(s => s.Id == req.StatusId);
        if (!statusExists)
            throw new InvalidOperationException("InvoiceStatus not found");

        var invoice = new Invoice
        {
            MemberId = member.Id,
            InvoiceNumber = req.InvoiceNumber,
            InvoiceDate = EnsureUtc(req.InvoiceDate),
            DueDate = EnsureUtc(req.DueDate),
            StatusId = req.StatusId,
            Remarks = req.Remarks,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // ✅ Lines 追加（LineNo は 1..n に正規化）
        var ordered = req.Lines.OrderBy(x => x.LineNo).ToList();
        for (int i = 0; i < ordered.Count; i++)
        {
            var l = ordered[i];

            invoice.Lines.Add(new InvoiceLine
            {
                LineNo = i + 1,
                Name = l.Name,
                Qty = l.Qty,
                UnitPrice = l.UnitPrice,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        // ✅ 合計は必ずサーバで再計算
        invoice.TotalAmount = invoice.Lines.Sum(x => x.Qty * x.UnitPrice);

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        // DTO用に読み直し
        await _db.Entry(invoice).Reference(i => i.Member).LoadAsync();
        await _db.Entry(invoice).Reference(i => i.Status).LoadAsync();

        return ToInvoiceDto(invoice);
    }


    public async Task<InvoiceDetailDto?> GetDetailByIdAsync(long id)
{
    var invoice = await _db.Invoices
        .Include(i => i.Member)
        .Include(i => i.Status)
        .Include(i => i.Lines)
        .Include(i => i.PaymentAllocations)
            .ThenInclude(pa => pa.Payment)
        .Include(i => i.ReminderHistories)
        .AsNoTracking()
        .FirstOrDefaultAsync(i => i.Id == id);

    if (invoice is null) return null;

    var paidAmount = invoice.PaymentAllocations.Sum(x => x.Amount);
    var remaining = invoice.TotalAmount - paidAmount;

    return new InvoiceDetailDto
    {
        Id = invoice.Id,
        MemberId = invoice.MemberId,
        MemberName = invoice.Member.Name,

        InvoiceNumber = invoice.InvoiceNumber,
        InvoiceDate = invoice.InvoiceDate,
        DueDate = invoice.DueDate,

        TotalAmount = invoice.TotalAmount,
        PaidAmount = paidAmount,
        RemainingAmount = remaining,

        StatusId = invoice.StatusId,
        StatusName = invoice.Status.Name,

        PdfPath = invoice.PdfPath,
        Remarks = invoice.Remarks,
        CreatedAt = invoice.CreatedAt,

        Lines = invoice.Lines
       .OrderBy(x => x.LineNo)
       .Select(x => new InvoiceLineDto
       {
            Id = x.Id,              // ★これが最重要
            LineNo = x.LineNo,
            Name = x.Name,
            Qty = x.Qty,
            UnitPrice = x.UnitPrice
       })
       .ToList(),

        Allocations = invoice.PaymentAllocations
            .OrderByDescending(a => a.Payment.PaymentDate)
            .ThenByDescending(a => a.PaymentId)
            .Select(a => new InvoicePaymentAllocationDto
            {
                PaymentId = a.PaymentId,
                PaymentDate = a.Payment.PaymentDate,
                AllocatedAmount = a.Amount,

                PayerName = a.Payment.PayerName,
                Method = a.Payment.Method,
                ImportBatchId = a.Payment.ImportBatchId
            })
            .ToList(),

        Reminders = invoice.ReminderHistories
            .OrderByDescending(r => r.RemindedAt)
            .ThenByDescending(r => r.Id)
            .Select(r => new InvoiceReminderHistoryDto
            {
                Id = r.Id,
                RemindedAt = r.RemindedAt,
                Method = r.Method,
                Note = r.Note
            })
            .ToList()
    };
}

    public async Task<bool> DeleteAsync(long id)
    {
        var invoice = await _db.Invoices
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice is null) return false;

        // ★重要：PaymentAllocation -> Invoice が Restrict なので、
        // 入金割当がある請求書は削除させない（409）
        var hasAllocations = await _db.PaymentAllocations
            .AnyAsync(pa => pa.InvoiceId == id);

        if (hasAllocations)
            throw new InvalidOperationException("入金割当が存在するため、この請求書は削除できません。");

        // ReminderHistory は Cascade にしているなら一緒に消える（or 無くてもOK）
        _db.Invoices.Remove(invoice);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task UpdateAsync(long invoiceId, UpdateInvoiceRequestDto req)
    {
        // =========================
        // 0. 事前取得
        // =========================
        var invoice = await _db.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice is null)
            throw new InvalidOperationException("Invoice not found");

        var member = await _db.Members.FindAsync(req.MemberId)
             ?? throw new InvalidOperationException("Member not found");

        invoice.MemberId = member.Id;

        // =========================
        // 1. ステータス存在チェック
        // =========================
        var statusExists = await _db.InvoiceStatuses
            .AnyAsync(s => s.Id == req.StatusId);

        if (!statusExists)
            throw new InvalidOperationException("InvoiceStatus not found");

        // =========================
        // 2. 更新禁止ステータスの防波堤（任意だが強く推奨）
        // =========================
        if (invoice.StatusId is 3 or 5) // PAID / CANCELLED
            throw new InvalidOperationException("This invoice cannot be edited.");

        // =========================
        // 3. Lines: Id 重複チェック
        // =========================
        var duplicate = req.Lines
            .Where(x => x.Id.HasValue)
            .GroupBy(x => x.Id!.Value)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate != null)
            throw new InvalidOperationException($"Duplicate InvoiceLine Id: {duplicate.Key}");

        // =========================
        // 4. ヘッダ更新
        // =========================
        invoice.InvoiceNumber = req.InvoiceNumber;
        invoice.InvoiceDate = EnsureUtc(req.InvoiceDate);
        invoice.DueDate = EnsureUtc(req.DueDate);
        invoice.StatusId = req.StatusId;
        invoice.Remarks = req.Remarks;
        invoice.UpdatedAt = DateTime.UtcNow;

        // =========================
        // 5. 明細（Lines）差分更新
        // =========================

        // 既存行を辞書化
        var existingById = invoice.Lines
            .Where(x => x.Id != 0)
            .ToDictionary(x => x.Id);

        var keepIds = new HashSet<long>();

        // 並び順はサーバで正規化（LineNo 1..n）
        var ordered = req.Lines.OrderBy(x => x.LineNo).ToList();

        for (int i = 0; i < ordered.Count; i++)
        {
            var l = ordered[i];
            var normalizedLineNo = i + 1;

            if (l.Id.HasValue)
            {
                // --- 既存行更新 ---
                if (!existingById.TryGetValue(l.Id.Value, out var entity))
                    throw new InvalidOperationException(
                        $"InvoiceLine {l.Id.Value} does not belong to invoice {invoiceId}");

                keepIds.Add(entity.Id);

                entity.LineNo = normalizedLineNo;
                entity.Name = l.Name;
                entity.Qty = l.Qty;
                entity.UnitPrice = l.UnitPrice;
                entity.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // --- 新規行追加 ---
                invoice.Lines.Add(new InvoiceLine
                {
                    LineNo = normalizedLineNo,
                    Name = l.Name,
                    Qty = l.Qty,
                    UnitPrice = l.UnitPrice,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        // =========================
        // 6. 削除（リクエストに含まれなかった行）
        // =========================
        var toRemove = invoice.Lines
            .Where(x => x.Id != 0 && !keepIds.Contains(x.Id))
            .ToList();

        foreach (var r in toRemove)
        {
            invoice.Lines.Remove(r); // ナビゲーションからも外す
        }
        _db.InvoiceLines.RemoveRange(toRemove);

        // =========================
        // 7. 合計金額は必ずサーバで再計算
        // =========================
        invoice.TotalAmount = invoice.Lines.Sum(x => x.Qty * x.UnitPrice);

        // =========================
        // 8. 保存
        // =========================
        await _db.SaveChangesAsync();
    }

    public async Task<MyInvoiceListResultDto> SearchMyInvoicesAsync(MyInvoiceSearchQuery query)
    {
        // -----------------------
        // 0) 入力補正
        // -----------------------
        var year = query.Year;
        var month = string.IsNullOrWhiteSpace(query.Month) ? "all" : query.Month.Trim();
        var status = string.IsNullOrWhiteSpace(query.Status) ? "all" : query.Status.Trim().ToLowerInvariant();
        var qText = (query.Q ?? "").Trim();
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

        // -----------------------
        // 1) ベースクエリ（自分の請求書のみ）
        //   - 重要：PaymentAllocations.Sum がSQLに変換される書き方にする
        // -----------------------
        var baseQuery = _db.Invoices
            .AsNoTracking()
            .Where(i => i.MemberId == query.MemberId)
            .Where(i => i.InvoiceDate.Year == year);

        // month 絞り込み（"all" 以外）
        if (month != "all" && int.TryParse(month, out var m) && m >= 1 && m <= 12)
        {
            baseQuery = baseQuery.Where(i => i.InvoiceDate.Month == m);
        }

        // 検索（請求書番号 / 備考）
        if (!string.IsNullOrWhiteSpace(qText))
        {
            baseQuery = baseQuery.Where(i =>
                i.InvoiceNumber.Contains(qText) ||
                (i.Remarks != null && i.Remarks.Contains(qText)));
        }

        // projection（PaidAmount を集計）
        var projected = baseQuery.Select(i => new
        {
            i.Id,
            i.InvoiceNumber,
            i.InvoiceDate,
            i.DueDate,
            i.TotalAmount,
            PaidAmount = i.PaymentAllocations.Sum(pa => (decimal?)pa.Amount) ?? 0m, // ★NULL対策
            StatusCode = i.Status.Code,
            StatusName = i.Status.Name,
            IsOverdue = i.Status.IsOverdue
        });

        // -----------------------
        // 2) ステータスフィルタ（unpaid / partial / paid / all）
        //   - “入金済み” 判定を「PaidAmount >= TotalAmount」で統一（シンプルで堅い）
        // -----------------------
        if (status == "unpaid")
        {
            projected = projected.Where(x => x.PaidAmount <= 0m);
        }
        else if (status == "partial")
        {
            projected = projected.Where(x => x.PaidAmount > 0m && x.PaidAmount < x.TotalAmount);
        }
        else if (status == "paid")
        {
            projected = projected.Where(x => x.PaidAmount >= x.TotalAmount);
        }
        // all はそのまま

        // -----------------------
        // 3) AvailableYears（UI用：自分の請求書が存在する年一覧）
        //   - ここは別クエリになるが軽い
        // -----------------------
        var years = await _db.Invoices
            .AsNoTracking()
            .Where(i => i.MemberId == query.MemberId)
            .Select(i => i.InvoiceDate.Year)
            .Distinct()
            .OrderByDescending(x => x)
            .ToListAsync();

        if (years.Count == 0) years = new List<int> { year };

        // -----------------------
        // 4) 件数 + ページング
        // -----------------------
        var totalCount = await projected.CountAsync();

        var skip = (page - 1) * pageSize;

        var items = await projected
            .OrderByDescending(x => x.InvoiceDate)
            .ThenByDescending(x => x.Id)
            .Skip(skip)
            .Take(pageSize)
            .Select(x => new MyInvoiceListItemDto(
                x.Id,
                x.InvoiceNumber,
                x.InvoiceDate,
                x.DueDate,
                x.TotalAmount,
                x.PaidAmount,
                x.TotalAmount - x.PaidAmount,
                x.StatusCode,
                x.StatusName,
                x.IsOverdue
            ))
            .ToListAsync();

        return new MyInvoiceListResultDto(
            Year: year,
            AvailableYears: years,
            Month: month,
            Status: status,
            Q: qText,
            Page: page,
            PageSize: pageSize,
            TotalCount: totalCount,
            Items: items
        );
    }


    // --------------------------
    // private mapping helpers
    // --------------------------

    private static InvoiceDto ToInvoiceDto(Invoice i) =>
        new()
        {
            Id = i.Id,
            MemberId = i.MemberId,
            MemberName = i.Member.Name,
            InvoiceNumber = i.InvoiceNumber,
            InvoiceDate = i.InvoiceDate,
            DueDate = i.DueDate,
            TotalAmount = i.TotalAmount,
            StatusId = i.StatusId,
            StatusName = i.Status.Name,
            PdfPath = i.PdfPath,
            Remarks = i.Remarks,
            CreatedAt = i.CreatedAt
        };

    private static InvoiceListItemDto ToInvoiceListItemDto(Invoice i) =>
        new()
        {
            Id = i.Id,
            InvoiceNumber = i.InvoiceNumber,
            InvoiceDate = i.InvoiceDate,
            DueDate = i.DueDate,
            TotalAmount = i.TotalAmount,
            MemberName = i.Member.Name,
            StatusName = i.Status.Name
        };

    public async Task<byte[]> GeneratePdfAsync(long invoiceId)
    {
        // ① 詳細DTOを取得（あなたの既存メソッドを再利用）
        var dto = await GetDetailByIdAsync(invoiceId);
        if (dto is null)
            throw new InvalidOperationException("Invoice not found.");

        // ② PDF生成
        // もし初回で「ライセンス設定が必要」と言われたら Program.cs に QuestPDF.Settings.License を追加（後述）
        var document = new InvoicePdfDocument(dto);

        // A4 / バイト配列
        return document.GeneratePdf();
    }

    // =========================
    // PDF Document
    // =========================
    private sealed class InvoicePdfDocument : IDocument
    {

        // ★ PDF専用（ダミー）発行者情報
        private const string IssuerName = "サンプル株式会社";
        private const string IssuerAddress = "〒100-0000 東京都千代田区1-2-3";
        private const string IssuerTel = "TEL: 03-1234-5678";
        private const string IssuerEmail = "info@example.co.jp";

        // ★ PDF専用（ダミー）振込先
        private const string BankName = "みずほ銀行";
        private const string BankBranch = "東京中央支店";
        private const string BankType = "普通";
        private const string BankAccount = "1234567";
        private const string BankHolder = "サンプルカブシキガイシャ";


        private readonly InvoiceDetailDto _x;

        public InvoicePdfDocument(InvoiceDetailDto dto) => _x = dto;

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Generated by InvoiceSystem").FontSize(9).FontColor(Colors.Grey.Darken1);
                });
            });
        }
        private void ComposeHeader(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("請求書").FontSize(20).SemiBold();
                        c.Item().Text($"請求番号：{_x.InvoiceNumber}").FontSize(10);
                    });

                    row.ConstantItem(220).Column(c =>
                    {
                        col.Item().AlignRight().Text(IssuerName).SemiBold();
                        col.Item().AlignRight().Text(IssuerAddress).FontSize(9);
                        col.Item().AlignRight().Text(IssuerTel).FontSize(9);
                        col.Item().AlignRight().Text(IssuerEmail).FontSize(9);

                        c.Item().AlignRight().Text($"発行日：{_x.InvoiceDate:yyyy-MM-dd}");
                        c.Item().AlignRight().Text($"支払期限：{_x.DueDate:yyyy-MM-dd}");
                    });
                });

                col.Item().PaddingTop(10)
                         .LineHorizontal(1)
                         .LineColor(Colors.Grey.Lighten2);
            });
        }


        private void ComposeContent(IContainer container)
        {
            container.Column(col =>
            {
                // 宛先
                col.Item().PaddingTop(12).Text(t =>
                {
                    t.Span("宛先：").SemiBold();
                    t.Span(_x.MemberName ?? "-");
                    t.Span(" 様");
                });

                // 明細テーブル
                col.Item().PaddingTop(12).Element(ComposeLinesTable);

                // 合計
                col.Item().PaddingTop(10).AlignRight().Element(ComposeTotals);

                // 備考
                var remarks = _x.Remarks ?? "";
                if (!string.IsNullOrWhiteSpace(remarks))
                {
                    col.Item().PaddingTop(12).Text("備考").SemiBold();
                    col.Item().PaddingTop(4).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8)
                        .Text(remarks).FontColor(Colors.Grey.Darken3);
                }

                // 振込先
                col.Item().PaddingTop(16).Element(ComposeBankInfo);
            });
        }

        private void ComposeBankInfo(IContainer container)
        {
            container.Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(10)
                .Column(col =>
                {
                    col.Item().Text("お振込み先").SemiBold();

                    col.Item().PaddingTop(4).Text($"{BankName} {BankBranch}");
                    col.Item().Text($"{BankType} {BankAccount}");
                    col.Item().Text($"口座名義：{BankHolder}");
                });
        }


        private void ComposeLinesTable(IContainer container)
        {
            // lines: [{ lineNo?, name, qty, unitPrice, amount? }]
            var lines = _x.Lines ?? new List<InvoiceLineDto>();

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);   // No
                    columns.RelativeColumn(5);    // 項目
                    columns.ConstantColumn(50);   // 数量
                    columns.ConstantColumn(80);   // 単価
                    columns.ConstantColumn(90);   // 金額
                });

                table.Header(header =>
                {
                    header.Cell().Element(Th).Text("No");
                    header.Cell().Element(Th).Text("項目");
                    header.Cell().Element(Th).AlignRight().Text("数量");
                    header.Cell().Element(Th).AlignRight().Text("単価");
                    header.Cell().Element(Th).AlignRight().Text("金額");
                });

                int i = 1;
                foreach (var l in lines)
                {
                    var name = l.Name ?? "";
                    var qty = (decimal)l.Qty;
                    var unit = l.UnitPrice;
                    var amount = qty * unit;

                    table.Cell().Element(Td).Text(i.ToString());
                    table.Cell().Element(Td).Text(name);
                    table.Cell().Element(Td).AlignRight().Text(l.Qty.ToString("0"));
                    table.Cell().Element(Td).AlignRight().Text(FormatMoney(unit));
                    table.Cell().Element(Td).AlignRight().Text(FormatMoney(amount));
                    i++;
                }


                static IContainer Th(IContainer c) =>
                    c.Background(Colors.Grey.Lighten4)
                     .Border(1).BorderColor(Colors.Grey.Lighten2)
                     .PaddingVertical(6).PaddingHorizontal(6)
                     .DefaultTextStyle(x => x.SemiBold().FontColor(Colors.Grey.Darken2).FontSize(9));

                static IContainer Td(IContainer c) =>
                    c.Border(1).BorderColor(Colors.Grey.Lighten2)
                     .PaddingVertical(6).PaddingHorizontal(6)
                     .DefaultTextStyle(x => x.FontSize(9));
            });
        }

        private void ComposeTotals(IContainer container)
        {
            decimal total = _x.TotalAmount;
            decimal tax = 0m;
            decimal subtotal = total - tax;


            container.Column(col =>
            {
                col.Item().Row(r =>
                {
                    r.ConstantItem(120).AlignRight().Text("小計：");
                    r.ConstantItem(120).AlignRight().Text(FormatMoney(subtotal));
                });
                col.Item().Row(r =>
                {
                    r.ConstantItem(120).AlignRight().Text("税：");
                    r.ConstantItem(120).AlignRight().Text(FormatMoney(tax));
                });
                col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                col.Item().PaddingTop(6).Row(r =>
                {
                    r.ConstantItem(120).AlignRight().Text("合計：").SemiBold();
                    r.ConstantItem(120).AlignRight().Text(FormatMoney(total)).SemiBold().FontSize(12);
                });
            });
        }

        private static string FormatDate(object? x)
        {
            if (x is DateTime dt) return dt.ToString("yyyy-MM-dd");
            if (DateTime.TryParse(Convert.ToString(x), out var d)) return d.ToString("yyyy-MM-dd");
            return "-";
        }

        private static decimal ToDecimal(object? x)
        {
            if (x is decimal d) return d;
            if (x is int i) return i;
            if (x is long l) return l;
            if (x is double db) return (decimal)db;
            if (decimal.TryParse(Convert.ToString(x), out var r)) return r;
            return 0m;
        }

        private static string FormatMoney(decimal v) => $"{v:N0} 円";


    }
}

















