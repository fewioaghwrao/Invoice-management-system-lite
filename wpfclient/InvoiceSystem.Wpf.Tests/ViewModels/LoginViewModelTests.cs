using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Services;
using InvoiceSystem.Wpf.Tests.Fakes;
using InvoiceSystem.Wpf.ViewModels;
using System;
using System.Threading.Tasks;
using Xunit;

namespace InvoiceSystem.Wpf.Tests.ViewModels;

public sealed class LoginViewModelTests
{
    [Fact]
    public void 初期状態では_LoginCommandは実行できない()
    {
        var authService = new FakeAuthService();
        var vm = new LoginViewModel(authService);

        Assert.False(vm.LoginCommand.CanExecute(null));
        Assert.False(vm.HasError);
        Assert.False(vm.IsBusy);
        Assert.Null(vm.CurrentUser);
    }

    [Fact]
    public void EmailとPasswordが入ると_LoginCommandが実行可能になる()
    {
        var authService = new FakeAuthService();
        var vm = new LoginViewModel(authService);

        vm.Email = "test@example.com";
        vm.Password = "password123";

        Assert.True(vm.LoginCommand.CanExecute(null));
    }

    [Fact]
    public async Task Login成功時_CurrentUserが設定され_LoginSucceededが発火する()
    {
        var authService = new FakeAuthService
        {
            ResponseToReturn = new LoginResponse
            {
                Id = 10,
                Name = "山田太郎",
                Email = "test@example.com",
                Role = "MEMBER",
                Token = "token-123"
            }
        };

        var vm = new LoginViewModel(authService)
        {
            Email = " test@example.com ",
            Password = "password123"
        };

        string? succeededRole = null;
        var tcs = new TaskCompletionSource<bool>();

        vm.LoginSucceeded += role =>
        {
            succeededRole = role;
            tcs.TrySetResult(true);
        };

        vm.LoginCommand.Execute(null);

        await tcs.Task;

        Assert.NotNull(authService.LastRequest);
        Assert.Equal("test@example.com", authService.LastRequest!.Email);
        Assert.Equal("password123", authService.LastRequest.Password);

        Assert.NotNull(vm.CurrentUser);
        Assert.Equal(10, vm.CurrentUser!.Id);
        Assert.Equal("山田太郎", vm.CurrentUser.Name);
        Assert.Equal("test@example.com", vm.CurrentUser.Email);
        Assert.Equal("MEMBER", vm.CurrentUser.Role);
        Assert.Equal("token-123", vm.CurrentUser.Token);

        Assert.Equal("MEMBER", succeededRole);
        Assert.False(vm.IsBusy);
        Assert.False(vm.HasError);
        Assert.Equal(string.Empty, vm.ErrorMessage);
    }

    [Fact]
    public async Task Login失敗時_ErrorMessageが設定され_CurrentUserはnullのまま()
    {
        var authService = new FakeAuthService
        {
            ExceptionToThrow = new Exception("メールアドレスまたはパスワードが正しくありません。")
        };

        var vm = new LoginViewModel(authService)
        {
            Email = "test@example.com",
            Password = "wrong-password"
        };

        vm.LoginCommand.Execute(null);

        await Task.Delay(20);

        Assert.True(vm.HasError);
        Assert.Equal("メールアドレスまたはパスワードが正しくありません。", vm.ErrorMessage);
        Assert.Null(vm.CurrentUser);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task Login実行中は_IsBusyがtrueになり_完了後falseに戻る()
    {
        var authService = new DelayedFakeAuthService();
        var vm = new LoginViewModel(authService)
        {
            Email = "test@example.com",
            Password = "password123"
        };

        vm.LoginCommand.Execute(null);

        await Task.Delay(20);
        Assert.True(vm.IsBusy);
        Assert.False(vm.LoginCommand.CanExecute(null));

        authService.Complete(new LoginResponse
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            Role = "MEMBER",
            Token = "token"
        });

        await Task.Delay(20);

        Assert.False(vm.IsBusy);
        Assert.True(vm.LoginCommand.CanExecute(null));
    }

    private sealed class DelayedFakeAuthService : IAuthService
    {
        private readonly TaskCompletionSource<LoginResponse> _tcs = new();

        public LoginRequest? LastRequest { get; private set; }

        public Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            LastRequest = request;
            return _tcs.Task;
        }

        public void Complete(LoginResponse response)
        {
            _tcs.SetResult(response);
        }

        public void Logout()
        {
        }

        public Task<(bool Success, string Message)> RegisterAsync(RegisterRequest request)
        {
            return Task.FromResult((true, "登録成功"));
        }
    }
}