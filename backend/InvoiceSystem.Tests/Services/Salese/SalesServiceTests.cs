using InvoiceSystem.Application.Queries.Sales;
using InvoiceSystem.Application.Services.Sales;
using InvoiceSystem.Domain.Entities;
using InvoiceSystem.Domain.Enums;
using InvoiceSystem.Infrastructure;
using InvoiceSystem.Infrastructure.Services.Sales;
using Microsoft.EntityFrameworkCore;
using PaymentEntity = InvoiceSystem.Domain.Entities.Payment;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace InvoiceSystem.Tests.Services.Sales;

public sealed class SalesServiceTests
{
    [Fact]
    public async Task SearchAsync_ReturnsSummaryAndRows()
    {
        // Arrange
        await using var db = CreateDbContext();

        var customer = CreateMember(1, "Customer A");

        var invoice1 = CreateInvoice(
            id: 1,
            memberId: customer.Id,
            invoiceNumber: "INV-001",
            invoiceDate: new DateTime(2026, 6, 1),
            dueDate: new DateTime(2026, 6, 30),
            totalAmount: 10000m
        );

        var invoice2 = CreateInvoice(
            id: 2,
            memberId: customer.Id,
            invoiceNumber: "INV-002",
            invoiceDate: new DateTime(2026, 6, 2),
            dueDate: new DateTime(2026, 6, 30),
            totalAmount: 20000m
        );

        var payment = CreatePayment(
            id: 1,
            memberId: customer.Id,
            paymentDate: new DateTime(2026, 6, 10),
            amount: 5000m
        );

        var allocation = CreatePaymentAllocation(
            id: 1,
            paymentId: payment.Id,
            invoiceId: invoice1.Id,
            amount: 5000m
        );

        db.Members.Add(customer);
        db.Invoices.AddRange(invoice1, invoice2);
        db.Payments.Add(payment);
        db.PaymentAllocations.Add(allocation);

        await db.SaveChangesAsync();

        var service = new SalesService(db);

        var req = new SalesSearchRequest
        {
            Year = 2026,
            Month = 6,
            Status = "all",
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await service.SearchAsync(req);

        // Assert
        Assert.Equal(2026, result.Year);
        Assert.Equal("6", result.Month);
        Assert.Equal("all", result.Status);
        Assert.Equal(2, result.TotalCount);

        Assert.Equal(30000m, result.Summary.InvoiceTotal);
        Assert.Equal(5000m, result.Summary.PaidTotal);
        Assert.Equal(25000m, result.Summary.RemainingTotal);
        Assert.Equal(16.7m, result.Summary.RecoveryRate);

        Assert.Equal(2, result.Rows.Count);

        var newest = result.Rows[0];
        Assert.Equal("INV-002", newest.InvoiceNumber);
        Assert.Equal("UNPAID", newest.Status);
        Assert.Equal(20000m, newest.InvoiceAmount);
        Assert.Equal(0m, newest.PaidAmount);
        Assert.Equal(20000m, newest.RemainingAmount);

        var older = result.Rows[1];
        Assert.Equal("INV-001", older.InvoiceNumber);
        Assert.Equal("PARTIAL", older.Status);
        Assert.Equal(10000m, older.InvoiceAmount);
        Assert.Equal(5000m, older.PaidAmount);
        Assert.Equal(5000m, older.RemainingAmount);
        Assert.Equal(new DateTime(2026, 6, 10), older.LastPaidAt);
    }

    [Fact]
    public async Task SearchAsync_StatusPaid_ReturnsOnlyPaidInvoices()
    {
        // Arrange
        await using var db = CreateDbContext();

        var customer = CreateMember(1, "Customer A");

        var unpaidInvoice = CreateInvoice(1, customer.Id, "INV-UNPAID", new DateTime(2026, 6, 1), new DateTime(2026, 6, 30), 10000m);
        var partialInvoice = CreateInvoice(2, customer.Id, "INV-PARTIAL", new DateTime(2026, 6, 2), new DateTime(2026, 6, 30), 10000m);
        var paidInvoice = CreateInvoice(3, customer.Id, "INV-PAID", new DateTime(2026, 6, 3), new DateTime(2026, 6, 30), 10000m);

        var payment1 = CreatePayment(1, customer.Id, new DateTime(2026, 6, 10), 5000m);
        var payment2 = CreatePayment(2, customer.Id, new DateTime(2026, 6, 11), 10000m);

        db.Members.Add(customer);
        db.Invoices.AddRange(unpaidInvoice, partialInvoice, paidInvoice);
        db.Payments.AddRange(payment1, payment2);
        db.PaymentAllocations.AddRange(
            CreatePaymentAllocation(1, payment1.Id, partialInvoice.Id, 5000m),
            CreatePaymentAllocation(2, payment2.Id, paidInvoice.Id, 10000m)
        );

        await db.SaveChangesAsync();

        var service = new SalesService(db);

        var req = new SalesSearchRequest
        {
            Year = 2026,
            Month = 6,
            Status = "paid",
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await service.SearchAsync(req);

        // Assert
        Assert.Single(result.Rows);
        Assert.Equal("INV-PAID", result.Rows[0].InvoiceNumber);
        Assert.Equal("PAID", result.Rows[0].Status);

        Assert.Equal(10000m, result.Summary.InvoiceTotal);
        Assert.Equal(10000m, result.Summary.PaidTotal);
        Assert.Equal(0m, result.Summary.RemainingTotal);
        Assert.Equal(100m, result.Summary.RecoveryRate);
    }

    [Fact]
    public async Task SearchAsync_MemberId_ReturnsOnlyTargetMemberAndMemberName()
    {
        // Arrange
        await using var db = CreateDbContext();

        var memberA = CreateMember(1, "Customer A");
        var memberB = CreateMember(2, "Customer B");

        db.Members.AddRange(memberA, memberB);
        db.Invoices.AddRange(
            CreateInvoice(1, memberA.Id, "INV-A", new DateTime(2026, 6, 1), new DateTime(2026, 6, 30), 10000m),
            CreateInvoice(2, memberB.Id, "INV-B", new DateTime(2026, 6, 1), new DateTime(2026, 6, 30), 20000m)
        );

        await db.SaveChangesAsync();

        var service = new SalesService(db);

        var req = new SalesSearchRequest
        {
            Year = 2026,
            Month = 6,
            MemberId = memberB.Id,
            Status = "all",
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await service.SearchAsync(req);

        // Assert
        Assert.Equal("Customer B", result.MemberName);
        Assert.Single(result.Rows);
        Assert.Equal("INV-B", result.Rows[0].InvoiceNumber);
        Assert.Equal(20000m, result.Summary.InvoiceTotal);
    }

    [Fact]
    public async Task SearchByMemberAsync_AggregatesByMember()
    {
        // Arrange
        await using var db = CreateDbContext();

        var memberA = CreateMember(1, "Customer A");
        var memberB = CreateMember(2, "Customer B");

        var invoiceA1 = CreateInvoice(1, memberA.Id, "INV-A1", new DateTime(2026, 6, 1), new DateTime(2026, 6, 30), 10000m);
        var invoiceA2 = CreateInvoice(2, memberA.Id, "INV-A2", new DateTime(2026, 6, 2), new DateTime(2026, 6, 30), 20000m);
        var invoiceB1 = CreateInvoice(3, memberB.Id, "INV-B1", new DateTime(2026, 6, 3), new DateTime(2026, 6, 30), 5000m);

        var payment = CreatePayment(1, memberA.Id, new DateTime(2026, 6, 10), 15000m);

        db.Members.AddRange(memberA, memberB);
        db.Invoices.AddRange(invoiceA1, invoiceA2, invoiceB1);
        db.Payments.Add(payment);
        db.PaymentAllocations.AddRange(
            CreatePaymentAllocation(1, payment.Id, invoiceA1.Id, 10000m),
            CreatePaymentAllocation(2, payment.Id, invoiceA2.Id, 5000m)
        );

        await db.SaveChangesAsync();

        var service = new SalesService(db);

        var req = new SalesSearchRequest
        {
            Year = 2026,
            Month = 6,
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await service.SearchByMemberAsync(req);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(35000m, result.Summary.InvoiceTotal);
        Assert.Equal(15000m, result.Summary.PaidTotal);
        Assert.Equal(20000m, result.Summary.RemainingTotal);
        Assert.Equal(42.9m, result.Summary.RecoveryRate);

        var customerA = result.Rows.Single(x => x.MemberName == "Customer A");
        Assert.Equal(30000m, customerA.InvoiceTotal);
        Assert.Equal(15000m, customerA.PaidTotal);
        Assert.Equal(15000m, customerA.RemainingTotal);
        Assert.Equal(50m, customerA.RecoveryRate);

        var customerB = result.Rows.Single(x => x.MemberName == "Customer B");
        Assert.Equal(5000m, customerB.InvoiceTotal);
        Assert.Equal(0m, customerB.PaidTotal);
        Assert.Equal(5000m, customerB.RemainingTotal);
        Assert.Equal(0m, customerB.RecoveryRate);
    }

    [Fact]
    public async Task ExportAsync_ReturnsRowsForCsv()
    {
        // Arrange
        await using var db = CreateDbContext();

        var customer = CreateMember(1, "Customer A");
        var status = CreateInvoiceStatus(1, "UNPAID", "未入金");

        var invoice = CreateInvoice(
            id: 1,
            memberId: customer.Id,
            invoiceNumber: "INV-001",
            invoiceDate: new DateTime(2026, 6, 1),
            dueDate: new DateTime(2026, 6, 30),
            totalAmount: 10000m,
            statusId: status.Id
        );

        var payment = CreatePayment(1, customer.Id, new DateTime(2026, 6, 10), 3000m);
        var allocation = CreatePaymentAllocation(1, payment.Id, invoice.Id, 3000m);

        db.Members.Add(customer);
        db.InvoiceStatuses.Add(status);
        db.Invoices.Add(invoice);
        db.Payments.Add(payment);
        db.PaymentAllocations.Add(allocation);

        await db.SaveChangesAsync();

        var service = new SalesService(db);

        var query = new SalesSearchQuery
        {
            Year = 2026,
            Month = 6,
            Status = "all",
            Keyword = null,
            MemberId = null
        };

        // Act
        var rows = await service.ExportAsync(query);

        // Assert
        Assert.Single(rows);

        var row = rows.Single();

        Assert.Equal(1, row.InvoiceId);
        Assert.Equal("INV-001", row.InvoiceNumber);
        Assert.Equal("Customer A", row.ClientName);
        Assert.Equal("UNPAID", row.Status);
        Assert.Equal(10000m, row.InvoiceAmount);
        Assert.Equal(3000m, row.PaidAmount);
        Assert.Equal(7000m, row.RemainingAmount);
        Assert.Equal(new DateTime(2026, 6, 10), row.LastPaidAt);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static Member CreateMember(long id, string name)
    {
        var now = DateTime.UtcNow;

        return new Member
        {
            Id = id,
            Name = name,
            Email = $"{name.Replace(" ", "").ToLowerInvariant()}@example.com",
            PasswordHash = "hash",
            Role = MemberRole.Customer,
            IsActive = true,
            IsEmailConfirmed = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static InvoiceStatus CreateInvoiceStatus(long id, string code, string name)
    {
        return new InvoiceStatus
        {
            Id = id,
            Code = code,
            Name = name
        };
    }

    private static Invoice CreateInvoice(
        long id,
        long memberId,
        string invoiceNumber,
        DateTime invoiceDate,
        DateTime dueDate,
        decimal totalAmount,
        long? statusId = null)
    {
        return new Invoice
        {
            Id = id,
            MemberId = memberId,
            InvoiceNumber = invoiceNumber,
            InvoiceDate = invoiceDate,
            DueDate = dueDate,
            TotalAmount = totalAmount,
            StatusId = statusId ?? 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static PaymentEntity CreatePayment(
        long id,
        long memberId,
        DateTime paymentDate,
        decimal amount)
    {
        return new PaymentEntity
        {
            Id = id,
            MemberId = memberId,
            PaymentDate = paymentDate,
            Amount = amount
        };
    }

    private static PaymentAllocation CreatePaymentAllocation(
        long id,
        long paymentId,
        long invoiceId,
        decimal amount)
    {
        return new PaymentAllocation
        {
            Id = id,
            PaymentId = paymentId,
            InvoiceId = invoiceId,
            Amount = amount,
        };
    }
}