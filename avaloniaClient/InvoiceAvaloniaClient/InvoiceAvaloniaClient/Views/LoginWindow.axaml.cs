using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using InvoiceAvaloniaClient.Configuration;
using InvoiceAvaloniaClient.Helpers;
using InvoiceAvaloniaClient.ViewModels;

namespace InvoiceAvaloniaClient.Views;

/// <summary>
/// ログイン前の画面をまとめたウィンドウ。
/// 新規会員登録・パスワード再設定は別ウィンドウにせず、このウィンドウ内で表示を切り替える。
/// </summary>
public partial class LoginWindow : Window
{
    private const string LoginTitle = "ログイン - Invoice Management System";
    private const string RegisterTitle = "新規会員登録 - Invoice Management System";
    private const string ForgotPasswordTitle = "パスワード再設定 - Invoice Management System";

    private readonly LoginViewModel _viewModel;
    private readonly AppServices _services;

    public LoginWindow()
    {
        InitializeComponent();

        _services = AppServices.Create();

        _viewModel = new LoginViewModel(_services.AuthService);
        _viewModel.LoginSucceeded += OnLoginSucceeded;

        DataContext = _viewModel;

        // サブ画面表示中は Esc でログインに戻る（子の入力欄より先に受け取る）
        AddHandler(KeyDownEvent, Window_OnPreviewKeyDown, RoutingStrategies.Tunnel);

        Opened += (_, _) => EmailTextBox.Focus();
    }

    // ===== 画面切り替え =====

    private void ShowLoginPage(string? notice = null)
    {
        SubPageHost.Content = null;
        SubPageHost.IsVisible = false;
        LoginPage.IsVisible = true;
        Title = LoginTitle;

        if (string.IsNullOrWhiteSpace(notice))
        {
            NoticeBorder.IsVisible = false;
        }
        else
        {
            NoticeTextBlock.Text = notice;
            NoticeBorder.IsVisible = true;
        }

        PageScrollViewer.ScrollToHome();
        Dispatcher.UIThread.Post(() => EmailTextBox.Focus(), DispatcherPriority.Input);
    }

    private void ShowSubPage(UserControl page, string title)
    {
        _viewModel.ErrorMessage = string.Empty;
        NoticeBorder.IsVisible = false;

        LoginPage.IsVisible = false;
        SubPageHost.Content = page;
        SubPageHost.IsVisible = true;
        Title = title;

        PageScrollViewer.ScrollToHome();
    }

    private void RegisterButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var page = new RegisterView(_services.AuthService);
        page.BackRequested += (_, _) => ShowLoginPage();
        page.Registered += (_, message) => ShowLoginPage(
            string.IsNullOrWhiteSpace(message)
                ? "登録が完了しました。登録したメールアドレスでログインしてください。"
                : message);

        ShowSubPage(page, RegisterTitle);
    }

    private void ForgotPasswordButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var page = new ForgotPasswordView(_services.AuthService);
        page.BackRequested += (_, _) => ShowLoginPage();

        ShowSubPage(page, ForgotPasswordTitle);
    }

    private void Window_OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && SubPageHost.IsVisible)
        {
            ShowLoginPage();
            e.Handled = true;
        }
    }

    // ===== パスワード入力 =====

    private void TogglePasswordButton_OnClick(object? sender, RoutedEventArgs e)
    {
        PasswordBox.RevealPassword = !PasswordBox.RevealPassword;
        TogglePasswordButton.Content = PasswordBox.RevealPassword ? "非表示" : "表示";

        PasswordBox.Focus();
        PasswordBox.CaretIndex = PasswordBox.Text?.Length ?? 0;
    }

    // ===== ログイン成功 =====

    private void OnLoginSucceeded(string role)
    {
        var normalizedRole = (role ?? string.Empty).Trim().ToUpperInvariant();

        Window next = normalizedRole == "ADMIN"
            ? new AdminShellWindow(_viewModel.CurrentUser, _services)
            : new MemberShellWindow(_viewModel.CurrentUser, _services);

        WindowNavigation.Replace(this, next);
    }

    // ===== 外部リンク =====

    private void OpenTermsButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ExternalLinks.Open(ExternalLinks.TermsUrl, this);
    }

    private void OpenPrivacyPolicyButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ExternalLinks.Open(ExternalLinks.PrivacyPolicyUrl, this);
    }
}
