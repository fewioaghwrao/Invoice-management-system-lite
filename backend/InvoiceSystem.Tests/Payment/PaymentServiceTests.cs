using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

using InvoiceSystem.Infrastructure;              // AppDbContext
using InvoiceSystem.Infrastructure.Services;     // PaymentService
using InvoiceSystem.Domain.Entities;             // Member/Invoice/Payment
using InvoiceSystem.Application.Services;        // SaveAllocationLine
using InvoiceSystem.Application.Common.Interfaces; // IAuditLogger, AuditActor

namespace InvoiceSystem.Tests.Services
{
    public class PaymentServiceTests
    {
        // --------------------------
        // DbContext (SQLite in-memory)
        // --------------------------
        private static (AppDbContext db, SqliteConnection conn) CreateDb()
        {
            var conn = new SqliteConnection("DataSource=:memory:");
            conn.Open(); // ★開きっぱなしでDB維持

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(conn)
                .EnableSensitiveDataLogging()
                .Options;

            var db = new AppDbContext(options);
            db.Database.EnsureCreated(); // ★HasData(InvoiceStatus) seed が入る

            return (db, conn);
        }

        private static PaymentService CreateSut(AppDbContext db)
            => new PaymentService(db, new NoopAuditLogger());

        // --------------------------
        // Test 1: UNPAID -> PARTIAL -> PAID
        // --------------------------
        [Fact]
        public async Task SaveAllocations_Recalc_UnpaidToPartialToPaid_ByReplace()
        {
            var (db, conn) = CreateDb();
            await using var _ = conn; // connection closeで破棄

            // Member（必須プロパティを埋める）
            var member = new Member
            {
                Name = "Taro",
                Email = "taro@example.com",
                PasswordHash = "dummy-hash",
                IsActive = true,
                Role = Domain.Enums.MemberRole.Customer,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            db.Members.Add(member);
            await db.SaveChangesAsync();

            // Invoice（StatusId: 1=UNPAID,2=PARTIAL,3=PAID,4=OVERDUE,5=CANCELLED）
            var invoice = new Invoice
            {
                MemberId = member.Id,
                InvoiceNumber = "INV-001",
                InvoiceDate = DateTime.UtcNow.AddDays(-1),
                DueDate = DateTime.UtcNow.AddDays(10), // 期限超過分岐を避ける
                TotalAmount = 1000m,
                StatusId = 1, // UNPAID
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            db.Invoices.Add(invoice);

            // Payment
            var payment = new Payment
            {
                MemberId = member.Id,
                PaymentDate = DateTime.UtcNow,
                Amount = 1000m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            db.Payments.Add(payment);

            await db.SaveChangesAsync();

            var sut = CreateSut(db);

            // ① 200円割当（置き換え）=> PARTIAL
            await sut.SaveAllocationsAsync(payment.Id, new[]
            {
                new SaveAllocationLine(invoice.Id, 200m)
            });

            var inv1 = await db.Invoices.AsNoTracking().FirstAsync(x => x.Id == invoice.Id);
            Assert.Equal(2, inv1.StatusId); // PARTIAL

            // ② 1000円に置き換え => PAID
            await sut.SaveAllocationsAsync(payment.Id, new[]
            {
                new SaveAllocationLine(invoice.Id, 1000m)
            });

            var inv2 = await db.Invoices.AsNoTracking().FirstAsync(x => x.Id == invoice.Id);
            Assert.Equal(3, inv2.StatusId); // PAID
        }

        // --------------------------
        // Test 2: DueDate past -> OVERDUE (when unpaid)
        // --------------------------
        [Fact]
        public async Task SaveAllocations_Recalc_UnpaidBecomesOverdue_WhenDueDatePast()
        {
            var (db, conn) = CreateDb();
            await using var _ = conn;

            var member = new Member
            {
                Name = "Hanako",
                Email = "hanako@example.com",
                PasswordHash = "dummy",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            db.Members.Add(member);
            await db.SaveChangesAsync();

            var invoice = new Invoice
            {
                MemberId = member.Id,
                InvoiceNumber = "INV-OD-001",
                InvoiceDate = DateTime.UtcNow.AddDays(-30),
                DueDate = DateTime.UtcNow.AddDays(-1), // 期限超過
                TotalAmount = 1000m,
                StatusId = 1, // UNPAID
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            db.Invoices.Add(invoice);

            var payment = new Payment
            {
                MemberId = member.Id,
                PaymentDate = DateTime.UtcNow,
                Amount = 1000m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            db.Payments.Add(payment);
            await db.SaveChangesAsync();

            var sut = CreateSut(db);

            // ★① 一度割当を入れる（beforeInvoiceIds を作る）
            await sut.SaveAllocationsAsync(payment.Id, new[]
            {
        new SaveAllocationLine(invoice.Id, 100m)
    });

            // ★② クリア → 再計算され、期限超過なので OVERDUE
            await sut.SaveAllocationsAsync(payment.Id, Array.Empty<SaveAllocationLine>());

            var inv = await db.Invoices.AsNoTracking().FirstAsync();
            Assert.Equal(4, inv.StatusId); // OVERDUE
        }


        // --------------------------
        // Test double
        // --------------------------
        private sealed class NoopAuditLogger : IAuditLogger
        {
            public Task WriteAsync(
                string action,
                string entity,
                string? entityId = null,
                string? summary = null,
                object? data = null,
                AuditActor? actor = null,
                CancellationToken ct = default)
                => Task.CompletedTask;
        }
    }
}
