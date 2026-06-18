namespace InvoiceSystem.Application.Services;

public interface IReminderJobProcessor
{
    Task ProcessPendingAsync(CancellationToken cancellationToken);
}