using System;
using System.Linq;
using System.Threading.Tasks;
using InvoiceSystem.Domain.Entities;
using InvoiceSystem.Infrastructure;
using InvoiceSystem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InvoiceSystem.Tests.Services.Admin;

public sealed class AdminOperationLogServiceTests
{
    [Fact]
    public async Task GetRecentAsync_ReturnsLogsOrderedByCreatedAtDescThenIdDesc()
    {
        // Arrange
        await using var db = CreateDbContext();

        var sameTime = new DateTime(2026, 6, 15, 9, 0, 0, DateTimeKind.Utc);

        db.AuditLogs.AddRange(
            CreateAuditLog(1, sameTime.AddMinutes(-10), "OLD_ACTION"),
            CreateAuditLog(2, sameTime, "NEW_ACTION_LOW_ID"),
            CreateAuditLog(3, sameTime, "NEW_ACTION_HIGH_ID")
        );

        await db.SaveChangesAsync();

        var service = new AdminOperationLogService(db);

        // Act
        var result = await service.GetRecentAsync(10);

        // Assert
        Assert.Equal(3, result.Count);

        Assert.Equal(3, result[0].Id);
        Assert.Equal("NEW_ACTION_HIGH_ID", result[0].Action);

        Assert.Equal(2, result[1].Id);
        Assert.Equal("NEW_ACTION_LOW_ID", result[1].Action);

        Assert.Equal(1, result[2].Id);
        Assert.Equal("OLD_ACTION", result[2].Action);
    }

    [Fact]
    public async Task GetRecentAsync_LimitIsClampedTo50()
    {
        // Arrange
        await using var db = CreateDbContext();

        for (var i = 1; i <= 60; i++)
        {
            db.AuditLogs.Add(CreateAuditLog(
                id: i,
                createdAt: new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc).AddMinutes(i),
                action: $"ACTION_{i}"
            ));
        }

        await db.SaveChangesAsync();

        var service = new AdminOperationLogService(db);

        // Act
        var result = await service.GetRecentAsync(999);

        // Assert
        Assert.Equal(50, result.Count);
        Assert.Equal(60, result[0].Id);
        Assert.Equal(11, result[^1].Id);
    }

    [Fact]
    public async Task GetRecentAsync_LimitLessThan1_IsClampedTo1()
    {
        // Arrange
        await using var db = CreateDbContext();

        db.AuditLogs.AddRange(
            CreateAuditLog(1, new DateTime(2026, 6, 15, 9, 0, 0, DateTimeKind.Utc), "ACTION_1"),
            CreateAuditLog(2, new DateTime(2026, 6, 15, 10, 0, 0, DateTimeKind.Utc), "ACTION_2")
        );

        await db.SaveChangesAsync();

        var service = new AdminOperationLogService(db);

        // Act
        var result = await service.GetRecentAsync(0);

        // Assert
        Assert.Single(result);
        Assert.Equal(2, result[0].Id);
    }

    [Fact]
    public async Task SearchAsync_ReturnsPagedResult()
    {
        // Arrange
        await using var db = CreateDbContext();

        for (var i = 1; i <= 25; i++)
        {
            db.AuditLogs.Add(CreateAuditLog(
                id: i,
                createdAt: new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc).AddMinutes(i),
                action: $"ACTION_{i}"
            ));
        }

        await db.SaveChangesAsync();

        var service = new AdminOperationLogService(db);

        // Act
        var result = await service.SearchAsync(page: 2, pageSize: 10);

        // Assert
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(10, result.Items.Count);

        Assert.Equal(15, result.Items[0].Id);
        Assert.Equal(6, result.Items[^1].Id);
    }

    [Fact]
    public async Task SearchAsync_PageLessThan1_IsCorrectedTo1()
    {
        // Arrange
        await using var db = CreateDbContext();

        for (var i = 1; i <= 3; i++)
        {
            db.AuditLogs.Add(CreateAuditLog(
                id: i,
                createdAt: new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc).AddMinutes(i),
                action: $"ACTION_{i}"
            ));
        }

        await db.SaveChangesAsync();

        var service = new AdminOperationLogService(db);

        // Act
        var result = await service.SearchAsync(page: 0, pageSize: 10);

        // Assert
        Assert.Equal(1, result.Page);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
        Assert.Equal(3, result.Items.Count);
    }

    [Fact]
    public async Task SearchAsync_PageGreaterThanTotalPages_IsCorrectedToLastPage()
    {
        // Arrange
        await using var db = CreateDbContext();

        for (var i = 1; i <= 25; i++)
        {
            db.AuditLogs.Add(CreateAuditLog(
                id: i,
                createdAt: new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc).AddMinutes(i),
                action: $"ACTION_{i}"
            ));
        }

        await db.SaveChangesAsync();

        var service = new AdminOperationLogService(db);

        // Act
        var result = await service.SearchAsync(page: 999, pageSize: 10);

        // Assert
        Assert.Equal(3, result.Page);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(5, result.Items.Count);

        Assert.Equal(5, result.Items[0].Id);
        Assert.Equal(1, result.Items[^1].Id);
    }

    [Fact]
    public async Task SearchAsync_EmptyLogs_ReturnsPage1AndTotalPages1()
    {
        // Arrange
        await using var db = CreateDbContext();
        var service = new AdminOperationLogService(db);

        // Act
        var result = await service.SearchAsync(page: 1, pageSize: 10);

        // Assert
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task SearchAsync_PageSizeIsClampedTo100()
    {
        // Arrange
        await using var db = CreateDbContext();

        for (var i = 1; i <= 150; i++)
        {
            db.AuditLogs.Add(CreateAuditLog(
                id: i,
                createdAt: new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc).AddMinutes(i),
                action: $"ACTION_{i}"
            ));
        }

        await db.SaveChangesAsync();

        var service = new AdminOperationLogService(db);

        // Act
        var result = await service.SearchAsync(page: 1, pageSize: 999);

        // Assert
        Assert.Equal(1, result.Page);
        Assert.Equal(100, result.PageSize);
        Assert.Equal(150, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(100, result.Items.Count);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static AuditLog CreateAuditLog(
        long id,
        DateTime createdAt,
        string action,
        string entity = "Invoice",
        string? entityId = "1",
        long actorUserId = 1,
        string? summary = null)
    {
        return new AuditLog
        {
            Id = id,
            CreatedAt = createdAt,
            ActorUserId = actorUserId,
            ActorRole = "Admin",
            Action = action,
            Entity = entity,
            EntityId = entityId,
            Summary = summary,
            DataJson = null,
            CorrelationId = null,
            IpAddress = null,
            UserAgent = null
        };
    }
}