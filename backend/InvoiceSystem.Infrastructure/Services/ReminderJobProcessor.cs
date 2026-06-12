using InvoiceSystem.Application.Common.Interfaces;
using InvoiceSystem.Application.Services;
using InvoiceSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvoiceSystem.Infrastructure.Services;

public sealed class ReminderJobProcessor : IReminderJobProcessor
{
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

    public async Task ProcessPendingAsync(CancellationToken cancellationToken)
    {
        var jobs = await _db.ReminderJobs
            .Where(x => x.Status == "Pending" && x.RetryCount < 3)
            .OrderBy(x => x.CreatedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        foreach (var job in jobs)
        {
            await ProcessOneAsync(job, cancellationToken);
        }
    }

    private async Task ProcessOneAsync(ReminderJob job, CancellationToken cancellationToken)
    {
        job.Status = "Processing";
        job.StartedAt = DateTime.UtcNow;
        job.ErrorMessage = null;

        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            await _emailSender.SendAsync(
                job.ToEmail,
                job.Subject,
                job.Body);

            job.Status = "Completed";
            job.CompletedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "Reminder job completed. JobId={JobId}, InvoiceId={InvoiceId}",
                job.Id,
                job.InvoiceId);
        }
        catch (Exception ex)
        {
            job.RetryCount++;
            job.Status = job.RetryCount >= 3 ? "Failed" : "Pending";
            job.ErrorMessage = ex.Message;

            _logger.LogError(
                ex,
                "Reminder job failed. JobId={JobId}, InvoiceId={InvoiceId}, RetryCount={RetryCount}",
                job.Id,
                job.InvoiceId,
                job.RetryCount);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
