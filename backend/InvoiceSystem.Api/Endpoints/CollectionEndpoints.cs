using InvoiceSystem.Application.Dtos.Collections;
using InvoiceSystem.Application.Services;
using Microsoft.AspNetCore.Mvc;
using InvoiceSystem.Api.Common;

namespace InvoiceSystem.Api.Endpoints;

public static class CollectionEndpoints
{
    public static IEndpointRouteBuilder MapCollectionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/collections")
            .WithTags("Collections")
            .RequireAuthorization("AdminOnly");


        group.MapGet("/{invoiceId:long}/snapshot",
            async (long invoiceId, [FromServices] ICollectionService svc) =>
            {
                var dto = await svc.GetSnapshotAsync(invoiceId);
                return Results.Ok(dto);
            });

        group.MapGet("/{invoiceId:long}/logs",
            async (long invoiceId, [FromServices] ICollectionService svc) =>
            {
                var list = await svc.GetLogsAsync(invoiceId);
                return Results.Ok(list);
            });

        group.MapPost("/{invoiceId:long}/logs",
            async (
                HttpContext http,
                long invoiceId,
                [FromBody] CreateDunningLogRequestDto req,
                [FromServices] ICollectionService svc
            ) =>
            {
                var actor = AuditActorFactory.FromHttp(http);

                var id = await svc.CreateLogAsync(invoiceId, req, actor);
                return Results.Ok(new { id });
            });

        return app;
    }
}


