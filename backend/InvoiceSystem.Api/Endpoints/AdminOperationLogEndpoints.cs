using InvoiceSystem.Application.Services;

namespace InvoiceSystem.Api.Endpoints;

public static class AdminOperationLogEndpoints
{
    public static IEndpointRouteBuilder MapAdminOperationLogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Admin")
            .RequireAuthorization("AdminOnly");

        // 操作ログ一覧（ページング）
        // GET /api/admin/operation-logs?page=1&pageSize=10
        group.MapGet("/operation-logs", async (
            int? page,
            int? pageSize,
            IAdminOperationLogService service,
            CancellationToken ct) =>
        {
            var result = await service.SearchAsync(
                page: page ?? 1,
                pageSize: pageSize ?? 10,
                ct: ct);

            return Results.Ok(result);
        });

        // 直近ログ（デフォルト5件）
        // GET /api/admin/operation-logs/recent?limit=5
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
