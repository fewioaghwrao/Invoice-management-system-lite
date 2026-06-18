using System;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Application.Services;
using InvoiceSystem.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace InvoiceSystem.Tests.Services.Reminders;

public sealed class ReminderJobWorkerTests
{
    [Fact]
    public async Task StartAsync_CallsProcessor()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var processor = new SpyReminderJobProcessor(() =>
        {
            cts.Cancel();
        });

        var services = new ServiceCollection();
        services.AddSingleton<IReminderJobProcessor>(processor);

        using var provider = services.BuildServiceProvider();

        var worker = new ReminderJobWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<ReminderJobWorker>.Instance
        );

        // Act
        await worker.StartAsync(cts.Token);

        // 少しだけ実行を待つ
        await Task.Delay(100);

        // StopAsync で BackgroundService を止める
        await worker.StopAsync(CancellationToken.None);

        // Assert
        Assert.True(processor.WasCalled);
    }

    [Fact]
    public async Task StartAsync_WhenProcessorThrows_DoesNotCrash()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var processor = new ThrowingReminderJobProcessor(() =>
        {
            cts.Cancel();
        });

        var services = new ServiceCollection();
        services.AddSingleton<IReminderJobProcessor>(processor);

        using var provider = services.BuildServiceProvider();

        var worker = new ReminderJobWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<ReminderJobWorker>.Instance
        );

        // Act
        await worker.StartAsync(cts.Token);

        await Task.Delay(100);

        var ex = await Record.ExceptionAsync(
            () => worker.StopAsync(CancellationToken.None)
        );

        // Assert
        Assert.Null(ex);
        Assert.True(processor.WasCalled);
    }

    private sealed class SpyReminderJobProcessor : IReminderJobProcessor
    {
        private readonly Action _onCalled;

        public bool WasCalled { get; private set; }

        public SpyReminderJobProcessor(Action onCalled)
        {
            _onCalled = onCalled;
        }

        public Task ProcessPendingAsync(CancellationToken cancellationToken)
        {
            WasCalled = true;
            _onCalled();
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingReminderJobProcessor : IReminderJobProcessor
    {
        private readonly Action _onCalled;

        public bool WasCalled { get; private set; }

        public ThrowingReminderJobProcessor(Action onCalled)
        {
            _onCalled = onCalled;
        }

        public Task ProcessPendingAsync(CancellationToken cancellationToken)
        {
            WasCalled = true;
            _onCalled();
            throw new InvalidOperationException("processor error");
        }
    }
}