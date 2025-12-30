namespace InvoiceSystem.Application.Dtos.Admin;

public record AdminOperationLogDto(
    long Id,
    DateTime At,          // CreatedAt (UTC)
    long ActorUserId,
    string Action,
    string Entity,
    string? EntityId,
    string Summary
);
