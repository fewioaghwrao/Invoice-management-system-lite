using InvoiceSystem.Application.Services;

namespace InvoiceSystem.Api.Endpoints;

public static class AdminOperationLogEndpoints
{
    public static IEndpointRouteBuilder MapAdminOperationLogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Admin")
            .RequireAuthorization("AdminOnly");

        // 直近ログ（デフォルト5件）
        group.MapGet("/operation-logs/recent", async (
            int? limit,
            IAdminOperationLogService service,
            CancellationToken ct) =>
        {
            var rows = await service.GetRecentAsync(limit ?? 5, ct);
            return Results.Ok(rows);
        });

        return app;
    }
}
