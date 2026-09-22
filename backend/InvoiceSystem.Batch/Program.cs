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

// Host構築前のエラーも同じログ形式で出せるようにする。
using var bootstrapLoggerFactory =
    LoggerFactory.Create(ConfigureConsoleLogging);

var logger =
    bootstrapLoggerFactory.CreateLogger("InvoiceSystem.Batch");

if (!string.Equals(
        command,
        "reminder",
        StringComparison.OrdinalIgnoreCase))
{
    logger.LogError(
        "Invalid command. Command={Command}, ExitCode={ExitCode}",
        command,
        ExitBusinessError);

    return ExitBusinessError;
}

if (string.IsNullOrWhiteSpace(externalJobId))
{
    logger.LogError(
        "Required parameter is missing. Parameter={Parameter}, ExitCode={ExitCode}",
        "--job-id",
        ExitBusinessError);

    return ExitBusinessError;
}

try
{
    var builder = Host.CreateApplicationBuilder();

    ConfigureConsoleLogging(builder.Logging);

    var databaseUrl =
        Environment.GetEnvironmentVariable("DATABASE_URL");

    var defaultConnection =
        builder.Configuration
            .GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(databaseUrl) &&
        string.IsNullOrWhiteSpace(defaultConnection))
    {
        logger.LogError(
            "Database connection string is not configured. " +
            "ExternalJobId={ExternalJobId}, ExitCode={ExitCode}",
            externalJobId,
            ExitSystemError);

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

    builder.Services.AddInfrastructureServices();
    builder.Services.AddScoped<IEmailSender, MailtrapEmailSender>();

    using var host = builder.Build();
    using var scope = host.Services.CreateScope();

    // ここからはHost側Loggerを使用する。
    logger =
        scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("InvoiceSystem.Batch");

    var processor =
        scope.ServiceProvider
            .GetRequiredService<IReminderJobProcessor>();

    logger.LogInformation(
        "Reminder batch started. " +
        "ExternalJobId={ExternalJobId}, BatchName={BatchName}",
        externalJobId,
        "Reminder");

    var result =
        await processor.ProcessPendingAsync(
            CancellationToken.None);

    logger.LogInformation(
        "Reminder batch processed. " +
        "ExternalJobId={ExternalJobId}, " +
        "TargetCount={TargetCount}, " +
        "CompletedCount={CompletedCount}, " +
        "RetryPendingCount={RetryPendingCount}, " +
        "FailedCount={FailedCount}",
        externalJobId,
        result.TargetCount,
        result.CompletedCount,
        result.RetryPendingCount,
        result.FailedCount);

    if (!result.HasTargets)
    {
        logger.LogInformation(
            "Reminder batch completed with no targets. " +
            "ExternalJobId={ExternalJobId}, ExitCode={ExitCode}",
            externalJobId,
            ExitNoTargets);

        return ExitNoTargets;
    }

    if (result.HasFailures)
    {
        logger.LogWarning(
            "Reminder batch completed with failures. " +
            "ExternalJobId={ExternalJobId}, ExitCode={ExitCode}",
            externalJobId,
            ExitSystemError);

        return ExitSystemError;
    }

    logger.LogInformation(
        "Reminder batch completed successfully. " +
        "ExternalJobId={ExternalJobId}, ExitCode={ExitCode}",
        externalJobId,
        ExitSuccess);

    return ExitSuccess;
}
catch (DbUpdateException ex)
{
    logger.LogError(
        ex,
        "Database update failed. " +
        "ExternalJobId={ExternalJobId}, ExitCode={ExitCode}",
        externalJobId,
        ExitSystemError);

    return ExitSystemError;
}
catch (DbException ex)
{
    logger.LogError(
        ex,
        "Database access failed. " +
        "ExternalJobId={ExternalJobId}, ExitCode={ExitCode}",
        externalJobId,
        ExitSystemError);

    return ExitSystemError;
}
catch (Exception ex)
{
    logger.LogError(
        ex,
        "Unexpected error. " +
        "ExternalJobId={ExternalJobId}, ExitCode={ExitCode}",
        externalJobId,
        ExitUnexpectedError);

    return ExitUnexpectedError;
}

static void ConfigureConsoleLogging(
    ILoggingBuilder logging)
{
    logging.ClearProviders();

    logging.AddSimpleConsole(options =>
    {
        options.TimestampFormat =
            "yyyy-MM-dd'T'HH:mm:ss.fff'Z' ";

        options.UseUtcTimestamp = true;
        options.SingleLine = true;
    });
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