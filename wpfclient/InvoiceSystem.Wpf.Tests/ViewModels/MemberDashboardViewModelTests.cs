using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Services;
using InvoiceSystem.Wpf.ViewModels;
using InvoiceSystem.Wpf.Views;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace InvoiceSystem.Wpf.Tests.ViewModels;

public class MemberDashboardViewModelTests
{
    [Fact]
    public void Constructor_WithRoleProperty_LoadsDisplayFields()
    {
        var authService = new RecordingAuthService();
        var invoiceService = CreateInvoiceService();
        var accountService = CreateAccountService();

        var currentUser = new
        {
            Name = "山田太郎",
            Email = "taro@example.com",
            Role = "ADMIN"
        };

        var vm = new MemberDashboardViewModel(
            currentUser,
            authService,
            invoiceService,
            accountService,
            openInvoiceList: () => { },
            openPaymentStatus: () => { },
            openProfile: () => { },
            openLogin: () => { },
            showConfirmDialog: _ => true);

        Assert.Equal("山田太郎", vm.DisplayName);
        Assert.Equal("taro@example.com", vm.DisplayEmail);
        Assert.Equal("管理者", vm.RoleLabel);
        Assert.Equal("ようこそ、山田太郎さん", vm.WelcomeMessage);
        Assert.Equal("taro@example.com でログイン中", vm.SubWelcomeMessage);
        Assert.Equal("Invoice & Payment Status Dashboard (Lite) / Member Dashboard", vm.FooterText);
    }

    [Fact]
    public void Constructor_WithRoleNameProperty_LoadsRoleLabel()
    {
        var authService = new RecordingAuthService();
        var invoiceService = CreateInvoiceService();
        var accountService = CreateAccountService();

        var currentUser = new
        {
            Name = "佐藤花子",
            Email = "hanako@example.com",
            RoleName = "member"
        };

        var vm = new MemberDashboardViewModel(
            currentUser,
            authService,
            invoiceService,
            accountService,
            openInvoiceList: () => { },
            openPaymentStatus: () => { },
            openProfile: () => { },
            openLogin: () => { },
            showConfirmDialog: _ => true);

        Assert.Equal("佐藤花子", vm.DisplayName);
        Assert.Equal("hanako@example.com", vm.DisplayEmail);
        Assert.Equal("一般会員", vm.RoleLabel);
    }

    [Fact]
    public void Constructor_WithUserRoleProperty_LoadsRoleLabel()
    {
        var authService = new RecordingAuthService();
        var invoiceService = CreateInvoiceService();
        var accountService = CreateAccountService();

        var currentUser = new
        {
            Name = "高橋次郎",
            Email = "jiro@example.com",
            UserRole = "9"
        };

        var vm = new MemberDashboardViewModel(
            currentUser,
            authService,
            invoiceService,
            accountService,
            openInvoiceList: () => { },
            openPaymentStatus: () => { },
            openProfile: () => { },
            openLogin: () => { },
            showConfirmDialog: _ => true);

        Assert.Equal("退会", vm.RoleLabel);
    }

    [Fact]
    public void Constructor_WithNullCurrentUser_UsesFallbackValues()
    {
        var authService = new RecordingAuthService();
        var invoiceService = CreateInvoiceService();
        var accountService = CreateAccountService();

        var vm = new MemberDashboardViewModel(
            null,
            authService,
            invoiceService,
            accountService,
            openInvoiceList: () => { },
            openPaymentStatus: () => { },
            openProfile: () => { },
            openLogin: () => { },
            showConfirmDialog: _ => true);

        Assert.Equal("会員ユーザー", vm.DisplayName);
        Assert.Equal("メールアドレス未設定", vm.DisplayEmail);
        Assert.Equal("一般会員", vm.RoleLabel);
        Assert.Equal("ようこそ、会員ユーザーさん", vm.WelcomeMessage);
        Assert.Equal("メールアドレス未設定 でログイン中", vm.SubWelcomeMessage);
    }

