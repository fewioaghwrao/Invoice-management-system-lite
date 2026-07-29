using InvoiceSystem.Wpf.Configuration;
using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Services;
using InvoiceSystem.Wpf.ViewModels;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;

namespace InvoiceSystem.Wpf.Views;

public partial class LoginWindow : Window
{
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

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (!_isPasswordVisible)
        {
            _viewModel.Password = PasswordBox.Password;
        }
    }

    private void VisiblePasswordTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isPasswordVisible)
        {
            _viewModel.Password = VisiblePasswordTextBox.Text;
        }
    }

    private void TogglePasswordButton_OnClick(object sender, RoutedEventArgs e)
    {
        _isPasswordVisible = !_isPasswordVisible;

        if (_isPasswordVisible)
        {
            VisiblePasswordTextBox.Text = PasswordBox.Password;
            PasswordBoxBorder.Visibility = Visibility.Collapsed;
            VisiblePasswordBorder.Visibility = Visibility.Visible;
            TogglePasswordButton.Content = "非表示";
            VisiblePasswordTextBox.Focus();
            VisiblePasswordTextBox.CaretIndex = VisiblePasswordTextBox.Text.Length;
        }
        else
        {
            PasswordBox.Password = VisiblePasswordTextBox.Text;
            PasswordBoxBorder.Visibility = Visibility.Visible;
            VisiblePasswordBorder.Visibility = Visibility.Collapsed;
            TogglePasswordButton.Content = "表示";
            PasswordBox.Focus();
        }
    }

    private void ForgotPasswordButton_OnClick(object sender, RoutedEventArgs e)
    {
        var window = new ForgotPasswordWindow(_authService)
        {
            Owner = this
        };

        window.ShowDialog();
    }

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

        var memberWindow = new MemberDashboardWindow(
            _viewModel.CurrentUser,
            _authService,
            _invoiceService,
            _accountService);

        memberWindow.Show();
        Close();
    }

    private void RegisterButton_OnClick(object sender, RoutedEventArgs e)
    {
        var window = new RegisterWindow(_authService)
        {
            Owner = this
        };

        window.ShowDialog();
    }

    private void OpenTermsButton_OnClick(object sender, RoutedEventArgs e)
    {
        var window = new TermsWindow
        {
            Owner = this
        };

        window.ShowDialog();
    }

    private void OpenPrivacyPolicyButton_OnClick(object sender, RoutedEventArgs e)
    {
        var window = new PrivacyPolicyWindow
        {
            Owner = this
        };

        window.ShowDialog();
    }
}