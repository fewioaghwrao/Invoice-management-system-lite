using InvoiceSystem.Api.Common;
using InvoiceSystem.Application.Common.Interfaces;
using InvoiceSystem.Application.Dtos.Payments;
using InvoiceSystem.Application.Queries.Payments;
using InvoiceSystem.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Api.Endpoints;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payments")
            .WithTags("Payments")
            .RequireAuthorization("AdminOnly");

        // 入金一覧（検索/ページング/サマリー）
        group.MapGet("", async (
            [AsParameters] PaymentSearchRequest request,
            IPaymentService service) =>
        {
            int? month = null;
            if (!string.IsNullOrWhiteSpace(request.Month) && request.Month != "all")
            {
                if (int.TryParse(request.Month, out var mv) && mv is >= 1 and <= 12)
                    month = mv;
            }

            var status = string.IsNullOrWhiteSpace(request.Status)
                ? "all"
                : request.Status.Trim().ToUpperInvariant();

            status = status switch
            {
                "UNALLOCATED" => "UNALLOCATED",
                "PARTIAL" => "PARTIAL",
                "ALLOCATED" => "ALLOCATED",
                "ALL" => "all",
                _ => "all"
            };

            var query = new PaymentSearchQuery
            {
                Year = request.Year ?? DateTime.Today.Year,
                Month = month,
                Keyword = request.Q ?? "",
                Status = status == "ALL" ? "all" : status,
                Page = request.Page ?? 1,
                PageSize = request.PageSize ?? 10,
            };

            var result = await service.SearchAsync(query);
            return Results.Ok(result);
        });

        // 入金詳細（割当込み）
        group.MapGet("/{id:long}", async (long id, IPaymentService service) =>
        {
            var dto = await service.GetByIdAsync(id);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        });

        // 入金登録（手動）
        group.MapPost("", async (
            [FromBody] CreatePaymentRequestDto request,
            IPaymentService service) =>
        {
            if (request.MemberId <= 0) return Results.BadRequest("MemberId is required.");
            if (request.Amount <= 0) return Results.BadRequest("Amount must be > 0.");
            if (request.PaymentDate == default) return Results.BadRequest("PaymentDate is required.");

            var id = await service.CreateAsync(request);
            return Results.Created($"/api/payments/{id}", new { id });
        });

        // =========================
        // ★ 割当追加（1件）: actorを渡す
        // =========================
        group.MapPost("/{id:long}/allocations", async (
            long id,
            [FromBody] AllocationLine request,
            IPaymentService service,
            HttpContext http) =>
        {
            if (request.InvoiceId <= 0) return Results.BadRequest("InvoiceId is required.");
            if (request.Amount <= 0) return Results.BadRequest("Amount must be > 0.");

            var actor = AuditActorFactory.FromHttp(http);

            var allocId = await service.AddAllocationAsync(
                paymentId: id,
                invoiceId: request.InvoiceId,
                amount: request.Amount,
                actor: actor);

            return Results.Created($"/api/payments/{id}", new { allocationId = allocId });
        });

        // =========================
        // ★ 割当削除（1件）: actorを渡す
        // =========================
        group.MapDelete("/{id:long}/allocations/{allocId:long}", async (
            long id,
            long allocId,
            IPaymentService service,
            HttpContext http) =>
        {
            var actor = AuditActorFactory.FromHttp(http);

            await service.DeleteAllocationAsync(
                paymentId: id,
                allocationId: allocId,
                actor: actor);

            return Results.NoContent();
        });

        // =========================
        // ★ 割当保存（InvoiceId版）: actorを渡す
        // =========================
        group.MapPut("/{id:long}/allocations", async (
            long id,
            [FromBody] SavePaymentAllocationsRequest request,
            IPaymentService service,
            HttpContext http) =>
        {
            var lines = (request.Lines ?? new List<AllocationLine>())
                .Where(x => x.Amount > 0)
                .Select(x => new SaveAllocationLine(x.InvoiceId, x.Amount))
                .ToList();

            var actor = AuditActorFactory.FromHttp(http);

            await service.SaveAllocationsAsync(
                paymentId: id,
                lines: lines,
                actor: actor);

            return Results.NoContent();
        });

        return app;
    }

    public class PaymentSearchRequest
    {
        public int? Year { get; set; }
        public string? Month { get; set; }   // "all" or "1".."12"
        public string? Q { get; set; }       // keyword
        public string? Status { get; set; }  // all/unallocated/partial/allocated
        public int? Page { get; set; }
        public int? PageSize { get; set; }
    }

    public record SavePaymentAllocationsRequest(List<AllocationLine> Lines);
    public record AllocationLine(long InvoiceId, decimal Amount);
}

