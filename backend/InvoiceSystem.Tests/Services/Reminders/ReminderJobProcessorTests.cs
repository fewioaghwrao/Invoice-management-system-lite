using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Application.Common.Interfaces;
using InvoiceSystem.Application.Services;
using InvoiceSystem.Domain.Entities;
using InvoiceSystem.Domain.Enums;
using InvoiceSystem.Infrastructure;
using InvoiceSystem.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace InvoiceSystem.Tests.Services.Reminders;

public sealed class ReminderJobProcessorTests
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
    public async Task ProcessPendingAsync_PendingJob_EmailSentAndCompleted()
    {
        // Arrange
        var (db, conn) = CreateDb();
        await using var _ = conn;

        var invoice = await CreateInvoiceAsync(db);

        db.ReminderJobs.Add(new ReminderJob
        {
            InvoiceId = invoice.Id,
            ToEmail = "customer@example.com",
            Subject = "お支払い確認",
            Body = "本文です。",
            Status = "Pending",
            RetryCount = 0,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var emailSender = new FakeEmailSender();

        var processor = new ReminderJobProcessor(
            db,
            emailSender,
            NullLogger<ReminderJobProcessor>.Instance
        );

        // Act
        var result =
            await processor.ProcessPendingAsync(CancellationToken.None);

        // Assert
        var job = await db.ReminderJobs.SingleAsync();

        Assert.Equal("Completed", job.Status);
        Assert.NotNull(job.StartedAt);
        Assert.NotNull(job.CompletedAt);
        Assert.Null(job.ErrorMessage);
        Assert.Equal(0, job.RetryCount);

        Assert.Single(emailSender.SentEmails);

        var sent = emailSender.SentEmails.Single();
        Assert.Equal("customer@example.com", sent.To);
        Assert.Equal("お支払い確認", sent.Subject);
        Assert.Equal("本文です。", sent.Body);

        // ReminderJobProcessResult
        Assert.Equal(1, result.TargetCount);
        Assert.Equal(1, result.CompletedCount);
        Assert.Equal(0, result.RetryPendingCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal(1, result.ProcessedCount);
        Assert.True(result.HasTargets);
        Assert.False(result.HasFailures);
    }

    [Fact]
    public async Task ProcessPendingAsync_EmailSendFails_FirstTime_ReturnsToPendingAndIncrementsRetryCount()
    {
        // Arrange
        var (db, conn) = CreateDb();
        await using var _ = conn;

        var invoice = await CreateInvoiceAsync(db);

        db.ReminderJobs.Add(new ReminderJob
        {
            InvoiceId = invoice.Id,
            ToEmail = "customer@example.com",
            Subject = "お支払い確認",
            Body = "本文です。",
            Status = "Pending",
            RetryCount = 0,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var emailSender = new ThrowingEmailSender("SMTP error");

        var processor = new ReminderJobProcessor(
            db,
            emailSender,
            NullLogger<ReminderJobProcessor>.Instance
        );

        // Act
        var result =
            await processor.ProcessPendingAsync(CancellationToken.None);

        // Assert
        var job = await db.ReminderJobs.SingleAsync();

        Assert.Equal("Pending", job.Status);
        Assert.Equal(1, job.RetryCount);
        Assert.Equal("SMTP error", job.ErrorMessage);
        Assert.NotNull(job.StartedAt);
        Assert.Null(job.CompletedAt);

        // ReminderJobProcessResult
        Assert.Equal(1, result.TargetCount);
        Assert.Equal(0, result.CompletedCount);
        Assert.Equal(1, result.RetryPendingCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal(1, result.ProcessedCount);
        Assert.True(result.HasTargets);
        Assert.True(result.HasFailures);
    }

    [Fact]
    public async Task ProcessPendingAsync_EmailSendFails_ThirdTime_MarksFailed()
    {
        // Arrange
        var (db, conn) = CreateDb();
        await using var _ = conn;

        var invoice = await CreateInvoiceAsync(db);

        db.ReminderJobs.Add(new ReminderJob
        {
            InvoiceId = invoice.Id,
            ToEmail = "customer@example.com",
            Subject = "お支払い確認",
            Body = "本文です。",
            Status = "Pending",
            RetryCount = 2,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var emailSender = new ThrowingEmailSender("SMTP error");

        var processor = new ReminderJobProcessor(
            db,
            emailSender,
            NullLogger<ReminderJobProcessor>.Instance
        );

        // Act
        var result =
            await processor.ProcessPendingAsync(CancellationToken.None);

        // Assert
        var job = await db.ReminderJobs.SingleAsync();

        Assert.Equal("Failed", job.Status);
        Assert.Equal(3, job.RetryCount);
        Assert.Equal("SMTP error", job.ErrorMessage);
        Assert.NotNull(job.StartedAt);
        Assert.Null(job.CompletedAt);

        // ReminderJobProcessResult
        Assert.Equal(1, result.TargetCount);
        Assert.Equal(0, result.CompletedCount);
        Assert.Equal(0, result.RetryPendingCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Equal(1, result.ProcessedCount);
        Assert.True(result.HasTargets);
        Assert.True(result.HasFailures);
    }

    [Fact]
    public async Task ProcessPendingAsync_CompletedJob_IsIgnored()
    {
        // Arrange
        var (db, conn) = CreateDb();
        await using var _ = conn;

        var invoice = await CreateInvoiceAsync(db);

        db.ReminderJobs.Add(new ReminderJob
        {
            InvoiceId = invoice.Id,
            ToEmail = "customer@example.com",
            Subject = "完了済み",
            Body = "本文です。",
            Status = "Completed",
            RetryCount = 0,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var emailSender = new FakeEmailSender();

        var processor = new ReminderJobProcessor(
            db,
            emailSender,
            NullLogger<ReminderJobProcessor>.Instance
        );

        // Act
        var result =
            await processor.ProcessPendingAsync(CancellationToken.None);

        // Assert
        var job = await db.ReminderJobs.SingleAsync();

        Assert.Equal("Completed", job.Status);
        Assert.Empty(emailSender.SentEmails);

        // ReminderJobProcessResult
        Assert.Equal(0, result.TargetCount);
        Assert.Equal(0, result.CompletedCount);
        Assert.Equal(0, result.RetryPendingCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal(0, result.ProcessedCount);
        Assert.False(result.HasTargets);
        Assert.False(result.HasFailures);
    }

    [Fact]
    public async Task ProcessPendingAsync_RetryCountThreeOrMore_IsIgnored()
    {
        // Arrange
        var (db, conn) = CreateDb();
        await using var _ = conn;

        var invoice = await CreateInvoiceAsync(db);

        db.ReminderJobs.Add(new ReminderJob
        {
            InvoiceId = invoice.Id,
            ToEmail = "customer@example.com",
            Subject = "リトライ上限",
            Body = "本文です。",
            Status = "Pending",
            RetryCount = 3,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var emailSender = new FakeEmailSender();

        var processor = new ReminderJobProcessor(
            db,
            emailSender,
            NullLogger<ReminderJobProcessor>.Instance
        );

        // Act
        var result =
            await processor.ProcessPendingAsync(CancellationToken.None);

        // Assert
        var job = await db.ReminderJobs.SingleAsync();

        Assert.Equal("Pending", job.Status);
        Assert.Equal(3, job.RetryCount);
        Assert.Empty(emailSender.SentEmails);

        // ReminderJobProcessResult
        Assert.Equal(0, result.TargetCount);
        Assert.Equal(0, result.CompletedCount);
        Assert.Equal(0, result.RetryPendingCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal(0, result.ProcessedCount);
        Assert.False(result.HasTargets);
        Assert.False(result.HasFailures);
    }

    [Fact]
    public async Task ProcessPendingAsync_ProcessesOnlyOldest10PendingJobs()
    {
        // Arrange
        var (db, conn) = CreateDb();
        await using var _ = conn;

        var invoice = await CreateInvoiceAsync(db);

        for (var i = 1; i <= 12; i++)
        {
            db.ReminderJobs.Add(new ReminderJob
            {
                InvoiceId = invoice.Id,
                ToEmail = $"customer{i}@example.com",
                Subject = $"Subject {i}",
                Body = $"Body {i}",
                Status = "Pending",
                RetryCount = 0,
                CreatedAt = new DateTime(
                    2026,
                    6,
                    15,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc
                ).AddMinutes(i)
            });
        }

        await db.SaveChangesAsync();

        var emailSender = new FakeEmailSender();

        var processor = new ReminderJobProcessor(
            db,
            emailSender,
            NullLogger<ReminderJobProcessor>.Instance
        );

        // Act
        var result =
            await processor.ProcessPendingAsync(CancellationToken.None);

        // Assert
        Assert.Equal(10, emailSender.SentEmails.Count);

        var completedCount =
            await db.ReminderJobs.CountAsync(
                x => x.Status == "Completed"
            );

        var pendingCount =
            await db.ReminderJobs.CountAsync(
                x => x.Status == "Pending"
            );

        Assert.Equal(10, completedCount);
        Assert.Equal(2, pendingCount);

        var remaining = await db.ReminderJobs
            .Where(x => x.Status == "Pending")
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();

        Assert.Equal(
            "customer11@example.com",
            remaining[0].ToEmail
        );

        Assert.Equal(
            "customer12@example.com",
            remaining[1].ToEmail
        );

        // ReminderJobProcessResult
        Assert.Equal(10, result.TargetCount);
        Assert.Equal(10, result.CompletedCount);
        Assert.Equal(0, result.RetryPendingCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal(10, result.ProcessedCount);
        Assert.True(result.HasTargets);
        Assert.False(result.HasFailures);
    }

    [Fact]
    public async Task ProcessPendingAsync_StaleProcessingJob_IsRecoveredAndCompleted()
    {
        // Arrange
        var (db, conn) = CreateDb();
        await using var _ = conn;

        var invoice = await CreateInvoiceAsync(db);

        db.ReminderJobs.Add(new ReminderJob
        {
            InvoiceId = invoice.Id,
            ToEmail = "stale-test@example.com",
            Subject = "stale processing test",
            Body = "stale processing test body",
            Status = "Processing",
            RetryCount = 0,
            CreatedAt = DateTime.UtcNow.AddHours(-1),

            // ProcessingTimeout = 10分を超えている
            StartedAt = DateTime.UtcNow.AddMinutes(-20)
        });

        await db.SaveChangesAsync();

        var emailSender = new FakeEmailSender();

        var processor = new ReminderJobProcessor(
            db,
            emailSender,
            NullLogger<ReminderJobProcessor>.Instance
        );

        // Act
        var result =
            await processor.ProcessPendingAsync(CancellationToken.None);

        // Assert
        var job = await db.ReminderJobs.SingleAsync();

        Assert.Equal("Completed", job.Status);
        Assert.Equal(0, job.RetryCount);
        Assert.NotNull(job.StartedAt);
        Assert.NotNull(job.CompletedAt);
        Assert.Null(job.ErrorMessage);

        Assert.Single(emailSender.SentEmails);

        Assert.Equal(1, result.TargetCount);
        Assert.Equal(1, result.CompletedCount);
        Assert.Equal(0, result.RetryPendingCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal(1, result.ProcessedCount);
        Assert.True(result.HasTargets);
        Assert.False(result.HasFailures);
    }

    [Fact]
    public async Task ProcessPendingAsync_RecentProcessingJob_IsNotRecovered()
    {
        // Arrange
        var (db, conn) = CreateDb();
        await using var _ = conn;

        var invoice = await CreateInvoiceAsync(db);

        db.ReminderJobs.Add(new ReminderJob
        {
            InvoiceId = invoice.Id,
            ToEmail = "recent-test@example.com",
            Subject = "recent processing test",
            Body = "recent processing test body",
            Status = "Processing",
            RetryCount = 0,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),

            // ProcessingTimeout = 10分以内
            StartedAt = DateTime.UtcNow.AddMinutes(-1)
        });

        await db.SaveChangesAsync();

        var emailSender = new FakeEmailSender();

        var processor = new ReminderJobProcessor(
            db,
            emailSender,
            NullLogger<ReminderJobProcessor>.Instance
        );

        // Act
        var result =
            await processor.ProcessPendingAsync(CancellationToken.None);

        // Assert
        var job = await db.ReminderJobs.SingleAsync();

        Assert.Equal("Processing", job.Status);
        Assert.Equal(0, job.RetryCount);
        Assert.NotNull(job.StartedAt);
        Assert.Null(job.CompletedAt);
        Assert.Null(job.ErrorMessage);

        Assert.Empty(emailSender.SentEmails);

        Assert.Equal(0, result.TargetCount);
        Assert.Equal(0, result.CompletedCount);
        Assert.Equal(0, result.RetryPendingCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal(0, result.ProcessedCount);
        Assert.False(result.HasTargets);
        Assert.False(result.HasFailures);
    }

    private static async Task<Invoice> CreateInvoiceAsync(
        AppDbContext db)
    {
        var member = new Member
        {
            Name = "Customer A",
            Email = "customer@example.com",
            PasswordHash = "dummy-hash",
            Role = MemberRole.Customer,
            IsActive = true,
            IsEmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Members.Add(member);
        await db.SaveChangesAsync();

        var invoice = new Invoice
        {
            MemberId = member.Id,
            InvoiceNumber = $"INV-{Guid.NewGuid():N}",
            InvoiceDate = DateTime.UtcNow.AddDays(-10),
            DueDate = DateTime.UtcNow.AddDays(10),
            TotalAmount = 10000m,
            StatusId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        return invoice;
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public List<SentEmail> SentEmails { get; } = new();

        public Task SendAsync(
            string to,
            string subject,
            string body)
        {
            SentEmails.Add(
                new SentEmail(to, subject, body)
            );

            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingEmailSender : IEmailSender
    {
        private readonly string _message;

        public ThrowingEmailSender(string message)
        {
            _message = message;
        }

        public Task SendAsync(
            string to,
            string subject,
            string body)
        {
            throw new InvalidOperationException(_message);
        }
    }

    private sealed record SentEmail(
        string To,
        string Subject,
        string Body);
}