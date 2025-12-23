using InvoiceSystem.Application.Common.Interfaces;

namespace InvoiceSystem.Api.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Admin")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/summary", async (int? year, IAdminSummaryService service, CancellationToken ct) =>
        {
            var y = year ?? DateTime.Today.Year;
            var dto = await service.GetSummaryAsync(y, ct);
            return Results.Ok(dto);
        });

        return app;
    }
}
