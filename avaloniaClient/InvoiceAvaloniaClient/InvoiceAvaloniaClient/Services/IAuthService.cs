using System.Threading.Tasks;
using InvoiceAvaloniaClient.Models;

namespace InvoiceAvaloniaClient.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    void Logout();

    Task<(bool Success, string Message)> RegisterAsync(RegisterRequest request);
}