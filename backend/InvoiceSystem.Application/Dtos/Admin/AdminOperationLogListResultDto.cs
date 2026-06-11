namespace InvoiceSystem.Application.Dtos.Admin;

public sealed record AdminOperationLogListResultDto(
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    IReadOnlyList<AdminOperationLogDto> Items
);