using System.Diagnostics;
using System.Security.Claims;
using InvoiceSystem.Application.Common.Interfaces;

namespace InvoiceSystem.Api.Common;

public static class AuditActorFactory
{
    public static AuditActor FromHttp(HttpContext http)
    {
        // 例：JWTで NameIdentifier に userId が入っている想定
        var idStr =
            http.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? http.User.FindFirstValue("sub"); // 予備

        if (!long.TryParse(idStr, out var userId))
            throw new InvalidOperationException("UserId claim is missing or invalid.");

        var role = http.User.FindFirstValue(ClaimTypes.Role); // 任意
        var ip = http.Connection.RemoteIpAddress?.ToString();
        var ua = http.Request.Headers.UserAgent.ToString();
        var corr = Activity.Current?.Id ?? http.TraceIdentifier;

        return new AuditActor(
            UserId: userId,
            Role: role,
            IpAddress: ip,
            UserAgent: ua,
            CorrelationId: corr
        );
    }
}
