using InvoiceSystem.Application.Commands.Invoices;
using InvoiceSystem.Application.Dtos.Invoices;
using InvoiceSystem.Application.Queries.Invoices;
using InvoiceSystem.Application.Services;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace InvoiceSystem.Api.Endpoints;

public static class InvoiceEndpoints
{
    public static IEndpointRouteBuilder MapInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/invoices")
            .WithTags("Invoices")
            .RequireAuthorization(); // ★ログイン必須

        // ---------------------------
        // AdminOnly（全件操作になり得るもの）
        // ---------------------------

        // 請求書作成（ヘッダ＋明細）※Adminのみ推奨
        group.MapPost("/", async (
            [FromBody] UpdateInvoiceRequestDto req,
            IInvoiceService service) =>
        {
            var result = await service.CreateWithLinesAsync(req);
            return Results.Created($"/api/invoices/{result.Id}", result);
        })
        .RequireAuthorization("AdminOnly");

        // 請求書検索（会員に開けると全員分検索できるので Admin のみ）
        group.MapGet("/", async (
            [AsParameters] InvoiceSearchRequest request,
            IInvoiceService service) =>
        {
            var query = new InvoiceSearchQuery
            {
                InvoiceNumber = request.InvoiceNumber,
                MemberName = request.MemberName,
                FromInvoiceDate = request.FromInvoiceDate,
                ToInvoiceDate = request.ToInvoiceDate,
                StatusId = request.StatusId,
                Page = request.Page ?? 1,
                PageSize = request.PageSize ?? 50,
            };

            var list = await service.SearchAsync(query);
            return Results.Ok(list);
        })
        .RequireAuthorization("AdminOnly");

        // 請求書詳細取得（invoiceNumber版）
        group.MapGet("/by-number/{invoiceNumber}", async (
            HttpContext ctx,
            string invoiceNumber,
            IInvoiceService service) =>
        {
            // ① invoiceNumber -> invoiceId を解決
            var invoiceId = await service.GetIdByInvoiceNumberAsync(invoiceNumber);
            if (invoiceId is null) return Results.NotFound();

            // ② 既存のガード（OwnerOrAdmin）を使い回す
            var guard = await EnsureOwnerOrAdminAsync(ctx, invoiceId.Value, service);
            if (guard is not null) return guard;

            // ③ 詳細取得
            var invoice = await service.GetDetailByIdAsync(invoiceId.Value);
            return invoice is null ? Results.NotFound() : Results.Ok(invoice);
        })
                .RequireAuthorization("AdminOnly");

        // ステータス更新（入金状況・督促など）※Adminのみ推奨
        group.MapPut("/{id:long}/status", async (
            long id,
            [FromBody] UpdateInvoiceStatusRequest request,
            IInvoiceService service) =>
        {
            var command = new UpdateInvoiceStatusCommand
            {
                InvoiceId = id,
                StatusId = request.StatusId
            };

            await service.UpdateStatusAsync(command);
            return Results.NoContent();
        })
        .RequireAuthorization("AdminOnly");

        // 請求書削除（※入金割当がある場合は 409）※Adminのみ推奨
        group.MapDelete("/{id:long}", async (long id, IInvoiceService service) =>
        {
            try
            {
                var deleted = await service.DeleteAsync(id);
                return deleted ? Results.NoContent() : Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status409Conflict);
            }
        })
        .RequireAuthorization("AdminOnly");

        // ---------------------------
        // OwnerOrAdmin（個別リソース）
        // ---------------------------

        // 請求書詳細取得（Admin or 所有者）
        group.MapGet("/{id:long}", async (HttpContext ctx, long id, IInvoiceService service) =>
        {
            var guard = await EnsureOwnerOrAdminAsync(ctx, id, service);
            if (guard is not null) return guard;

            var invoice = await service.GetDetailByIdAsync(id);
            return invoice is null ? Results.NotFound() : Results.Ok(invoice);
        });

        // 請求書 更新（ヘッダ＋明細）
        // ※会員に更新を許可しないなら、この endpoint も AdminOnly にしてOK
        group.MapPut("/{id:long}", async (
            HttpContext ctx,
            long id,
            [FromBody] UpdateInvoiceRequestDto req,
            IInvoiceService service) =>
        {
            var guard = await EnsureOwnerOrAdminAsync(ctx, id, service);
            if (guard is not null) return guard;

            await service.UpdateAsync(id, req);
            return Results.NoContent();
        });

        // 請求書PDF取得（Admin or 所有者）
        group.MapGet("/{id:long}/pdf", async (
            HttpContext ctx,
            long id,
            IInvoiceService service) =>
        {
            var guard = await EnsureOwnerOrAdminAsync(ctx, id, service);
            if (guard is not null) return guard;

            var pdfBytes = await service.GeneratePdfAsync(id);
            var fileName = $"invoice-{id}.pdf";
            return Results.File(pdfBytes, "application/pdf", fileName);
        });

        return app;
    }

    // =========================
    // Authorization helpers
    // =========================

    private static bool IsAdmin(HttpContext ctx) => ctx.User.IsInRole("Admin");

    private static long? GetMyMemberId(HttpContext ctx)
    {
        // AuthEndpoints で sub = user.Id を入れている
        var s = ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return long.TryParse(s, out var id) ? id : null;
    }

    private static async Task<IResult?> EnsureOwnerOrAdminAsync(
        HttpContext ctx,
        long invoiceId,
        IInvoiceService service)
    {
        if (IsAdmin(ctx)) return null;

        var me = GetMyMemberId(ctx);
        if (me is null) return Results.Unauthorized();

        var ownerId = await service.GetOwnerMemberIdAsync(invoiceId);
        if (ownerId is null) return Results.NotFound();

        return ownerId.Value == me.Value ? null : Results.Forbid();
    }

    /// <summary>
    /// GET /api/invoices 用のクエリパラメータ
    /// </summary>
    public class InvoiceSearchRequest
    {
        public string? InvoiceNumber { get; set; }
        public string? MemberName { get; set; }

        public DateTime? FromInvoiceDate { get; set; }
        public DateTime? ToInvoiceDate { get; set; }

        public long? StatusId { get; set; }

        public int? Page { get; set; }
        public int? PageSize { get; set; }
    }

    /// <summary>
    /// PUT /api/invoices/{id}/status 用のリクエスト
    /// </summary>
    public class UpdateInvoiceStatusRequest
    {
        public long StatusId { get; set; }
    }
}

