using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using InvoiceSystem.Domain.Enums;
using InvoiceSystem.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Api.Common;

public static class HttpContextExtensions
{
    public static async Task<long> GetMemberIdAsync(this HttpContext http, AppDbContext db)
    {
        if (http.User?.Identity?.IsAuthenticated != true)
            throw new UnauthorizedAccessException("Not authenticated");

        // ★ JWT の sub = user.Id
        var sub = http.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? http.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!long.TryParse(sub, out var memberId))
            throw new UnauthorizedAccessException("User id (sub) not found");

        // ★ 退会/無効（Disabled）を弾く（退会後トークン対策）
        var member = await db.Members
            .AsNoTracking()
            .Where(m => m.Id == memberId)
            .Select(m => new { m.IsActive, m.Role })
            .FirstOrDefaultAsync();

        if (member is null)
            throw new UnauthorizedAccessException("Member not found");

        if (!member.IsActive || member.Role == MemberRole.Disabled)
            throw new UnauthorizedAccessException("Member is disabled");

        return memberId;
    }
}



