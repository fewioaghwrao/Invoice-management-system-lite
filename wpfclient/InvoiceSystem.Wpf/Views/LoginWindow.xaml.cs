using InvoiceSystem.Wpf.Configuration;
using InvoiceSystem.Wpf.Helpers;
using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Services;
using InvoiceSystem.Wpf.ViewModels;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace InvoiceSystem.Wpf.Views;

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
    private readonly AuthService _authService;
    private readonly InvoiceService _invoiceService;
    private readonly AccountService _accountService;
    private readonly AdminService _adminService;
    private readonly MemberService _memberService;
    private readonly SalesService _salesService;
    private readonly PaymentService _paymentService;
    private bool _isPasswordVisible;

    public LoginWindow()
    {
        InitializeComponent();

        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var apiSettings = config.GetSection("ApiSettings").Get<ApiSettings>()
                         ?? throw new InvalidOperationException("ApiSettings の読み込みに失敗しました。");

        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(apiSettings.BaseUrl)
        };

        _authService = new AuthService(httpClient);
        _invoiceService = new InvoiceService(httpClient);
        _accountService = new AccountService(httpClient);
        _adminService = new AdminService(httpClient);
        _memberService = new MemberService(httpClient);
        _salesService = new SalesService(httpClient);
        _paymentService = new PaymentService(httpClient);

        _viewModel = new LoginViewModel(_authService);
        _viewModel.LoginSucceeded += OnLoginSucceeded;

        DataContext = _viewModel;
    }

    // ===== 画面切り替え =====

    private void ShowLoginPage(string? notice = null)
    {
        SubPageHost.Content = null;
        SubPageHost.Visibility = Visibility.Collapsed;
        LoginPage.Visibility = Visibility.Visible;
        Title = LoginTitle;

        if (string.IsNullOrWhiteSpace(notice))
        {
            NoticeBorder.Visibility = Visibility.Collapsed;
        }
        else
        {
            NoticeTextBlock.Text = notice;
            NoticeBorder.Visibility = Visibility.Visible;
        }

        PageScrollViewer.ScrollToTop();
        Dispatcher.InvokeAsync(() => EmailTextBox.Focus(), System.Windows.Threading.DispatcherPriority.Input);
    }

    private void ShowSubPage(UserControl page, string title)
    {
        _viewModel.ErrorMessage = string.Empty;
        NoticeBorder.Visibility = Visibility.Collapsed;

        LoginPage.Visibility = Visibility.Collapsed;
        SubPageHost.Content = page;
        SubPageHost.Visibility = Visibility.Visible;
        Title = title;

        PageScrollViewer.ScrollToTop();
    }

    private void RegisterButton_OnClick(object sender, RoutedEventArgs e)
    {
        var page = new RegisterView(_authService);
        page.BackRequested += (_, _) => ShowLoginPage();
        page.Registered += (_, message) => ShowLoginPage(
            string.IsNullOrWhiteSpace(message)
                ? "登録が完了しました。登録したメールアドレスでログインしてください。"
                : message);

        ShowSubPage(page, RegisterTitle);
    }

    private void ForgotPasswordButton_OnClick(object sender, RoutedEventArgs e)
    {
        var page = new ForgotPasswordView(_authService);
        page.BackRequested += (_, _) => ShowLoginPage();

        ShowSubPage(page, ForgotPasswordTitle);
    }

    private void Window_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        // サブ画面表示中は Esc でログインに戻る
        if (e.Key == Key.Escape && SubPageHost.Visibility == Visibility.Visible)
        {
            ShowLoginPage();
            e.Handled = true;
        }
    }

    // ===== パスワード入力 =====

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (!_isPasswordVisible)
        {
            _viewModel.Password = PasswordBox.Password;
        }

        UpdatePasswordPlaceholder();
    }

    private void VisiblePasswordTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isPasswordVisible)
        {
            _viewModel.Password = VisiblePasswordTextBox.Text;
        }

        UpdatePasswordPlaceholder();
    }

    private void UpdatePasswordPlaceholder()
    {
        // PasswordBox は Password を Binding/Trigger できないため、プレースホルダーはここで切り替える
        if (PasswordPlaceholder is null)
        {
            return;
        }

        var current = _isPasswordVisible ? VisiblePasswordTextBox.Text : PasswordBox.Password;
        PasswordPlaceholder.Visibility = string.IsNullOrEmpty(current)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void TogglePasswordButton_OnClick(object sender, RoutedEventArgs e)
    {
        _isPasswordVisible = !_isPasswordVisible;

        if (_isPasswordVisible)
        {
            VisiblePasswordTextBox.Text = PasswordBox.Password;
            PasswordBox.Visibility = Visibility.Collapsed;
            VisiblePasswordTextBox.Visibility = Visibility.Visible;
            TogglePasswordButton.Content = "非表示";
            VisiblePasswordTextBox.Focus();
            VisiblePasswordTextBox.CaretIndex = VisiblePasswordTextBox.Text.Length;
        }
        else
        {
            PasswordBox.Password = VisiblePasswordTextBox.Text;
            PasswordBox.Visibility = Visibility.Visible;
            VisiblePasswordTextBox.Visibility = Visibility.Collapsed;
            TogglePasswordButton.Content = "表示";
            PasswordBox.Focus();
        }

        UpdatePasswordPlaceholder();
    }

    // ===== ログイン成功 =====

    private void OnLoginSucceeded(string role)
    {
        var normalizedRole = (role ?? string.Empty).Trim().ToUpperInvariant();

        if (normalizedRole == "ADMIN")
        {
            var adminWindow = new AdminDashboardWindow(
                _viewModel.CurrentUser,
                _authService,
                _invoiceService,
                _accountService,
                _adminService,
                _memberService,
                _salesService,
                _paymentService);

            adminWindow.Show();
            Close();
            return;
        }

        var memberWindow = new MemberShellWindow(
            _viewModel.CurrentUser,
            _authService,
            _invoiceService,
            _accountService);

        memberWindow.Show();
        Close();
    }

    // ===== 外部リンク =====

    private void OpenTermsButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExternalLinks.Open(ExternalLinks.TermsUrl, this);
    }

    private void OpenPrivacyPolicyButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExternalLinks.Open(ExternalLinks.PrivacyPolicyUrl, this);
    }
}
