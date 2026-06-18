using System;
using System.Linq;
using System.Threading.Tasks;
using InvoiceSystem.Domain.Entities;
using InvoiceSystem.Domain.Enums;
using InvoiceSystem.Infrastructure;
using InvoiceSystem.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PaymentEntity = InvoiceSystem.Domain.Entities.Payment;
using Xunit;

namespace InvoiceSystem.Tests.Services.Admin;

public sealed class AdminSummaryServiceTests
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

    [Fact]
    public async Task GetSummaryAsync_ReturnsYearSummary()
    {
        // Arrange
        var (db, conn) = CreateDb();
        await using var _ = conn;

        var member = CreateMember("Customer A", "customer-a@example.com");
        db.Members.Add(member);
        await db.SaveChangesAsync();

        var invoice1 = CreateInvoice(
            member.Id,
            "INV-2026-001",
            new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            DateTime.UtcNow.AddDays(10),
            10000m
        );

        var invoice2 = CreateInvoice(
            member.Id,
            "INV-2026-002",
            new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc),
            DateTime.UtcNow.AddDays(10),
            20000m
        );

        // 対象外：2025年
        var invoiceOtherYear = CreateInvoice(
            member.Id,
            "INV-2025-001",
            new DateTime(2025, 12, 10, 0, 0, 0, DateTimeKind.Utc),
            DateTime.UtcNow.AddDays(10),
            99999m
        );

        db.Invoices.AddRange(invoice1, invoice2, invoiceOtherYear);
        await db.SaveChangesAsync();

        var payment1 = CreatePayment(
            member.Id,
            new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc),
            5000m
        );

        var payment2 = CreatePayment(
            member.Id,
            new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc),
            10000m
        );

        // 対象外：2025年の入金件数
        var paymentOtherYear = CreatePayment(
            member.Id,
            new DateTime(2025, 12, 20, 0, 0, 0, DateTimeKind.Utc),
            99999m
        );

        db.Payments.AddRange(payment1, payment2, paymentOtherYear);
        await db.SaveChangesAsync();

        db.PaymentAllocations.AddRange(
            CreateAllocation(payment1.Id, invoice1.Id, 5000m),
            CreateAllocation(payment2.Id, invoice2.Id, 10000m)
        );

        await db.SaveChangesAsync();

        var service = new AdminSummaryService(db);

        // Act
        var result = await service.GetSummaryAsync(2026);

        // Assert
        Assert.Equal(2026, result.Year);

        Assert.Equal(30000m, result.InvoiceTotal);
        Assert.Equal(15000m, result.PaidTotal);
        Assert.Equal(15000m, result.RemainingTotal);
        Assert.Equal(50m, result.RecoveryRate);

        Assert.Equal(2, result.InvoiceCount);
        Assert.Equal(2, result.PaymentCount);

        Assert.Equal(12, result.MonthlySales.Count);
        Assert.Equal(10000m, result.MonthlySales.Single(x => x.Month == 1).InvoiceTotal);
        Assert.Equal(20000m, result.MonthlySales.Single(x => x.Month == 2).InvoiceTotal);
        Assert.Equal(0m, result.MonthlySales.Single(x => x.Month == 3).InvoiceTotal);
    }

    [Fact]
    public async Task GetSummaryAsync_NoInvoices_ReturnsZeroSummaryAnd12Months()
    {
        // Arrange
        var (db, conn) = CreateDb();
        await using var _ = conn;

        var service = new AdminSummaryService(db);

        // Act
        var result = await service.GetSummaryAsync(2026);

        // Assert
        Assert.Equal(2026, result.Year);
        Assert.Equal(0m, result.InvoiceTotal);
        Assert.Equal(0m, result.PaidTotal);
        Assert.Equal(0m, result.RemainingTotal);
        Assert.Equal(0m, result.RecoveryRate);

        Assert.Equal(0, result.InvoiceCount);
        Assert.Equal(0, result.PaymentCount);

        Assert.Equal(12, result.MonthlySales.Count);
        Assert.All(result.MonthlySales, x => Assert.Equal(0m, x.InvoiceTotal));

        Assert.Empty(result.UnpaidTop5);
    }

    [Fact]
    public async Task GetSummaryAsync_UnpaidTop5_PrioritizesOverdueThenRemainingAmount()
    {
        // Arrange
        var (db, conn) = CreateDb();
        await using var _ = conn;

        var member = CreateMember("Customer A", "customer-a@example.com");
        db.Members.Add(member);
        await db.SaveChangesAsync();

        var today = DateTime.UtcNow.Date;

        var overdueSmall = CreateInvoice(
            member.Id,
            "INV-OVERDUE-SMALL",
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            today.AddDays(-10),
            1000m
        );

        var overdueLarge = CreateInvoice(
            member.Id,
            "INV-OVERDUE-LARGE",
            new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            today.AddDays(-5),
            5000m
        );

        var notOverdueLarge = CreateInvoice(
            member.Id,
            "INV-NOT-OVERDUE-LARGE",
            new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc),
            today.AddDays(10),
            100000m
        );

        db.Invoices.AddRange(overdueSmall, overdueLarge, notOverdueLarge);
        await db.SaveChangesAsync();

        var service = new AdminSummaryService(db);

        // Act
        var result = await service.GetSummaryAsync(2026);

        // Assert
        Assert.Equal(3, result.UnpaidTop5.Count);

        Assert.Equal("INV-OVERDUE-LARGE", result.UnpaidTop5[0].InvoiceNumber);
        Assert.True(result.UnpaidTop5[0].IsOverdue);

        Assert.Equal("INV-OVERDUE-SMALL", result.UnpaidTop5[1].InvoiceNumber);
        Assert.True(result.UnpaidTop5[1].IsOverdue);

        Assert.Equal("INV-NOT-OVERDUE-LARGE", result.UnpaidTop5[2].InvoiceNumber);
        Assert.False(result.UnpaidTop5[2].IsOverdue);
    }

    [Fact]
    public async Task GetSummaryAsync_UnpaidTop5_ExcludesFullyPaidInvoices()
    {
        // Arrange
        var (db, conn) = CreateDb();
        await using var _ = conn;

        var member = CreateMember("Customer A", "customer-a@example.com");
        db.Members.Add(member);
        await db.SaveChangesAsync();

        var unpaidInvoice = CreateInvoice(
            member.Id,
            "INV-UNPAID",
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DateTime.UtcNow.AddDays(10),
            10000m
        );

        var paidInvoice = CreateInvoice(
            member.Id,
            "INV-PAID",
            new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            DateTime.UtcNow.AddDays(10),
            20000m
        );

        db.Invoices.AddRange(unpaidInvoice, paidInvoice);
        await db.SaveChangesAsync();

        var payment = CreatePayment(
            member.Id,
            new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc),
            20000m
        );

        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        db.PaymentAllocations.Add(
            CreateAllocation(payment.Id, paidInvoice.Id, 20000m)
        );

        await db.SaveChangesAsync();

        var service = new AdminSummaryService(db);

        // Act
        var result = await service.GetSummaryAsync(2026);

        // Assert
        Assert.Single(result.UnpaidTop5);
        Assert.Equal("INV-UNPAID", result.UnpaidTop5[0].InvoiceNumber);
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
        decimal totalAmount)
    {
        var now = DateTime.UtcNow;

        return new Invoice
        {
            MemberId = memberId,
            InvoiceNumber = invoiceNumber,
            InvoiceDate = invoiceDate,
            DueDate = dueDate,
            TotalAmount = totalAmount,
            StatusId = 1,
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

    private static PaymentAllocation CreateAllocation(
        long paymentId,
        long invoiceId,
        decimal amount)
    {
        return new PaymentAllocation
        {
            PaymentId = paymentId,
            InvoiceId = invoiceId,
            Amount = amount
        };
    }
}