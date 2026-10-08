using InvoiceSystem.Api.Utils;
using InvoiceSystem.Application.Queries.AccessExport;
using InvoiceSystem.Application.Services.AccessExport;

namespace InvoiceSystem.Api.Endpoints;

public static class AccessExportEndpoints
{
    public static IEndpointRouteBuilder MapAccessExportEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/admin/access-export")
            .WithTags("Access Export")
            .RequireAuthorization("AdminOnly");

        group.MapGet("", async (
            [AsParameters] AccessExportRequest request,
            IAccessExportService service) =>
        {
            // -----------------------------
            // Validation
            // -----------------------------
            if (request.Year is null)
            {
                return Results.BadRequest(
                    "year is required.");
            }

            if (request.Month is null)
            {
                return Results.BadRequest(
                    "month is required.");
            }

            if (request.Year is < 1 or > 9999)
            {
                return Results.BadRequest(
                    "year is invalid.");
            }

            if (request.Month is < 1 or > 12)
            {
                return Results.BadRequest(
                    "month must be between 1 and 12.");
            }

            // -----------------------------
            // Request -> Query
            // -----------------------------
            var query = new AccessExportQuery
            {
                Year = request.Year.Value,
                Month = request.Month.Value
            };

            // -----------------------------
            // DB -> DTO
            // -----------------------------
            var data = await service.ExportAsync(query);

            // -----------------------------
            // DTO -> CSV -> ZIP
            // -----------------------------
            var bytes =
                AccessExportZipBuilder.Build(data);

            var fileName =
                $"invoice-access-{query.Year}{query.Month:00}.zip";

            // -----------------------------
            // Response
            // -----------------------------
            return Results.File(
                bytes,
                "application/zip",
                fileName);
        });

        return app;
    }

    public sealed class AccessExportRequest
    {
        public int? Year { get; set; }

        public int? Month { get; set; }
    }
}
