using InvoiceSystem.Application.Commands.Members;
using InvoiceSystem.Application.Services.Auth;
using InvoiceSystem.Application.Services.Members;
using InvoiceSystem.Domain.Entities;
using InvoiceSystem.Domain.Enums;
using InvoiceSystem.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace InvoiceSystem.Api.Endpoints
{
    public static class AuthEndpoints
    {
        public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
        {

            // =========================
            //  会員登録 /auth/register
            // =========================
            app.MapPost("/auth/register", async (
                RegisterRequest request,
                MemberRegistrationService service,
                CancellationToken ct) =>
            {
                var command = new RegisterMemberCommand
                {
                    Name = request.Name,
                    Email = request.Email,
                    PostalCode = request.PostalCode,
                    Address = request.Address,
                    Phone = request.Phone,
                    Password = request.Password,
                    // フロントからの登録は常に一般会員
                    Role = null   // ← service 側で MemberRole.Customer を入れる
                };

                var result = await service.RegisterAsync(command, ct);

                if (!result.IsSuccess)
                {
                    return Results.BadRequest(new { message = result.Error });
                }

                return Results.Ok(new { message = "登録が完了しました。" });
            });

            // =========================
            //  メールアドレス確認
            // =========================
            app.MapPost("/auth/verify-email", async (
                VerifyEmailRequest request,
                AppDbContext db) =>
            {
                if (string.IsNullOrWhiteSpace(request.Token))
                {
                    return Results.BadRequest(new { message = "トークンが指定されていません。" });
                }

                var now = DateTime.UtcNow;

                var member = await db.Members
                    .FirstOrDefaultAsync(m =>
                        m.EmailVerificationToken == request.Token &&
                        m.EmailVerificationTokenExpiresAt != null &&
                        m.EmailVerificationTokenExpiresAt > now);

                if (member == null)
                {
                    return Results.BadRequest(new { message = "トークンが無効、または有効期限切れです。" });
                }

                member.IsEmailConfirmed = true;
                member.EmailVerificationToken = null;
                member.EmailVerificationTokenExpiresAt = null;
                member.UpdatedAt = now; // プロパティがあれば

                await db.SaveChangesAsync();

                return Results.Ok(new { message = "メールアドレスの確認が完了しました。" });
            });


            // =========================
            //  パスワード再設定メール送信 /auth/forgot-password
            // =========================
            app.MapPost("/auth/forgot-password", async (
                ForgotPasswordRequest request,
                PasswordResetService service) =>
            {
                // セキュリティ上、存在するかどうかはレスポンスでは伏せる
                var result = await service.RequestResetAsync(request.Email);

                if (!result.IsSuccess)
                {
                    // 内部エラー用（通常はここには来ない想定）
                    return Results.BadRequest(new { message = result.Error });
                }

                return Results.Ok(new { message = "パスワード再設定用のメールを送信しました。" });
            });

            // =========================
            //  パスワード再設定 /auth/reset-password
            // =========================
            app.MapPost("/auth/reset-password", async (
                ResetPasswordRequest request,
                PasswordResetService service) =>
            {
                var result = await service.ResetPasswordAsync(request.Token, request.NewPassword);

                if (!result.IsSuccess)
                {
                    return Results.BadRequest(new { message = result.Error });
                }

                return Results.Ok(new { message = "パスワードを更新しました。" });
            });

            // =========================
            //  ログイン /auth/login
            // =========================
            app.MapPost("/auth/login", async (
                LoginRequest request,
                AppDbContext db,
                IPasswordHasher<Member> hasher,
                IConfiguration config) =>
            {
                var user = await db.Members
                    .Where(m => m.Email == request.Email && m.IsActive)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return Results.Unauthorized();
                }

                var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
                if (result == PasswordVerificationResult.Failed)
                {
                    return Results.Unauthorized();
                }

                if (!user.IsEmailConfirmed)
                {
                    return Results.BadRequest(new { message = "メールアドレスの確認が完了していません。" });
                }

                // 🔽 ここが一番大事：アプリ用のロール名を決める ("Admin" / "Member")
                var appRole = user.Role == MemberRole.Admin ? "Admin" : "Member";

                var jwtSection = config.GetSection("Jwt");
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var claims = new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim("name", user.Name),
                    new Claim(ClaimTypes.Role, appRole) // ← ここも appRole を使う
                };

                var token = new JwtSecurityToken(
                    issuer: jwtSection["Issuer"],
                    audience: jwtSection["Audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtSection["ExpiresMinutes"] ?? "60")),
                    signingCredentials: creds
                );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                var response = new LoginResponse(
                    user.Id,
                    user.Name,
                    user.Email,
                    appRole,     // ← 返却も "Admin" / "Member"
                    tokenString
                );

                return Results.Ok(response);
            });

            return app;
        }

        // ===== DTOたち =====

        public record RegisterRequest(
            string Name,
            string Email,
            string Password,
            string? PostalCode,
            string? Address,
            string? Phone
        );

        public record VerifyEmailRequest(string Token);

        public record LoginRequest(string Email, string Password);
        public record LoginResponse(
            long Id,
            string Name,
            string Email,
            string Role,   // "Admin" / "Member"
            string Token
        );
        public record ForgotPasswordRequest(string Email);
        public record ResetPasswordRequest(string Token, string NewPassword);
    }
}

