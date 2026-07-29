using InvoiceSystem.Wpf.Infrastructure;
using InvoiceSystem.Wpf.Services;
using InvoiceSystem.Wpf.Views;
using System;
using System.Reflection;
using System.Windows.Input;

namespace InvoiceSystem.Wpf.ViewModels;

public sealed class MemberDashboardViewModel : ViewModelBase
{
    private readonly object? _currentUser;
    private readonly IAuthService _authService;
    private readonly InvoiceService _invoiceService;
    private readonly AccountService _accountService;

    private readonly Action _openInvoiceList;
    private readonly Action _openPaymentStatus;
    private readonly Action _openProfile;
    private readonly Action _openLogin;
    private readonly Func<ConfirmDialogRequest, bool> _showConfirmDialog;

    private string _displayName = "会員ユーザー";
    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }

    private string _displayEmail = "メールアドレス未設定";
    public string DisplayEmail
    {
        get => _displayEmail;
        set => SetProperty(ref _displayEmail, value);
    }

    private string _roleLabel = "一般会員";
    public string RoleLabel
    {
        get => _roleLabel;
        set => SetProperty(ref _roleLabel, value);
    }

    private string _welcomeMessage = "ようこそ、会員さま";
    public string WelcomeMessage
    {
        get => _welcomeMessage;
        set => SetProperty(ref _welcomeMessage, value);
    }

    private string _subWelcomeMessage = "ログイン情報を読み込みました。";
    public string SubWelcomeMessage
    {
        get => _subWelcomeMessage;
        set => SetProperty(ref _subWelcomeMessage, value);
    }

    private string _footerText = "Invoice & Payment Status Dashboard (Lite) / Member Dashboard";
    public string FooterText
    {
        get => _footerText;
        set => SetProperty(ref _footerText, value);
    }

    public ICommand OpenInvoicesCommand { get; }
    public ICommand OpenUnpaidCommand { get; }
    public ICommand OpenProfileCommand { get; }
    public ICommand BackToLoginCommand { get; }
    public ICommand LogoutCommand { get; }

    public MemberDashboardViewModel(
        object? currentUser,
        IAuthService authService,
        InvoiceService invoiceService,
        AccountService accountService,
        Action openInvoiceList,
        Action openPaymentStatus,
        Action openProfile,
        Action openLogin,
        Func<ConfirmDialogRequest, bool> showConfirmDialog)
    {
        _currentUser = currentUser;
        _authService = authService;
        _invoiceService = invoiceService;
        _accountService = accountService;
        _openInvoiceList = openInvoiceList;
        _openPaymentStatus = openPaymentStatus;
        _openProfile = openProfile;
        _openLogin = openLogin;
        _showConfirmDialog = showConfirmDialog;

        OpenInvoicesCommand = new RelayCommand(OpenInvoices);
        OpenUnpaidCommand = new RelayCommand(OpenUnpaid);
        OpenProfileCommand = new RelayCommand(OpenProfile);
        BackToLoginCommand = new RelayCommand(BackToLogin);
        LogoutCommand = new RelayCommand(Logout);

        LoadCurrentUser();
    }

    private void LoadCurrentUser()
    {
        var name = ReadProperty(_currentUser, "Name") ?? "会員ユーザー";
        var email = ReadProperty(_currentUser, "Email") ?? "メールアドレス未設定";
        var roleRaw = ReadProperty(_currentUser, "Role")
                      ?? ReadProperty(_currentUser, "RoleName")
                      ?? ReadProperty(_currentUser, "UserRole");

        var roleLabel = ToRoleLabel(roleRaw);

        DisplayName = name;
        DisplayEmail = email;
        RoleLabel = roleLabel;
        WelcomeMessage = $"ようこそ、{name}さん";
        SubWelcomeMessage = $"{email} でログイン中";
    }

    private void OpenInvoices()
    {
        _openInvoiceList();
    }

    private void OpenUnpaid()
    {
        _openPaymentStatus();
    }

    private void OpenProfile()
    {
        _openProfile();
    }

    private void BackToLogin()
    {
        var confirmed = _showConfirmDialog(new ConfirmDialogRequest
        {
            Title = "ログイン画面へ戻る",
            Message = "現在の画面を閉じて、ログイン画面へ戻りますか？",
            ConfirmText = "戻る",
            CancelText = "キャンセル",
            VisualType = ConfirmDialogWindow.DialogVisualType.Default
        });

        if (!confirmed)
            return;

        _openLogin();
    }

    private void Logout()
    {
        var confirmed = _showConfirmDialog(new ConfirmDialogRequest
        {
            Title = "ログアウト確認",
            Message = "現在のセッションを終了してログアウトしますか？",
            ConfirmText = "ログアウト",
            CancelText = "キャンセル",
            VisualType = ConfirmDialogWindow.DialogVisualType.DangerConfirm,
            SubMessage = "ログアウトすると、再度ログインが必要になります。"
        });

        if (!confirmed)
            return;

        _authService.Logout();
        _openLogin();
    }

    private static string? ReadProperty(object? target, string propertyName)
    {
        if (target == null) return null;

        var prop = target.GetType().GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        var value = prop?.GetValue(target);
        return value?.ToString();
    }

    private static string ToRoleLabel(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return "一般会員";

        var normalized = role.Trim().ToUpperInvariant();

        return normalized switch
        {
            "ADMIN" => "管理者",
            "MEMBER" => "一般会員",
            "USER" => "一般会員",
            "2" => "一般会員",
            "1" => "管理者",
            "9" => "退会",
            _ => role
        };
    }
}
