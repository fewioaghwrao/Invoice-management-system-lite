using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using InvoiceSystem.Application.Common.Interfaces;
using InvoiceSystem.Infrastructure;
using InvoiceSystem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InvoiceSystem.Tests.Services.Audit;

public sealed class AuditLoggerTests
{
    [Fact]
    public async Task WriteAsync_WithActor_CreatesAuditLog()
    {
        // Arrange
        await using var db = CreateDbContext();

        var logger = new AuditLogger(db);

        var actor = new AuditActor(
            UserId: 123,
            Role: "Admin",
            CorrelationId: "corr-001",
            IpAddress: "127.0.0.1",
            UserAgent: "TestAgent"
        );

        // Act
        await logger.WriteAsync(
            action: "PAYMENT_CREATED",
            entity: "Payment",
            entityId: "10",
            summary: "入金を登録しました。",
            data: new
            {
                PaymentId = 10,
                Amount = 5000m
            },
            actor: actor
        );

        // Assert
        var log = await db.AuditLogs.SingleAsync();

        Assert.Equal(123, log.ActorUserId);
        Assert.Equal("Admin", log.ActorRole);
        Assert.Equal("PAYMENT_CREATED", log.Action);
        Assert.Equal("Payment", log.Entity);
        Assert.Equal("10", log.EntityId);
        Assert.Equal("入金を登録しました。", log.Summary);

        Assert.Equal("corr-001", log.CorrelationId);
        Assert.Equal("127.0.0.1", log.IpAddress);
        Assert.Equal("TestAgent", log.UserAgent);

        Assert.NotNull(log.DataJson);
        Assert.Contains("paymentId", log.DataJson);
        Assert.Contains("amount", log.DataJson);
        Assert.DoesNotContain("PaymentId", log.DataJson);

        Assert.True(log.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task WriteAsync_WithoutActor_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var db = CreateDbContext();

        var logger = new AuditLogger(db);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => logger.WriteAsync(
                action: "TEST_ACTION",
                entity: "TestEntity",
                actor: null
            )
        );

        Assert.Equal(
            "AuditActor is required (Endpoint에서渡す方針のため).",
            ex.Message
        );

        Assert.Empty(db.AuditLogs);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