    [Theory]
    [InlineData("ADMIN", "管理者")]
    [InlineData("MEMBER", "一般会員")]
    [InlineData("USER", "一般会員")]
    [InlineData("1", "管理者")]
    [InlineData("2", "一般会員")]
    [InlineData("9", "退会")]
    [InlineData("CustomRole", "CustomRole")]
    public void Constructor_RoleMappings_AreApplied(string roleValue, string expectedLabel)
    {
        var authService = new RecordingAuthService();
        var invoiceService = CreateInvoiceService();
        var accountService = CreateAccountService();

        var currentUser = new
        {
            Name = "テスト",
            Email = "test@example.com",
            Role = roleValue
        };

        var vm = new MemberDashboardViewModel(
            currentUser,
            authService,
            invoiceService,
            accountService,
            openInvoiceList: () => { },
            openPaymentStatus: () => { },
            openProfile: () => { },
            openLogin: () => { },
            showConfirmDialog: _ => true);

        Assert.Equal(expectedLabel, vm.RoleLabel);
    }

    [Fact]
    public void OpenInvoicesCommand_InvokesOpenInvoiceListAction()
    {
        var authService = new RecordingAuthService();
        var invoiceService = CreateInvoiceService();
        var accountService = CreateAccountService();
        var called = 0;

        var vm = new MemberDashboardViewModel(
            new { Name = "A", Email = "a@example.com", Role = "MEMBER" },
            authService,
            invoiceService,
            accountService,
            openInvoiceList: () => called++,
            openPaymentStatus: () => { },
            openProfile: () => { },
            openLogin: () => { },
            showConfirmDialog: _ => true);

        vm.OpenInvoicesCommand.Execute(null);

        Assert.Equal(1, called);
    }

    [Fact]
    public void OpenUnpaidCommand_InvokesOpenPaymentStatusAction()
    {
        var authService = new RecordingAuthService();
        var invoiceService = CreateInvoiceService();
        var accountService = CreateAccountService();
        var called = 0;

        var vm = new MemberDashboardViewModel(
            new { Name = "A", Email = "a@example.com", Role = "MEMBER" },
            authService,
            invoiceService,
            accountService,
            openInvoiceList: () => { },
            openPaymentStatus: () => called++,
            openProfile: () => { },
            openLogin: () => { },
            showConfirmDialog: _ => true);

        vm.OpenUnpaidCommand.Execute(null);

        Assert.Equal(1, called);
    }

    [Fact]
    public void OpenProfileCommand_InvokesOpenProfileAction()
    {
        var authService = new RecordingAuthService();
        var invoiceService = CreateInvoiceService();
        var accountService = CreateAccountService();
        var called = 0;

        var vm = new MemberDashboardViewModel(
            new { Name = "A", Email = "a@example.com", Role = "MEMBER" },
            authService,
            invoiceService,
            accountService,
            openInvoiceList: () => { },
            openPaymentStatus: () => { },
            openProfile: () => called++,
            openLogin: () => { },
            showConfirmDialog: _ => true);

        vm.OpenProfileCommand.Execute(null);

        Assert.Equal(1, called);
    }

    [Fact]
    public void BackToLoginCommand_WhenConfirmed_InvokesOpenLogin()
    {
        var authService = new RecordingAuthService();
        var invoiceService = CreateInvoiceService();
        var accountService = CreateAccountService();
        var openLoginCalled = 0;
        ConfirmDialogRequest? request = null;

        var vm = new MemberDashboardViewModel(
            new { Name = "A", Email = "a@example.com", Role = "MEMBER" },
            authService,
            invoiceService,
            accountService,
            openInvoiceList: () => { },
            openPaymentStatus: () => { },
            openProfile: () => { },
            openLogin: () => openLoginCalled++,
            showConfirmDialog: r =>
            {
                request = r;
                return true;
            });

        vm.BackToLoginCommand.Execute(null);

        Assert.Equal(1, openLoginCalled);
        Assert.NotNull(request);
        Assert.Equal("ログイン画面へ戻る", request!.Title);
        Assert.Equal("現在の画面を閉じて、ログイン画面へ戻りますか？", request.Message);
        Assert.Equal("戻る", request.ConfirmText);
        Assert.Equal("キャンセル", request.CancelText);
        Assert.Equal(ConfirmDialogWindow.DialogVisualType.Default, request.VisualType);
    }

