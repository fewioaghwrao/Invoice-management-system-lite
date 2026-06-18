using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Application.Common.Interfaces;
using InvoiceSystem.Application.Dtos.Invoices;
using InvoiceSystem.Application.Queries.Invoices;
using InvoiceSystem.Domain.Entities;
using InvoiceSystem.Domain.Enums;
using InvoiceSystem.Infrastructure;
using InvoiceSystem.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using PaymentEntity = InvoiceSystem.Domain.Entities.Payment;

namespace InvoiceSystem.Tests.Services.Invoices;

public sealed class InvoiceServiceTests
{
    private static (AppDbContext db, SqliteConnection conn) CreateDb()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(conn)
            .EnableSensitiveDataLogging()
            .Options;

        var db = new AppDbContext(options);
        db.Database.EnsureCreated();

        return (db, conn);
    }

    private static InvoiceService CreateSut(AppDbContext db)
        => new(db, new NoopAuditLogger());

    [Fact]
    public async Task CreateWithLinesAsync_CalculatesTotalAmountAndNormalizesLineNo()
    {
        // Arrange
        var (db, conn) = CreateDb();
        await using var _ = conn;

        var member = CreateMember("Customer A", "customer-a@example.com");
        db.Members.Add(member);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var req = new UpdateInvoiceRequestDto
        {
            MemberId = member.Id,
            InvoiceNumber = "INV-001",
            InvoiceDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            DueDate = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            StatusId = 1,
            Remarks = "初回請求",
            Lines = new List<InvoiceLineDto>
            {
                new()
                {
                    LineNo = 20,
                    Name = "Item B",
                    Qty = 2,
                    UnitPrice = 3000m
                },
                new()
                {
                    LineNo = 10,
                    Name = "Item A",
                    Qty = 1,
                    UnitPrice = 5000m
                }
            }
        };

        // Act
        var result = await sut.CreateWithLinesAsync(req);

        // Assert
        Assert.True(result.Id > 0);
        Assert.Equal("INV-001", result.InvoiceNumber);
        Assert.Equal(11000m, result.TotalAmount);

        var invoice = await db.Invoices
            .Include(x => x.Lines)
            .SingleAsync(x => x.Id == result.Id);

        Assert.Equal(11000m, invoice.TotalAmount);

        var lines = invoice.Lines.OrderBy(x => x.LineNo).ToList();
        Assert.Equal(2, lines.Count);

        Assert.Equal(1, lines[0].LineNo);
        Assert.Equal("Item A", lines[0].Name);

        Assert.Equal(2, lines[1].LineNo);
        Assert.Equal("Item B", lines[1].Name);
    }

    [Fact]
    public async Task UpdateAsync_ReplacesLinesAndRecalculatesTotalAmount()
    {
        // Arrange
        var (db, conn) = CreateDb();
        await using var _ = conn;

        var member = CreateMember("Customer A", "customer-a@example.com");
        db.Members.Add(member);
        await db.SaveChangesAsync();

        var invoice = CreateInvoice(
            member.Id,
            "INV-001",
            new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            10000m,
            statusId: 1
        );

        invoice.Lines.Add(new InvoiceLine
        {
            LineNo = 1,
            Name = "Old A",
            Qty = 1,
            UnitPrice = 4000m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        invoice.Lines.Add(new InvoiceLine
        {
            LineNo = 2,
            Name = "Old B",
            Qty = 1,
            UnitPrice = 6000m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        var existingLine = await db.InvoiceLines
            .OrderBy(x => x.LineNo)
            .FirstAsync(x => x.InvoiceId == invoice.Id);

        var sut = CreateSut(db);

        var req = new UpdateInvoiceRequestDto
        {
            MemberId = member.Id,
            InvoiceNumber = "INV-001-UPDATED",
            InvoiceDate = new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc),
            DueDate = new DateTime(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc),
            StatusId = 2,
            Remarks = "更新後",
            Lines = new List<InvoiceLineDto>
            {
                new()
                {
                    Id = existingLine.Id,
                    LineNo = 20,
                    Name = "Updated A",
                    Qty = 3,
                    UnitPrice = 2000m
                },
                new()
                {
                    LineNo = 10,
                    Name = "New B",
                    Qty = 2,
                    UnitPrice = 5000m
                }
            }
        };

        // Act
        await sut.UpdateAsync(invoice.Id, req);

        // Assert
        var updated = await db.Invoices
            .Include(x => x.Lines)
            .SingleAsync(x => x.Id == invoice.Id);

        Assert.Equal("INV-001-UPDATED", updated.InvoiceNumber);
        Assert.Equal(16000m, updated.TotalAmount);

        var lines = updated.Lines.OrderBy(x => x.LineNo).ToList();

        Assert.Equal(2, lines.Count);
        Assert.Equal(1, lines[0].LineNo);
        Assert.Equal("New B", lines[0].Name);
        Assert.Equal(10000m, lines[0].Qty * lines[0].UnitPrice);

        Assert.Equal(2, lines[1].LineNo);
        Assert.Equal("Updated A", lines[1].Name);
        Assert.Equal(6000m, lines[1].Qty * lines[1].UnitPrice);

        Assert.DoesNotContain(lines, x => x.Name == "Old B");
    }

    [Fact]
    public async Task DeleteAsync_WithPaymentAllocation_ThrowsInvalidOperationException()
    {
        // Arrange
        var (db, conn) = CreateDb();
        await using var _ = conn;

        var member = CreateMember("Customer A", "customer-a@example.com");
        db.Members.Add(member);
        await db.SaveChangesAsync();

        var invoice = CreateInvoice(
            member.Id,
            "INV-001",
            DateTime.UtcNow.AddDays(-10),
            DateTime.UtcNow.AddDays(10),
            10000m,
            statusId: 1
        );

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        var payment = CreatePayment(member.Id, DateTime.UtcNow, 3000m);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        db.PaymentAllocations.Add(new PaymentAllocation
        {
            InvoiceId = invoice.Id,
            PaymentId = payment.Id,
            Amount = 3000m
        });

        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.DeleteAsync(invoice.Id)
        );

        Assert.Equal("入金割当が存在するため、この請求書は削除できません。", ex.Message);
    }

    [Fact]
    public async Task GetDetailByIdAsync_ReturnsLinesAllocationsAndReminders()
    {
        // Arrange
        var (db, conn) = CreateDb();
        await using var _ = conn;

        var member = CreateMember("Customer A", "customer-a@example.com");
        db.Members.Add(member);
        await db.SaveChangesAsync();

        var invoice = CreateInvoice(
            member.Id,
            "INV-DETAIL-001",
            DateTime.UtcNow.AddDays(-10),
            DateTime.UtcNow.AddDays(10),
            10000m,
            statusId: 1
        );

        invoice.Lines.Add(new InvoiceLine
        {
            LineNo = 1,
            Name = "Item A",
            Qty = 1,
            UnitPrice = 7000m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        invoice.Lines.Add(new InvoiceLine
        {
            LineNo = 2,
            Name = "Item B",
            Qty = 1,
            UnitPrice = 3000m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        var payment = CreatePayment(member.Id, DateTime.UtcNow, 4000m);
        payment.PayerName = "Customer A";
        payment.Method = "BANK";
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        db.PaymentAllocations.Add(new PaymentAllocation
        {
            InvoiceId = invoice.Id,
            PaymentId = payment.Id,
            Amount = 4000m
        });

        db.ReminderHistories.Add(new ReminderHistory
        {
            InvoiceId = invoice.Id,
            RemindedAt = DateTime.UtcNow,
            Method = "EMAIL",
            Tone = "SOFT",
            Title = "催促",
            Note = "確認お願いします",
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        // Act
        var detail = await sut.GetDetailByIdAsync(invoice.Id);

        // Assert
        Assert.NotNull(detail);
        Assert.Equal("INV-DETAIL-001", detail.InvoiceNumber);
        Assert.Equal(10000m, detail.TotalAmount);
        Assert.Equal(4000m, detail.PaidAmount);
        Assert.Equal(6000m, detail.RemainingAmount);

        Assert.Equal(2, detail.Lines.Count);
        Assert.Equal("Item A", detail.Lines[0].Name);
        Assert.Equal("Item B", detail.Lines[1].Name);

        Assert.Single(detail.Allocations);
        Assert.Equal(4000m, detail.Allocations[0].AllocatedAmount);
        Assert.Equal("Customer A", detail.Allocations[0].PayerName);
        Assert.Equal("BANK", detail.Allocations[0].Method);

        Assert.Single(detail.Reminders);
        Assert.Equal("EMAIL", detail.Reminders[0].Method);
        Assert.Equal("確認お願いします", detail.Reminders[0].Note);
    }

    [Fact]
    public async Task SearchMyInvoicesAsync_FiltersByPaidStatus()
    {
        // Arrange
        var (db, conn) = CreateDb();
        await using var _ = conn;

        var member = CreateMember("Customer A", "customer-a@example.com");
        db.Members.Add(member);
        await db.SaveChangesAsync();

        var unpaid = CreateInvoice(member.Id, "INV-UNPAID", new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), DateTime.UtcNow.AddDays(10), 10000m, 1);
        var partial = CreateInvoice(member.Id, "INV-PARTIAL", new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc), DateTime.UtcNow.AddDays(10), 10000m, 1);
        var paid = CreateInvoice(member.Id, "INV-PAID", new DateTime(2026, 6, 3, 0, 0, 0, DateTimeKind.Utc), DateTime.UtcNow.AddDays(10), 10000m, 1);

        db.Invoices.AddRange(unpaid, partial, paid);
        await db.SaveChangesAsync();

        var payment1 = CreatePayment(member.Id, DateTime.UtcNow, 5000m);
        var payment2 = CreatePayment(member.Id, DateTime.UtcNow, 10000m);

        db.Payments.AddRange(payment1, payment2);
        await db.SaveChangesAsync();

        db.PaymentAllocations.AddRange(
            new PaymentAllocation
            {
                InvoiceId = partial.Id,
                PaymentId = payment1.Id,
                Amount = 5000m
            },
            new PaymentAllocation
            {
                InvoiceId = paid.Id,
                PaymentId = payment2.Id,
                Amount = 10000m
            }
        );

        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var query = new MyInvoiceSearchQuery
        {
            MemberId = member.Id,
            Year = 2026,
            Month = "all",
            Status = "paid",
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await sut.SearchMyInvoicesAsync(query);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("INV-PAID", result.Items[0].InvoiceNumber);
        Assert.Equal(10000m, result.Items[0].PaidAmount);
        Assert.Equal(0m, result.Items[0].RemainingAmount);
    }

    private static Member CreateMember(string name, string email)
    {
        var now = DateTime.UtcNow;

        return new Member
        {
            Name = name,
            Email = email,
            PasswordHash = "dummy-hash",
            Role = MemberRole.Customer,
            IsActive = true,
            IsEmailConfirmed = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static Invoice CreateInvoice(
        long memberId,
        string invoiceNumber,
        DateTime invoiceDate,
        DateTime dueDate,
        decimal totalAmount,
        long statusId)
    {
        var now = DateTime.UtcNow;

        return new Invoice
        {
            MemberId = memberId,
            InvoiceNumber = invoiceNumber,
            InvoiceDate = invoiceDate,
            DueDate = dueDate,
            TotalAmount = totalAmount,
            StatusId = statusId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static PaymentEntity CreatePayment(
        long memberId,
        DateTime paymentDate,
        decimal amount)
    {
        var now = DateTime.UtcNow;

        return new PaymentEntity
        {
            MemberId = memberId,
            PaymentDate = paymentDate,
            Amount = amount,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

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