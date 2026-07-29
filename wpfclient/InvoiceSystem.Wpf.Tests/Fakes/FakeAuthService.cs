using System;
using System.Threading.Tasks;
using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Services;

namespace InvoiceSystem.Wpf.Tests.Fakes;

public sealed class FakeAuthService : IAuthService
{
    // =========================
    // LoginViewModel 用
    // =========================
    public LoginRequest? LastRequest { get; private set; }
    public LoginResponse? ResponseToReturn { get; set; }
    public Exception? ExceptionToThrow { get; set; }

    public Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        LastRequest = request;

        if (ExceptionToThrow != null)
        {
            return Task.FromException<LoginResponse>(ExceptionToThrow);
        }

        return Task.FromResult(ResponseToReturn ?? new LoginResponse());
    }

    public int LogoutCallCount { get; private set; }

    public void Logout()
    {
        LogoutCallCount++;
    }

    // =========================
    // RegisterViewModel 用
    // =========================
    public RegisterRequest? LastRegisterRequest { get; private set; }

    public int RegisterCallCount { get; private set; }

    public (bool Success, string Message) RegisterResultToReturn { get; set; }
        = (true, "登録が完了しました。ログイン画面からサインインしてください。");

    public Exception? ExceptionToThrowOnRegister { get; set; }

    public Task<(bool Success, string Message)> RegisterAsync(RegisterRequest request)
    {
        RegisterCallCount++;
        LastRegisterRequest = request;

        if (ExceptionToThrowOnRegister != null)
        {
            return Task.FromException<(bool Success, string Message)>(
                ExceptionToThrowOnRegister);
        }

        return Task.FromResult(RegisterResultToReturn);
    }
}