// Api/Endpoints/MemberEndpoints.cs
using InvoiceSystem.Application.Queries.Members;
using InvoiceSystem.Application.Services;
using InvoiceSystem.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using InvoiceSystem.Application.Commands.Members;
using InvoiceSystem.Application.Dtos.Members; // ★追加
using Microsoft.EntityFrameworkCore;
using InvoiceSystem.Infrastructure;
using InvoiceSystem.Domain.Entities;

namespace InvoiceSystem.Api.Endpoints
{
    public static class MemberEndpoints
    {
        public static IEndpointRouteBuilder MapMemberEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/members")
                .WithTags("Members")
                .RequireAuthorization("AdminOnly");

            // 会員検索 + 一覧
            group.MapGet("/", async (
                [AsParameters] MemberSearchRequest request,
                IMemberService service) =>
            {
                var query = new MemberSearchQuery
                {
                    Keyword = request.Keyword,
                    Role = request.RoleId.HasValue
                        ? (MemberRole?)request.RoleId.Value
                        : null,
                    IsActive = request.IsActive,
                    Page = request.Page,
                    PageSize = request.PageSize
                };

                var list = await service.SearchAsync(query);
                return Results.Ok(list);
            });

            // 詳細取得 GET /api/members/{id}
            group.MapGet("/{id:long}", async (long id, IMemberService service) =>
            {
                var dto = await service.GetByIdAsync(id);
                return dto is null ? Results.NotFound() : Results.Ok(dto);
            });

            // 編集保存 PUT /api/members/{id}
            group.MapPut("/{id:long}", async (long id, UpdateMemberRequest request, AppDbContext db, IMemberService service) =>
            {
                if (!Enum.IsDefined(typeof(MemberRole), request.RoleId))
                    return Results.BadRequest(new { message = "RoleId must be 1(Admin), 2(Customer), 9(Disabled)." });

                // ★ 退会（無効）会員は更新不可（API直叩き対策）
    var member = await db.Members
        .AsNoTracking()
        .Where(m => m.Id == id)
        .Select(m => new { m.IsActive, m.Role })
        .FirstOrDefaultAsync();

                if (member is null)
                    return Results.NotFound();

                if (!member.IsActive || member.Role == MemberRole.Disabled)
                    return Results.Conflict(new { message = "This member is disabled and cannot be edited." });

                var command = new UpdateMemberCommand
                {
                    Name = request.Name,
                    Email = request.Email,
                    PostalCode = request.PostalCode,
                    Address = request.Address,
                    Phone = request.Phone,
                    Role = (MemberRole)request.RoleId,
                    IsActive = request.IsActive
                };

                try
                {
                    var updated = await service.UpdateAsync(id, command);
                    return updated is null ? Results.NotFound() : Results.Ok(updated);
                }
                catch (InvalidOperationException ex)
                {
                    // Email重複など
                    return Results.BadRequest(new { message = ex.Message });
                }
            });

            // 退会（disabled 式）
            group.MapPut("/{id:long}/disable", async (
                long id,
                IMemberService service) =>
            {
                await service.DeactivateAsync(id);
                return Results.NoContent();
            });

            // 会員選択用 options（Active + Customerのみ）
            // GET /api/members/options
            group.MapGet("/options", async (IMemberService service) =>
            {
                // ★ service 側に Options 用メソッドを生やすのが理想
                // いったん “最小で動く” なら、service に OptionsAsync を追加するのがおすすめ
                var list = await service.GetOptionsAsync();
                return Results.Ok(list);
            });


            return app;
        }

        // GET /api/members 用のクエリパラメータ
        public class MemberSearchRequest
        {
            // 名前・メールどちらでも検索
            public string? Keyword { get; set; }

            // 1:管理者, 2:会員, 9:退会
            public int? RoleId { get; set; }

            // true:有効、false:無効、null:すべて
            public bool? IsActive { get; set; }

            public int Page { get; set; } = 1;
            public int PageSize { get; set; } = 50;
        }

        public sealed class UpdateMemberRequest
        {
            public string Name { get; set; } = null!;
            public string Email { get; set; } = null!;
            public string? PostalCode { get; set; }
            public string? Address { get; set; }
            public string? Phone { get; set; }
            public int RoleId { get; set; }     // 1/2/9
            public bool IsActive { get; set; }
        }
    }
}


