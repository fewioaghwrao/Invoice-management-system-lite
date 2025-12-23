using InvoiceSystem.Api.Endpoints.Sales;
using InvoiceSystem.Application.Queries.Sales;
using InvoiceSystem.Application.Services.Sales;
using System.Text;
using InvoiceSystem.Api.Utils;

namespace InvoiceSystem.Api.Endpoints;

public static class SalesEndpoints
{
    public static IEndpointRouteBuilder MapSalesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sales")
            .WithTags("Sales")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/", async ([AsParameters] SalesSearchRequest request, ISalesService service) =>
        {
            var result = await service.SearchAsync(request);
            return Results.Ok(result);
        });

        group.MapGet("/by-member",
    async ([AsParameters] SalesSearchRequest request, ISalesService service) =>
    {
        var result = await service.SearchByMemberAsync(request);
        return Results.Ok(result);
    });

        // ★CSV Export
        group.MapGet("/export", async (
            [AsParameters] SalesExportRequest request,
            ISalesService service) =>
        {
            var year = request.Year ?? DateTime.Today.Year;

            var query = new SalesSearchQuery
            {
                Year = year,
                Month = request.Month,
                Keyword = request.Q,
                Status = string.IsNullOrWhiteSpace(request.Status) ? "all" : request.Status!,
                MemberId = request.MemberId,
            };

            var rows = await service.ExportAsync(query);
            var csv = SalesCsvBuilder.Build(rows);

            var bom = Encoding.UTF8.GetPreamble();
            var body = Encoding.UTF8.GetBytes(csv);
            var bytes = bom.Concat(body).ToArray();

            var ym = request.Month is null ? $"{year}" : $"{year}-{request.Month:00}";
            var fileName = $"sales_{ym}.csv";

            return Results.File(bytes, "text/csv; charset=utf-8", fileName);
        });

        // ★by-member CSV Export（顧客別集計そのものをCSV）
        group.MapGet("/by-member/export", async (
            [AsParameters] SalesSearchRequest request,
            ISalesService service) =>
        {
            var result = await service.SearchByMemberAsync(request);

            // ★Summary を渡す
            var csv = SalesByMemberCsvBuilder.Build(
                result.Rows,
                result.Summary
            );

            var bom = Encoding.UTF8.GetPreamble();
            var body = Encoding.UTF8.GetBytes(csv);
            var bytes = bom.Concat(body).ToArray();

            var year = request.Year ?? DateTime.Today.Year;
            var ym = request.Month is null ? $"{year}" : $"{year}-{request.Month:00}";
            var fileName = $"sales_by_member_{ym}.csv";

            return Results.File(bytes, "text/csv; charset=utf-8", fileName);
        });

        // ★回収率ワースト顧客 TOP5
        group.MapGet("/by-member/worst-top5", async (
            [AsParameters] SalesSearchRequest request,
            ISalesService service) =>
        {
            // ※SearchByMemberAsync はページング前提なので、ここでは “十分大きいPageSize” で取り切る
            // （Lite版としてはこれでOK。将来は専用Query化してDB側でTopNに寄せる）
            var reqAll = new SalesSearchRequest
            {
                Year = request.Year,
                Month = request.Month,
                Q = request.Q,
                Page = 1,
                PageSize = 100000, // ここは上限。必要なら調整
                                   // Status/MemberId は by-member 集計では使っていない想定（使うならここにもコピー）
            };

            var result = await service.SearchByMemberAsync(reqAll);

            var worst = result.Rows
                .Where(r => r.RemainingTotal > 0)                 // ★未回収がある顧客だけ
                .OrderBy(r => r.RecoveryRate)                      // ★回収率が低い順
                .ThenByDescending(r => r.RemainingTotal)           // ★同率なら未回収が大きい順
                .Take(5)
                .Select(r => new WorstCustomerDto(
                    r.MemberId,
                    r.MemberName,
                    r.InvoiceTotal,
                    r.PaidTotal,
                    r.RemainingTotal,
                    r.RecoveryRate
                ))
                .ToList();

            var dto = new WorstCustomersResultDto(
                result.Year,
                result.Month,
                result.Keyword,
                worst.Count,
                worst
            );

            return Results.Ok(dto);
        });

        return app;
    }
}

