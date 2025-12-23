// Api/Common/HttpContextExtensions.cs
using System.Security.Claims;
using InvoiceSystem.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Api.Common;

public static class HttpContextExtensions
{
    public static async Task<long> GetMemberIdAsync(this HttpContext http, AppDbContext db)
    {
        var email = http.User?.FindFirstValue("email")
                 ?? http.User?.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrWhiteSpace(email))
            throw new UnauthorizedAccessException("User email not found");

        var memberId = await db.Members
            .Where(m => m.Email == email && m.IsActive)
            .Select(m => (long?)m.Id)
            .FirstOrDefaultAsync();

        if (memberId == null)
            throw new UnauthorizedAccessException("MemberId not found");

        return memberId.Value;
    }
}



