namespace InvoiceSystem.Application.Services;

public sealed record ReminderJobProcessResult(
    int TargetCount,
    int CompletedCount,
    int RetryPendingCount,
    int FailedCount)
{
    public int ProcessedCount =>
        CompletedCount + RetryPendingCount + FailedCount;

    public bool HasTargets =>
        TargetCount > 0;

    public bool HasFailures =>
        RetryPendingCount > 0 || FailedCount > 0;
}