using InvoiceSystem.Domain.Entities;
using InvoiceSystem.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Infrastructure.Data.Seed;

public static class DemoDataSeeder
{
    public static void Seed(AppDbContext context, IPasswordHasher<Member> hasher)
    {
        var now = new DateTime(2026, 1, 8, 0, 0, 0, DateTimeKind.Utc);


        // -----------------------------
        // Members (Upsert by Email)
        // -----------------------------
        Member UpsertMember(string name, string email, MemberRole role, string rawPassword,
            string? postal = null, string? address = null, string? phone = null)
        {
            var m = context.Members.SingleOrDefault(x => x.Email == email);

            if (m == null)
            {
                m = new Member
                {
                    Name = name,
                    Email = email,
                    PostalCode = postal,
                    Address = address,
                    Phone = phone,
                    IsActive = true,
                    Role = role,
                    CreatedAt = now,
                    UpdatedAt = now,
                    IsEmailConfirmed = true
                };
                m.PasswordHash = hasher.HashPassword(m, rawPassword);
                context.Members.Add(m);
                context.SaveChanges();
            }
            else
            {
                // 最低限の補正（必要なら）
                m.Name = name;
                m.Role = role;
                m.IsActive = true;
                m.IsEmailConfirmed = true;
                m.PostalCode ??= postal;
                m.Address ??= address;
                m.Phone ??= phone;
                m.UpdatedAt = now;

                // パスワードを固定で上書きしたいなら有効化（任意）
                // m.PasswordHash = hasher.HashPassword(m, rawPassword);

                context.SaveChanges();
            }
            return m;
        }

        var admin = UpsertMember("デモ管理者", "admin@example.com", MemberRole.Admin, "Admin1234!",
            "1000001", "東京都千代田区テスト1-1-1", "090-1111-2222");

        var member1 = UpsertMember("デモ会員1", "member@example.com", MemberRole.Customer, "Member1234!",
            "1000002", "東京都千代田区テスト2-2-2", "090-2222-3333");

        var member2 = UpsertMember("デモ会員2", "member2@example.com", MemberRole.Customer, "Test1234!",
            "1000003", "東京都千代田区テスト3-3-3", "090-3333-4444");

        var member3 = UpsertMember("デモ会員3", "member3@example.com", MemberRole.Customer, "Test1234!",
            "1000004", "東京都千代田区テスト4-4-4", "090-4444-5555");

        var member4 = UpsertMember("デモ会員4", "member4@example.com", MemberRole.Customer, "Test1234!",
            "1000005", "東京都千代田区テスト5-5-5", "090-5555-6666");

        // -----------------------------
        // Status master（5種を取得）
        // -----------------------------
        var stUnpaid = MustGetStatus(context, "UNPAID");
        var stPartial = MustGetStatus(context, "PARTIAL");
        var stPaid = MustGetStatus(context, "PAID");
        var stOverdue = MustGetStatus(context, "OVERDUE");
        var stDunning = MustGetStatus(context, "DUNNING"); // 今回は作るだけで未使用

        // -----------------------------
        // Helper: Invoice + Lines (idempotent by InvoiceNumber)
        // Helper: Payment (idempotent by MemberId + PaymentDate + Amount)
        // Helper: Allocation
        // -----------------------------
        Invoice UpsertInvoice(
            Member m,
            string invoiceNo,
            DateTime invoiceDateUtc,
            DateTime dueDateUtc,
            params (int lineNo, string name, int qty, decimal unitPrice)[] lines)
        {
            var inv = context.Invoices
                .Include(x => x.Lines)
                .SingleOrDefault(x => x.InvoiceNumber == invoiceNo);

            var total = lines.Sum(x => x.qty * x.unitPrice);

            if (inv == null)
            {
                inv = new Invoice
                {
                    MemberId = m.Id,
                    InvoiceNumber = invoiceNo,
                    InvoiceDate = invoiceDateUtc,
                    DueDate = dueDateUtc,
                    TotalAmount = total,
                    StatusId = stUnpaid.Id, // ★後で再計算
                    CreatedAt = now,
                    UpdatedAt = now,
                    Lines = new List<InvoiceLine>()
                };

                foreach (var (lineNo, name, qty, unitPrice) in lines)
                {
                    inv.Lines.Add(new InvoiceLine
                    {
                        LineNo = lineNo,
                        Name = name,
                        Qty = qty,
                        UnitPrice = unitPrice,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }

                context.Invoices.Add(inv);
                context.SaveChanges();
            }
            else
            {
                inv.MemberId = m.Id;
                inv.InvoiceDate = invoiceDateUtc;
                inv.DueDate = dueDateUtc;
                inv.TotalAmount = total;
                inv.UpdatedAt = now;

                if (inv.Lines == null || inv.Lines.Count == 0)
                {
                    inv.Lines = new List<InvoiceLine>();
                    foreach (var (lineNo, name, qty, unitPrice) in lines)
                    {
                        inv.Lines.Add(new InvoiceLine
                        {
                            LineNo = lineNo,
                            Name = name,
                            Qty = qty,
                            UnitPrice = unitPrice,
                            CreatedAt = now,
                            UpdatedAt = now
                        });
                    }
                }

                context.SaveChanges();
            }

            return inv;
        }

        Payment UpsertPayment(Member m, DateTime paymentDateUtc, decimal amount, string method = "銀行振込", string? payerName = null)
        {
            var p = context.Payments.SingleOrDefault(x =>
                x.MemberId == m.Id &&
                x.PaymentDate == paymentDateUtc &&
                x.Amount == amount);

            if (p == null)
            {
                p = new Payment
                {
                    MemberId = m.Id,
                    PaymentDate = paymentDateUtc,
                    Amount = amount,
                    Method = method,
                    PayerName = payerName ?? m.Name,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                context.Payments.Add(p);
                context.SaveChanges();
            }

            return p;
        }

        void EnsureAllocation(Payment p, Invoice inv, decimal amount)
        {
            var exists = context.PaymentAllocations.Any(x =>
                x.PaymentId == p.Id &&
                x.InvoiceId == inv.Id &&
                x.Amount == amount);

            if (!exists)
            {
                context.PaymentAllocations.Add(new PaymentAllocation
                {
                    PaymentId = p.Id,
                    InvoiceId = inv.Id,
                    Amount = amount
                });
                context.SaveChanges();
            }
        }

        // UTC helper
        DateTime Utc(int y, int m, int d) => new DateTime(y, m, d, 0, 0, 0, DateTimeKind.Utc);

        // PaymentDate を衝突しないようにズラす
        DateTime PayAt(int y, int m, int day, int memberNo, int invSeq, int splitNo)
        {
            var baseUtc = Utc(y, m, day);

            // ★月(m)も混ぜて衝突をさらに避ける
            var minutes = (memberNo * 300) + (m * 20) + (invSeq * 5) + (splitNo - 1);

            return baseUtc.AddMinutes(minutes);
        }

        // ==========================================================
        // ① 2025年11月（完全固定：過去・完結 + overdue）
        // ==========================================================
        {
            var y = 2025; var m = 11;
            var inv = UpsertInvoice(member1, $"INV-{y}-{m:00}-FIX-M1-001", Utc(y, m, 5), Utc(y, m, 30),
                (1, $"月額利用料（{y}/{m:00}）", 1, 120000m));
            var p = UpsertPayment(member1, PayAt(y, m, 20, 1, 1, 1), 120000m);
            EnsureAllocation(p, inv, 120000m);
        }

        {
            var y = 2025; var m = 11;
            var inv = UpsertInvoice(member2, $"INV-{y}-{m:00}-FIX-M2-001", Utc(y, m, 7), Utc(y, m, 30),
                (1, $"月額利用料（{y}/{m:00}）", 1, 200000m));
            var p1 = UpsertPayment(member2, PayAt(y, m, 15, 2, 1, 1), 100000m);
            var p2 = UpsertPayment(member2, PayAt(y, m, 28, 2, 1, 2), 100000m);
            EnsureAllocation(p1, inv, 100000m);
            EnsureAllocation(p2, inv, 100000m);
        }

        {
            var y = 2025; var m = 11;
            _ = UpsertInvoice(member3, $"INV-{y}-{m:00}-FIX-M3-001", Utc(y, m, 10), Utc(y, m, 25),
                (1, $"月額利用料（{y}/{m:00}）", 1, 80000m));
        }

        {
            var y = 2025; var m = 11;
            var inv = UpsertInvoice(member4, $"INV-{y}-{m:00}-FIX-M4-001", Utc(y, m, 12), Utc(y, m, 30),
                (1, $"月額利用料（{y}/{m:00}）", 1, 50000m));
            var p = UpsertPayment(member4, PayAt(y, m, 29, 4, 1, 1), 50000m);
            EnsureAllocation(p, inv, 50000m);
        }

        {
            var y = 2025; var m = 11;
            var inv = UpsertInvoice(member4, $"INV-{y}-{m:00}-FIX-M4-002-DUNNING", Utc(y, m, 2), Utc(y, m, 10),
                (1, $"月額利用料（{y}/{m:00}）", 1, 50000m));
            inv.StatusId = stDunning.Id;
            inv.UpdatedAt = now;
            context.SaveChanges();
        }

        // ==========================================================
        // ② 2025年12月（固定：説明用サンプル）
        // ==========================================================
        {
            var y = 2025; var m = 12;
            var inv = UpsertInvoice(member1, $"INV-{y}-{m:00}-FIX-M1-001", Utc(y, m, 1), Utc(y, m, 31),
                (1, $"月額利用料（{y}/{m:00}）", 1, 150000m));
            var p = UpsertPayment(member1, PayAt(y, m, 10, 1, 1, 1), 50000m);
            EnsureAllocation(p, inv, 50000m);
        }

        {
            var y = 2025; var m = 12;
            _ = UpsertInvoice(member2, $"INV-{y}-{m:00}-FIX-M2-001", Utc(y, m, 3), Utc(y, m, 28),
                (1, $"月額利用料（{y}/{m:00}）", 1, 90000m));
        }

        {
            var y = 2025; var m = 12;
            var inv = UpsertInvoice(member3, $"INV-{y}-{m:00}-FIX-M3-001", Utc(y, m, 5), Utc(y, m, 25),
                (1, $"月額利用料（{y}/{m:00}）", 1, 60000m));
            var p = UpsertPayment(member3, PayAt(y, m, 20, 3, 1, 1), 60000m);
            EnsureAllocation(p, inv, 60000m);
        }

        {
            var y = 2025; var m = 12;
            var inv = UpsertInvoice(member4, $"INV-{y}-{m:00}-FIX-M4-001", Utc(y, m, 8), Utc(y, m, 31),
                (1, $"月額利用料（{y}/{m:00}）", 1, 100000m));
            var p1 = UpsertPayment(member4, PayAt(y, m, 18, 4, 1, 1), 40000m);
            EnsureAllocation(p1, inv, 40000m);
        }

        // ==========================================================
        // ③ 2026年（1年分）: 毎月 会員1〜4 各1枚（=48件）
        // ==========================================================
        for (int month = 1; month <= 12; month++)
        {
            int y = 2026;
            int m = month;

            {
                var inv = UpsertInvoice(member1, $"INV-{y}-{m:00}-M1-001", Utc(y, m, 5), Utc(y, m, 25),
                    (1, $"月額利用料（{y}/{m:00}）", 1, 120000m));

                if (m % 2 == 0)
                {
                    var p = UpsertPayment(member1, PayAt(y, m, 20, 1, 1, 1), 120000m);
                    EnsureAllocation(p, inv, 120000m);
                }
            }

            {
                var inv = UpsertInvoice(member2, $"INV-{y}-{m:00}-M2-001", Utc(y, m, 6), Utc(y, m, 25),
                    (1, $"月額利用料（{y}/{m:00}）", 1, 200000m));

                var p1 = UpsertPayment(member2, PayAt(y, m, 15, 2, 1, 1), 100000m);
                var p2 = UpsertPayment(member2, PayAt(y, m, 24, 2, 1, 2), 100000m);
                EnsureAllocation(p1, inv, 100000m);
                EnsureAllocation(p2, inv, 100000m);
            }

            {
                var inv = UpsertInvoice(member3, $"INV-{y}-{m:00}-M3-001", Utc(y, m, 7), Utc(y, m, 25),
                    (1, $"月額利用料（{y}/{m:00}）", 1, 80000m));

                bool isQuarterEnd = (m % 3 == 0);
                if (!isQuarterEnd)
                {
                    var p = UpsertPayment(member3, PayAt(y, m, 18, 3, 1, 1), 30000m);
                    EnsureAllocation(p, inv, 30000m);
                }
            }

            {
                var inv = UpsertInvoice(member4, $"INV-{y}-{m:00}-M4-001", Utc(y, m, 8), Utc(y, m, 25),
                    (1, $"月額利用料（{y}/{m:00}）", 1, 50000m));

                var p = UpsertPayment(member4, PayAt(y, m, 22, 4, 1, 1), 50000m);
                EnsureAllocation(p, inv, 50000m);
            }
        }

        // ==========================================================
        // ④ Status 再計算（UNPAID / PARTIAL / PAID / OVERDUE）
        // ==========================================================
        var targets = context.Invoices
            .Include(i => i.Status)
            .Include(i => i.PaymentAllocations)
            .Where(i =>
                i.InvoiceNumber.StartsWith("INV-2025-11-") ||
                i.InvoiceNumber.StartsWith("INV-2025-12-") ||
                i.InvoiceNumber.StartsWith("INV-2026-"))
            .ToList();

        foreach (var inv in targets)
        {
            if (inv.Status?.Code == "DUNNING")
                continue;

            var paid = inv.PaymentAllocations.Sum(a => a.Amount);
            var total = inv.TotalAmount;

            bool isOverdue = inv.DueDate.Date < now.Date && paid < total;

            long newStatusId =
                isOverdue ? stOverdue.Id :
                paid <= 0m ? stUnpaid.Id :
                paid < total ? stPartial.Id :
                stPaid.Id;

            if (inv.StatusId != newStatusId)
            {
                inv.StatusId = newStatusId;
                inv.UpdatedAt = now;
            }
        }

        context.SaveChanges();

        // ==========================================================
        // ⑤ AuditLog（管理者トップ表示用：直近5件）
        // ==========================================================
        if (!context.AuditLogs.Any())
        {
            var t1 = new DateTime(2026, 1, 7, 9, 30, 0, DateTimeKind.Utc);
            var t2 = new DateTime(2026, 1, 7, 11, 15, 0, DateTimeKind.Utc);
            var t3 = new DateTime(2026, 1, 8, 9, 10, 0, DateTimeKind.Utc);
            var t4 = new DateTime(2026, 1, 8, 10, 45, 0, DateTimeKind.Utc);
            var t5 = new DateTime(2026, 1, 8, 14, 20, 0, DateTimeKind.Utc);

            context.AuditLogs.AddRange(
                new AuditLog
                {
                    ActorUserId = admin.Id,
                    ActorRole = "Admin",
                    Action = "PAYMENT_CREATED",
                    Entity = "PAYMENT",
                    EntityId = "2026-01-M1-001",
                    Summary = "入金を登録しました（手動登録）",
                    CreatedAt = t1
                },
                new AuditLog
                {
                    ActorUserId = admin.Id,
                    ActorRole = "Admin",
                    Action = "PAYMENT_ALLOCATION_ADDED",
                    Entity = "INVOICE",
                    EntityId = "INV-2025-12-FIX-M1-001",
                    Summary = "請求書に入金を割り当てました",
                    CreatedAt = t2
                },
                new AuditLog
                {
                    ActorUserId = admin.Id,
                    ActorRole = "Admin",
                    Action = "PAYMENT_ALLOCATIONS_REPLACED",
                    Entity = "PAYMENT",
                    EntityId = "2026-01-M2-001",
                    Summary = "入金割当を保存しました（再割当）",
                    CreatedAt = t3
                },
                new AuditLog
                {
                    ActorUserId = admin.Id,
                    ActorRole = "Admin",
                    Action = "DUNNING_LOG_CREATED",
                    Entity = "INVOICE",
                    EntityId = "INV-2025-11-FIX-M4-002-DUNNING",
                    Summary = "催促履歴を追加しました",
                    CreatedAt = t4
                },
                new AuditLog
                {
                    ActorUserId = admin.Id,
                    ActorRole = "Admin",
                    Action = "INVOICE_STATUS_UPDATED",
                    Entity = "INVOICE",
                    EntityId = "INV-2025-11-FIX-M3-001",
                    Summary = "期限超過のためステータスを更新しました",
                    CreatedAt = t5
                }
            );

            context.SaveChanges();
        }
    }

    private static InvoiceStatus MustGetStatus(AppDbContext context, string code)
    {
        return context.InvoiceStatuses.SingleOrDefault(x => x.Code == code)
            ?? throw new InvalidOperationException($"InvoiceStatus not found. Code={code}");
    }
}