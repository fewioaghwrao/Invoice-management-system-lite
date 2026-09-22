using InvoiceSystem.Application.Common.Interfaces;
using InvoiceSystem.Application.Services;
using InvoiceSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvoiceSystem.Infrastructure.Services;

public sealed class ReminderJobProcessor : IReminderJobProcessor
{
    private static readonly TimeSpan ProcessingTimeout =
        TimeSpan.FromMinutes(10);

    private readonly AppDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ReminderJobProcessor> _logger;

    public ReminderJobProcessor(
        AppDbContext db,
        IEmailSender emailSender,
        ILogger<ReminderJobProcessor> logger)
    {
        _db = db;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<ReminderJobProcessResult> ProcessPendingAsync(
        CancellationToken cancellationToken)
    {
        // Processingのまま一定時間残留しているジョブを復旧する
        await RecoverStaleProcessingJobsAsync(cancellationToken);

        var jobs =
            await ClaimPendingJobsAsync(cancellationToken);

        var completedCount = 0;
        var retryPendingCount = 0;
        var failedCount = 0;

        foreach (var job in jobs)
        {
            await ProcessOneAsync(
                job,
                cancellationToken);

            switch (job.Status)
            {
                case "Completed":
                    completedCount++;
                    break;

                case "Pending":
                    retryPendingCount++;
                    break;

                case "Failed":
                    failedCount++;
                    break;
            }
        }

        return new ReminderJobProcessResult(
            TargetCount: jobs.Count,
            CompletedCount: completedCount,
            RetryPendingCount: retryPendingCount,
            FailedCount: failedCount);
    }

    private async Task<List<ReminderJob>> ClaimPendingJobsAsync(
    CancellationToken cancellationToken)
    {
        // 単体テストではSQLiteを使用しているため、
        // PostgreSQL固有の FOR UPDATE SKIP LOCKED は使用しない。
        if (!_db.Database.IsNpgsql())
        {
            var jobs = await _db.ReminderJobs
                .Where(x =>
                    x.Status == "Pending" &&
                    x.RetryCount < 3)
                .OrderBy(x => x.CreatedAt)
                .Take(10)
                .ToListAsync(cancellationToken);

            var startedAt = DateTime.UtcNow;

            foreach (var job in jobs)
            {
                job.Status = "Processing";
                job.StartedAt = startedAt;
                job.ErrorMessage = null;
            }

            await _db.SaveChangesAsync(cancellationToken);

            return jobs;
        }

        // PostgreSQLでは同時実行時の二重取得を防ぐため、
        // 行ロック + SKIP LOCKED で処理対象を確保する。
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                cancellationToken);

        var claimedJobs = await _db.ReminderJobs
            .FromSqlRaw(
                """
            SELECT *
            FROM "ReminderJobs"
            WHERE "Status" = 'Pending'
              AND "RetryCount" < 3
            ORDER BY "CreatedAt"
            LIMIT 10
            FOR UPDATE SKIP LOCKED
            """)
            .ToListAsync(cancellationToken);

        var claimedAt = DateTime.UtcNow;

        foreach (var job in claimedJobs)
        {
            job.Status = "Processing";
            job.StartedAt = claimedAt;
            job.ErrorMessage = null;
        }

        await _db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Reminder jobs claimed. Count={Count}",
            claimedJobs.Count);

        return claimedJobs;
    }

    private async Task RecoverStaleProcessingJobsAsync(
        CancellationToken cancellationToken)
    {
        var staleBefore =
            DateTime.UtcNow - ProcessingTimeout;

        var recoveredCount = await _db.ReminderJobs
            .Where(x =>
                x.Status == "Processing" &&
                x.StartedAt != null &&
                x.StartedAt <= staleBefore &&
                x.RetryCount < 3)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        x => x.Status,
                        "Pending")
                    .SetProperty(
                        x => x.ErrorMessage,
                        "Recovered from stale Processing state."),
                cancellationToken);

        if (recoveredCount == 0)
        {
            return;
        }

        _logger.LogWarning(
            "Recovered stale reminder jobs. Count={Count}, StaleBefore={StaleBefore}",
            recoveredCount,
            staleBefore);

        // ExecuteUpdateAsync は ChangeTracker を経由しないため、
        // 同じDbContextに残る古いReminderJobの追跡状態を破棄する。
        _db.ChangeTracker.Clear();
    }

    private async Task ProcessOneAsync(
        ReminderJob job,
        CancellationToken cancellationToken)
    {
        try
        {
            await _emailSender.SendAsync(
                job.ToEmail,
                job.Subject,
                job.Body);

            job.Status = "Completed";
            job.CompletedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "Reminder job completed. " +
                "ReminderJobId={ReminderJobId}, InvoiceId={InvoiceId}",
                job.Id,
                job.InvoiceId);
        }
        catch (Exception ex)
        {
            job.RetryCount++;

            job.Status =
                job.RetryCount >= 3
                    ? "Failed"
                    : "Pending";

            job.ErrorMessage = ex.Message;

            _logger.LogError(
                ex,
                "Reminder job failed. " +
                "ReminderJobId={ReminderJobId}, InvoiceId={InvoiceId}, RetryCount={RetryCount}",
                job.Id,
                job.InvoiceId,
                job.RetryCount);
        }

        await _db.SaveChangesAsync(
            cancellationToken);
    }
}