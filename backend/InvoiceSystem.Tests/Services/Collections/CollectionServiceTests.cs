using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Application.Common.Interfaces;
using InvoiceSystem.Application.Dtos.Collections;
using InvoiceSystem.Domain.Entities;
using InvoiceSystem.Domain.Enums;
using InvoiceSystem.Infrastructure;
using InvoiceSystem.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PaymentEntity = InvoiceSystem.Domain.Entities.Payment;
using Xunit;

namespace InvoiceSystem.Tests.Services.Collections;

public sealed class CollectionServiceTests
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
    public async Task GetSnapshotAsync_ReturnsInvoiceSnapshotWithPaidTotal()
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
            PaymentId = payment.Id,
            InvoiceId = invoice.Id,
            Amount = 3000m
        });

        await db.SaveChangesAsync();

        var service = new CollectionService(db, new NoopAuditLogger());

        // Act
        var result = await service.GetSnapshotAsync(invoice.Id);

        // Assert
        Assert.Equal(invoice.Id, result.InvoiceId);
        Assert.Equal("INV-001", result.InvoiceNumber);
        Assert.Equal("Customer A", result.MemberName);
        Assert.Equal("customer-a@example.com", result.MemberEmail);
        Assert.Equal(10000m, result.Total);
        Assert.Equal(3000m, result.PaidTotal);
    }

    [Fact]
    public async Task GetSnapshotAsync_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var (db, conn) = CreateDb();
        await using var _ = conn;

        var service = new CollectionService(db, new NoopAuditLogger());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.GetSnapshotAsync(999)
        );

        Assert.Equal("Invoice not found", ex.Message);
    }

    [Fact]
    public async Task GetLogsAsync_ReturnsLogsOrderedByRemindedAtDescending()
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

        db.ReminderHistories.AddRange(
            new ReminderHistory
            {
                InvoiceId = invoice.Id,
                RemindedAt = new DateTime(2026, 6, 10, 9, 0, 0, DateTimeKind.Utc),
                Method = "PHONE",
                Tone = "NORMAL",
                Title = "古い催促",
                Note = "old",
                CreatedAt = DateTime.UtcNow
            },
            new ReminderHistory
            {
                InvoiceId = invoice.Id,
                RemindedAt = new DateTime(2026, 6, 12, 9, 0, 0, DateTimeKind.Utc),
                Method = "EMAIL",
                Tone = "SOFT",
                Title = "新しい催促",
                Note = "new",
                CreatedAt = DateTime.UtcNow
            }
        );

        await db.SaveChangesAsync();

        var service = new CollectionService(db, new NoopAuditLogger());

        // Act
        var result = await service.GetLogsAsync(invoice.Id);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("新しい催促", result[0].Title);
        Assert.Equal("EMAIL", result[0].Channel);
        Assert.Equal("古い催促", result[1].Title);
        Assert.Equal("PHONE", result[1].Channel);
    }

    [Fact]
    public async Task CreateLogAsync_Email_CreatesReminderHistoryAndReminderJobAndUpdatesStatusToDunning()
    {
        // Arrange
        var (db, conn) = CreateDb();
        await using var _ = conn;

        AddDunningStatus(db);

        var member = CreateMember("Customer A", "customer-a@example.com");
        db.Members.Add(member);
        await db.SaveChangesAsync();

        var invoice = CreateInvoice(
            member.Id,
            "INV-001",
            DateTime.UtcNow.AddDays(-10),
            DateTime.UtcNow.AddDays(10),
            10000m,
            statusId: 1 // UNPAID
        );

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        var service = new CollectionService(db, new NoopAuditLogger());

        var req = new CreateDunningLogRequestDto
        {
            Channel = "EMAIL",
            Tone = "SOFT",
            Title = "お支払い確認",
            Memo = "ご確認お願いします。",
            Subject = "お支払い状況のご確認",
            BodyText = "本文です。",
            NextActionDate = DateTime.UtcNow.AddDays(3)
        };

        // Act
        var id = await service.CreateLogAsync(invoice.Id, req);

        // Assert
        Assert.True(id > 0);

        var history = await db.ReminderHistories.SingleAsync();
        Assert.Equal(invoice.Id, history.InvoiceId);
        Assert.Equal("EMAIL", history.Method);
        Assert.Equal("SOFT", history.Tone);
        Assert.Equal("お支払い確認", history.Title);
        Assert.Equal("ご確認お願いします。", history.Note);
        Assert.Equal("お支払い状況のご確認", history.Subject);
        Assert.Equal("本文です。", history.BodyText);
        Assert.NotNull(history.NextActionDate);

        var job = await db.ReminderJobs.SingleAsync();
        Assert.Equal(invoice.Id, job.InvoiceId);
        Assert.Equal("customer-a@example.com", job.ToEmail);
        Assert.Equal("お支払い状況のご確認", job.Subject);
        Assert.Equal("本文です。", job.Body);
        Assert.Equal("Pending", job.Status);
        Assert.Equal(0, job.RetryCount);

        var updatedInvoice = await db.Invoices.AsNoTracking()
            .SingleAsync(x => x.Id == invoice.Id);

        Assert.Equal(6, updatedInvoice.StatusId); // DUNNING
    }

    [Fact]
    public async Task CreateLogAsync_Phone_CreatesReminderHistoryButDoesNotCreateReminderJob()
    {
        // Arrange
        var (db, conn) = CreateDb();
        await using var _ = conn;

        AddDunningStatus(db);

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

        var service = new CollectionService(db, new NoopAuditLogger());

        var req = new CreateDunningLogRequestDto
        {
            Channel = "PHONE",
            Tone = "NORMAL",
            Title = "電話催促",
            Memo = "電話しました。"
        };

        // Act
        var id = await service.CreateLogAsync(invoice.Id, req);

        // Assert
        Assert.True(id > 0);
        Assert.Single(db.ReminderHistories);
        Assert.Empty(db.ReminderJobs);
    }

    [Fact]
    public async Task CreateLogAsync_EmailWithoutMemberEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        var (db, conn) = CreateDb();
        await using var _ = conn;

        AddDunningStatus(db);

        var member = CreateMember("Customer A", "");
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

        var service = new CollectionService(db, new NoopAuditLogger());

        var req = new CreateDunningLogRequestDto
        {
            Channel = "EMAIL",
            Tone = "SOFT",
            Title = "メール催促",
            Memo = "本文"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateLogAsync(invoice.Id, req)
        );

        Assert.Equal("Member email is not set.", ex.Message);
    }

    [Fact]
    public async Task CreateLogAsync_WhenDunningStatusMissing_ThrowsInvalidOperationException()
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
            statusId: 1 // UNPAID
        );

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        var service = new CollectionService(db, new NoopAuditLogger());

        var req = new CreateDunningLogRequestDto
        {
            Channel = "PHONE",
            Tone = "NORMAL",
            Title = "電話催促",
            Memo = "電話しました。"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateLogAsync(invoice.Id, req)
        );

        Assert.Equal(
            "InvoiceStatus 'DUNNING' is not found. Seed InvoiceStatuses first.",
            ex.Message
        );
    }

    [Fact]
    public async Task CreateLogAsync_WithActor_WritesAuditLog()
    {
        // Arrange
        var (db, conn) = CreateDb();
        await using var _ = conn;

        AddDunningStatus(db);

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

        var audit = new SpyAuditLogger();
        var service = new CollectionService(db, audit);

        var req = new CreateDunningLogRequestDto
        {
            Channel = "PHONE",
            Tone = "NORMAL",
            Title = "電話催促",
            Memo = "電話しました。"
        };

        var actor = new AuditActor(
            UserId: 1,
            Role: "Admin",
            CorrelationId: "corr-001",
            IpAddress: "127.0.0.1",
            UserAgent: "TestAgent"
        );

        // Act
        var id = await service.CreateLogAsync(invoice.Id, req, actor);

        // Assert
        Assert.True(id > 0);

        Assert.True(audit.WasCalled);
        Assert.Equal("DUNNING_LOG_CREATED", audit.Action);
        Assert.Equal("Invoice", audit.Entity);
        Assert.Equal(invoice.Id.ToString(), audit.EntityId);
        Assert.Equal(actor, audit.Actor);
    }

    private static void AddDunningStatus(AppDbContext db)
    {
        db.InvoiceStatuses.Add(new InvoiceStatus
        {
            Id = 6,
            Code = "DUNNING",
            Name = "催促中",
            IsOverdue = false,
            IsClosed = false,
            SortOrder = 60
        });

        db.SaveChanges();
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

    private sealed class SpyAuditLogger : IAuditLogger
    {
        public bool WasCalled { get; private set; }
        public string? Action { get; private set; }
        public string? Entity { get; private set; }
        public string? EntityId { get; private set; }
        public AuditActor? Actor { get; private set; }

        public Task WriteAsync(
            string action,
            string entity,
            string? entityId = null,
            string? summary = null,
            object? data = null,
            AuditActor? actor = null,
            CancellationToken ct = default)
        {
            WasCalled = true;
            Action = action;
            Entity = entity;
            EntityId = entityId;
            Actor = actor;
            return Task.CompletedTask;
        }
    }
}