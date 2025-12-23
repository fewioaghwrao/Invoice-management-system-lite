using InvoiceSystem.Application.Commands.Invoices;
using InvoiceSystem.Application.Dtos.Invoices;
using InvoiceSystem.Application.Queries.Invoices;
using InvoiceSystem.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Api.Endpoints;

public static class InvoiceEndpoints
{
    public static IEndpointRouteBuilder MapInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/invoices")
            .WithTags("Invoices");


         // 請求書作成（ヘッダ＋明細）
        group.MapPost("/", async(
         [FromBody] UpdateInvoiceRequestDto req,
        IInvoiceService service) =>
        { 
            var result = await service.CreateWithLinesAsync(req);
                 return Results.Created($"/api/invoices/{result.Id}", result);
             });

        // 請求書詳細取得
        group.MapGet("/{id:long}", async (long id, IInvoiceService service) =>
        {
            var invoice = await service.GetDetailByIdAsync(id);
            return invoice is null ? Results.NotFound() : Results.Ok(invoice);
        });

        // 請求書検索
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
                Page = request.Page ?? 1,   // ← ここで 1 を補完
                PageSize = request.PageSize ?? 50,  // ← ここで 50 を補完
            };

            var list = await service.SearchAsync(query);
            return Results.Ok(list);
        });

        // ステータス更新（入金状況・督促など）
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
        });

        // 請求書削除（※入金割当がある場合は 409）
        group.MapDelete("/{id:long}", async (long id, IInvoiceService service) =>
        {
            try
            {
                var deleted = await service.DeleteAsync(id);
                return deleted ? Results.NoContent() : Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                // 例：入金割当があるので削除できない
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status409Conflict);
            }
        });

        // 請求書 更新（ヘッダ＋明細）
        group.MapPut("/{id:long}", async (
            long id,
            [FromBody] UpdateInvoiceRequestDto req,
            IInvoiceService service) =>
        {
            await service.UpdateAsync(id, req);   // ← InvoiceService に追加した UpdateAsync を呼ぶ
            return Results.NoContent();
        });

        // 請求書PDF取得
        group.MapGet("/{id:long}/pdf", async (
            long id,
            IInvoiceService service) =>
        {
            var pdfBytes = await service.GeneratePdfAsync(id);

            // 仮のファイル名（本番は請求書番号などから生成してOK）
            var fileName = $"invoice-{id}.pdf";

            // 中身はダミーでも、ContentType は application/pdf にしておく
            return Results.File(pdfBytes, "application/pdf", fileName);
        });

        return app;
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

        public int? Page { get; set; }      // ← nullable
        public int? PageSize { get; set; }  // ← nullable
    }

    /// <summary>
    /// PUT /api/invoices/{id}/status 用のリクエスト
    /// </summary>
    public class UpdateInvoiceStatusRequest
    {
        public long StatusId { get; set; }
    }
}

