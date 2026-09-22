using System.Data.Common;
using InvoiceSystem.Application.Common.Interfaces;
using InvoiceSystem.Application.Services;
using InvoiceSystem.Infrastructure;
using InvoiceSystem.Infrastructure.Database;
using InvoiceSystem.Infrastructure.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

const int ExitSuccess = 0;
const int ExitNoTargets = 10;
const int ExitBusinessError = 20;
const int ExitSystemError = 50;
const int ExitUnexpectedError = 99;

var command = args.FirstOrDefault();
var externalJobId = GetOption(args, "--job-id");

if (!string.Equals(
        command,
        "reminder",
        StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine(
        "ERROR: command must be 'reminder'.");

    return ExitBusinessError;
}

if (string.IsNullOrWhiteSpace(externalJobId))
{
    Console.Error.WriteLine(
        "ERROR: required parameter '--job-id' is missing.");

    return ExitBusinessError;
}

try
{
    // 業務用CLI引数をHostへ渡すと、
    // Configuration用引数として解釈される可能性があるため、
    // argsは渡さずHostを構築する。
    var builder = Host.CreateApplicationBuilder();

    var databaseUrl =
        Environment.GetEnvironmentVariable("DATABASE_URL");

    var defaultConnection =
        builder.Configuration
            .GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(databaseUrl) &&
        string.IsNullOrWhiteSpace(defaultConnection))
    {
        Console.Error.WriteLine(
            $"ERROR ExternalJobId={externalJobId} " +
            "Database connection string is not configured.");

        return ExitSystemError;
    }

    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            options.UseNpgsql(
                PostgresConnectionStringFactory.Create(databaseUrl));
        }
        else
        {
            options.UseNpgsql(defaultConnection);
        }
    });

    // ReminderJobProcessor等の共通Infrastructureを登録する。
    // ReminderJobWorkerは登録しない。
    builder.Services.AddInfrastructureServices();

    // ReminderJobProcessorが使用するメール送信実装。
    builder.Services.AddScoped<IEmailSender, MailtrapEmailSender>();

    using var host = builder.Build();
    using var scope = host.Services.CreateScope();

    var loggerFactory =
        scope.ServiceProvider.GetRequiredService<ILoggerFactory>();

    var logger =
        loggerFactory.CreateLogger("InvoiceSystem.Batch");

    var processor =
        scope.ServiceProvider
            .GetRequiredService<IReminderJobProcessor>();

    logger.LogInformation(
        "Reminder batch started. ExternalJobId={ExternalJobId}, BatchName={BatchName}",
        externalJobId,
        "Reminder");

    var result =
        await processor.ProcessPendingAsync(
            CancellationToken.None);

    logger.LogInformation(
        """
        Reminder batch processed.
        ExternalJobId={ExternalJobId},
        TargetCount={TargetCount},
        CompletedCount={CompletedCount},
        RetryPendingCount={RetryPendingCount},
        FailedCount={FailedCount}
        """,
        externalJobId,
        result.TargetCount,
        result.CompletedCount,
        result.RetryPendingCount,
        result.FailedCount);

    if (!result.HasTargets)
    {
        logger.LogInformation(
            "Reminder batch completed with no targets. ExternalJobId={ExternalJobId}, ExitCode={ExitCode}",
            externalJobId,
            ExitNoTargets);

        return ExitNoTargets;
    }

    if (result.HasFailures)
    {
        logger.LogWarning(
            "Reminder batch completed with failures. ExternalJobId={ExternalJobId}, ExitCode={ExitCode}",
            externalJobId,
            ExitSystemError);

        return ExitSystemError;
    }

    logger.LogInformation(
        "Reminder batch completed successfully. ExternalJobId={ExternalJobId}, ExitCode={ExitCode}",
        externalJobId,
        ExitSuccess);

    return ExitSuccess;
}
catch (DbUpdateException ex)
{
    Console.Error.WriteLine(
        $"ERROR ExternalJobId={externalJobId} Database update failed: {ex.Message}");

    return ExitSystemError;
}
catch (DbException ex)
{
    Console.Error.WriteLine(
        $"ERROR ExternalJobId={externalJobId} Database access failed: {ex.Message}");

    return ExitSystemError;
}
catch (Exception ex)
{
    Console.Error.WriteLine(
        $"ERROR ExternalJobId={externalJobId} Unexpected error: {ex}");

    return ExitUnexpectedError;
}

static string? GetOption(
    string[] args,
    string optionName)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (args[i].Equals(
                optionName,
                StringComparison.OrdinalIgnoreCase))
        {
            return args[i + 1];
        }
    }

    return null;
}