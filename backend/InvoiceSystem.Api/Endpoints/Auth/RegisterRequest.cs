// InvoiceSystem.Api/Endpoints/Auth/RegisterRequest.cs
namespace InvoiceSystem.Api.Endpoints.Auth;

public sealed class RegisterRequest
{
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;

    public string? PostalCode { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }

    // ★ 管理画面から管理者登録したい場合はここに Role を追加してもOK
    // public MemberRole? Role { get; set; }
}


