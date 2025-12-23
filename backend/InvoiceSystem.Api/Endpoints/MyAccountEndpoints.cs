// Api/Endpoints/MyAccountEndpoints.cs
using InvoiceSystem.Api.Common;
using InvoiceSystem.Application.Dtos.Members;
using InvoiceSystem.Application.Services;
using InvoiceSystem.Domain.Enums;
using InvoiceSystem.Infrastructure;
using Microsoft.EntityFrameworkCore;
using static InvoiceSystem.Api.Endpoints.InvoiceEndpoints;
using InvoiceSystem.Application.Queries.Invoices;
using Microsoft.AspNetCore.Mvc;
using InvoiceSystem.Application.Dtos.Account;


namespace InvoiceSystem.Api.Endpoints;

public static class MyAccountEndpoints
{
    public static IEndpointRouteBuilder MapMyAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/members/me")
            .WithTags("MyAccount")
            .RequireAuthorization("MemberOnly");

        // 🔹 自分の会員情報取得
        group.MapGet("/", async (HttpContext http, AppDbContext db) =>
        {
            var memberId = await http.GetMemberIdAsync(db);

            var member = await db.Members
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == memberId && m.IsActive);

            if (member == null)
                return Results.NotFound();

            return Results.Ok(new MyProfileResponse(
                member.Id,
                member.Name,
                member.Email,
                member.PostalCode,
                member.Address,
                member.Phone,
                member.IsEmailConfirmed
            ));
        });


        // 🔹 自分の会員情報更新
        group.MapPut("/", async (
            HttpContext http,
            UpdateMyProfileRequest req,
            AppDbContext db) =>
        {
            var memberId = await http.GetMemberIdAsync(db);

            var member = await db.Members
                .FirstOrDefaultAsync(m => m.Id == memberId && m.IsActive);

            if (member == null)
                return Results.NotFound();

            if (string.IsNullOrWhiteSpace(req.Name))
                return Results.BadRequest("Name is required");

            if (string.IsNullOrWhiteSpace(req.Email))
                return Results.BadRequest("Email is required");

            var emailChanged = !string.Equals(
                member.Email,
                req.Email,
                StringComparison.OrdinalIgnoreCase
            );

            member.Name = req.Name.Trim();
            member.Email = req.Email.Trim();
            member.PostalCode = req.PostalCode;
            member.Address = req.Address;
            member.Phone = req.Phone;
            member.UpdatedAt = DateTime.UtcNow;

            if (emailChanged)
            {
                member.IsEmailConfirmed = false;
                member.EmailVerificationToken = null;
                member.EmailVerificationTokenExpiresAt = null;
            }

            await db.SaveChangesAsync();

            return Results.Ok(new MyProfileResponse(
                member.Id,
                member.Name,
                member.Email,
                member.PostalCode,
                member.Address,
                member.Phone,
                member.IsEmailConfirmed
            ));
        });


        // 🔹 退会（論理削除）
        group.MapDelete("/", async (HttpContext http, AppDbContext db) =>
        {
            var memberId = await http.GetMemberIdAsync(db);

            var member = await db.Members
                .FirstOrDefaultAsync(m => m.Id == memberId && m.IsActive);

            if (member == null)
                return Results.NotFound();

            member.IsActive = false;
            member.Role = MemberRole.Disabled;
            member.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return Results.NoContent();
        });
        // 🔹 自分の請求書一覧（入金状況付き）
        group.MapGet("/invoices", async (
            HttpContext http,
            AppDbContext db,
            [AsParameters] MyInvoiceSearchRequest req,
            IInvoiceService service) =>
        {
            var memberId = await http.GetMemberIdAsync(db);

            var now = DateTime.Today;
            var year = req.Year ?? now.Year;

            var result = await service.SearchMyInvoicesAsync(new MyInvoiceSearchQuery
            {
                MemberId = memberId,
                Year = year,
                Month = req.Month ?? "all",
                Status = req.Status ?? "all",
                Q = req.Q ?? "",
                Page = req.Page ?? 1,
                PageSize = req.PageSize ?? 10
            });

            // ★フロント互換に変換（issuedAt/dueAt へ）
            var dto = new AccountInvoiceListDto(
                Year: result.Year,
                AvailableYears: result.AvailableYears,
                Month: result.Month,
                Status: result.Status,
                Q: result.Q,
                Page: result.Page,
                PageSize: result.PageSize,
                TotalCount: result.TotalCount,
                Items: result.Items.Select(x => new AccountInvoiceListItemDto(
                    Id: x.Id,
                    InvoiceNumber: x.InvoiceNumber,
                    IssuedAt: x.InvoiceDate,
                    DueAt: x.DueDate,
                    TotalAmount: x.TotalAmount,
                    StatusName: x.StatusName,
                    IsOverdue: x.IsOverdue
                )).ToList()
            );

            return Results.Ok(dto);
        });

        group.MapGet("/invoices/with-balance", async (
     HttpContext http,
     AppDbContext db,
     [AsParameters] MyInvoiceSearchRequest req,
     IInvoiceService service) =>
        {
            var memberId = await http.GetMemberIdAsync(db);

            var now = DateTime.Today;
            var year = req.Year ?? now.Year;

            var result = await service.SearchMyInvoicesAsync(new MyInvoiceSearchQuery
            {
                MemberId = memberId,
                Year = year,
                Month = req.Month ?? "all",
                Status = req.Status ?? "all",
                Q = req.Q ?? "",
                Page = req.Page ?? 1,
                PageSize = req.PageSize ?? 10
            });

            // ★ PaidAmount / RemainingAmount を含む DTO（MyInvoiceListResultDto）をそのまま返す
            return Results.Ok(result);
        });

        // 🔹 自分の請求書 詳細取得
        group.MapGet("/invoices/{id:long}", async (
            long id,
            HttpContext http,
            AppDbContext db,
            IInvoiceService invoiceService) =>
        {
            var memberId = await http.GetMemberIdAsync(db);

            // ① 所有チェック（存在漏洩を避けるなら 404 が無難）
            var isMine = await db.Invoices
                .AsNoTracking()
                .AnyAsync(i => i.Id == id && i.MemberId == memberId);

            if (!isMine) return Results.NotFound();

            // ② 既存の詳細取得（Lines/Allocations/Reminders まで入る）
            var dto = await invoiceService.GetDetailByIdAsync(id);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        });

        // 🔹 自分の請求書 PDF 取得（ブラウザ表示/ダウンロード用）
        group.MapGet("/invoices/{id:long}/pdf", async (
            long id,
            HttpContext http,
            AppDbContext db,
            IInvoiceService invoiceService) =>
        {
            var memberId = await http.GetMemberIdAsync(db);

            // 所有チェック（存在漏洩防止なら 404）
            var isMine = await db.Invoices
                .AsNoTracking()
                .AnyAsync(i => i.Id == id && i.MemberId == memberId);

            if (!isMine) return Results.NotFound();

            var pdfBytes = await invoiceService.GeneratePdfAsync(id);

            // 請求書番号でファイル名を付けたい場合（任意）
            var invNo = await db.Invoices
                .AsNoTracking()
                .Where(i => i.Id == id)
                .Select(i => i.InvoiceNumber)
                .FirstAsync();

            var fileName = $"{invNo}.pdf"; // 例: INV-001.pdf

            return Results.File(pdfBytes, "application/pdf", fileName);
        });



        return app;
    }


    public class MyInvoiceSearchRequest
    {
        public int? Year { get; set; }
        public string? Month { get; set; }   // "all" or "1".."12"
        public string? Status { get; set; }  // "all" | "unpaid" | "partial" | "paid"
        public string? Q { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
    }
}
