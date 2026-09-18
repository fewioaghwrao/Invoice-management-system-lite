namespace InvoiceSystem.Application.Services;

public interface IReminderJobProcessor
{
    Task<ReminderJobProcessResult> ProcessPendingAsync(
        CancellationToken cancellationToken);
}