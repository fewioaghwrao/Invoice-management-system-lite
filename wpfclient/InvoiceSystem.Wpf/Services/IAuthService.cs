using System.Threading.Tasks;
using InvoiceSystem.Wpf.Models;

namespace InvoiceSystem.Wpf.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    void Logout();

    Task<(bool Success, string Message)> RegisterAsync(RegisterRequest request);
}