    [Fact]
    public void BackToLoginCommand_WhenCancelled_DoesNotInvokeOpenLogin()
    {
        var authService = new RecordingAuthService();
        var invoiceService = CreateInvoiceService();
        var accountService = CreateAccountService();
        var openLoginCalled = 0;

        var vm = new MemberDashboardViewModel(
            new { Name = "A", Email = "a@example.com", Role = "MEMBER" },
            authService,
            invoiceService,
            accountService,
            openInvoiceList: () => { },
            openPaymentStatus: () => { },
            openProfile: () => { },
            openLogin: () => openLoginCalled++,
            showConfirmDialog: _ => false);

        vm.BackToLoginCommand.Execute(null);

        Assert.Equal(0, openLoginCalled);
    }

    [Fact]
    public void LogoutCommand_WhenConfirmed_LogsOutAndOpensLogin()
    {
        var authService = new RecordingAuthService();
        var invoiceService = CreateInvoiceService();
        var accountService = CreateAccountService();
        var openLoginCalled = 0;
        ConfirmDialogRequest? request = null;

        var vm = new MemberDashboardViewModel(
            new { Name = "A", Email = "a@example.com", Role = "MEMBER" },
            authService,
            invoiceService,
            accountService,
            openInvoiceList: () => { },
            openPaymentStatus: () => { },
            openProfile: () => { },
            openLogin: () => openLoginCalled++,
            showConfirmDialog: r =>
            {
                request = r;
                return true;
            });

        vm.LogoutCommand.Execute(null);

        Assert.Equal(1, authService.LogoutCallCount);
        Assert.Equal(1, openLoginCalled);

        Assert.NotNull(request);
        Assert.Equal("ログアウト確認", request!.Title);
        Assert.Equal("現在のセッションを終了してログアウトしますか？", request.Message);
        Assert.Equal("ログアウト", request.ConfirmText);
        Assert.Equal("キャンセル", request.CancelText);
        Assert.Equal("ログアウトすると、再度ログインが必要になります。", request.SubMessage);
        Assert.Equal(ConfirmDialogWindow.DialogVisualType.DangerConfirm, request.VisualType);
    }

    [Fact]
    public void LogoutCommand_WhenCancelled_DoesNothing()
    {
        var authService = new RecordingAuthService();
        var invoiceService = CreateInvoiceService();
        var accountService = CreateAccountService();
        var openLoginCalled = 0;

        var vm = new MemberDashboardViewModel(
            new { Name = "A", Email = "a@example.com", Role = "MEMBER" },
            authService,
            invoiceService,
            accountService,
            openInvoiceList: () => { },
            openPaymentStatus: () => { },
            openProfile: () => { },
            openLogin: () => openLoginCalled++,
            showConfirmDialog: _ => false);

        vm.LogoutCommand.Execute(null);

        Assert.Equal(0, authService.LogoutCallCount);
        Assert.Equal(0, openLoginCalled);
    }

    private static InvoiceService CreateInvoiceService()
    {
        return new InvoiceService(new HttpClient(new StubHttpMessageHandler()));
    }

    private static AccountService CreateAccountService()
    {
        return new AccountService(new HttpClient(new StubHttpMessageHandler()));
    }

    private sealed class RecordingAuthService : IAuthService
    {
        public int LogoutCallCount { get; private set; }

        public Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            return Task.FromResult(new LoginResponse());
        }

        public void Logout()
        {
            LogoutCallCount++;
        }

        public Task<(bool Success, string Message)> RegisterAsync(RegisterRequest request)
        {
            return Task.FromResult((true, "登録成功"));
        }
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}