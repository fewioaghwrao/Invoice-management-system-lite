using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Services;
using InvoiceSystem.Wpf.ViewModels;
using System.Windows;

namespace InvoiceSystem.Wpf.Views;

public partial class AdminDashboardWindow : Window
{
    private readonly AdminDashboardViewModel _viewModel;
    private readonly AuthService _authService;
    private readonly MemberService _memberService;
    private readonly InvoiceService _invoiceService;
    private readonly SalesService _salesService;
    private readonly PaymentService _paymentService;

    public AdminDashboardWindow(
        CurrentUser? currentUser,
        AuthService authService,
        InvoiceService invoiceService,
        AccountService accountService,
        AdminService adminService,
        MemberService memberService,
        SalesService salesService,
        PaymentService paymentService)
    {
        InitializeComponent();

        _authService = authService;
        _invoiceService = invoiceService;
        _memberService = memberService;
        _salesService = salesService;
        _paymentService = paymentService;

        _viewModel = new AdminDashboardViewModel(adminService, currentUser);
        DataContext = _viewModel;

        Loaded += AdminDashboardWindow_Loaded;
    }

    private async void AdminDashboardWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadAsync();
    }

    private async void PreviousYearButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.MovePreviousYear();
        await _viewModel.LoadAsync();
    }

    private async void NextYearButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.MoveNextYear();
        await _viewModel.LoadAsync();
    }

    private async void ReloadButton_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadAsync();
    }

    private void LogoutButton_OnClick(object sender, RoutedEventArgs e)
    {
        var confirmed = ShowConfirmDialog(
            title: "ログアウト確認",
            message: "管理者セッションを終了してログアウトしますか？",
            confirmText: "ログアウト",
            visualType: ConfirmDialogWindow.DialogVisualType.DangerConfirm,
            subMessage: "ログアウトすると、再度ログインが必要になります。");

        if (!confirmed)
            return;

        try
        {
            _authService.Logout();
        }
        catch
        {
        }

        var loginWindow = new LoginWindow();
        loginWindow.Show();
        Close();
    }

    private void InvoicesButton_OnClick(object sender, RoutedEventArgs e)
    {
        var window = new InvoiceListWindow(_invoiceService)
        {
            Owner = this
        };

        window.ShowDialog();
    }

    private void MembersButton_OnClick(object sender, RoutedEventArgs e)
    {
        var window = new MemberListWindow(_memberService)
        {
            Owner = this
        };

        window.ShowDialog();
    }

    private void SalesButton_OnClick(object sender, RoutedEventArgs e)
    {
        var window = new SalesListWindow(_salesService)
        {
            Owner = this
        };

        window.ShowDialog();
    }

    private void PaymentsButton_OnClick(object sender, RoutedEventArgs e)
    {
        var window = new PaymentListWindow(_paymentService, _invoiceService)
        {
            Owner = this
        };

        window.ShowDialog();
    }

    private bool ShowConfirmDialog(
    string title,
    string message,
    string confirmText,
    ConfirmDialogWindow.DialogVisualType visualType = ConfirmDialogWindow.DialogVisualType.Default,
    string? subMessage = null)
    {
        var dialog = new ConfirmDialogWindow(
            title: title,
            message: message,
            confirmText: confirmText,
            cancelText: "キャンセル",
            visualType: visualType,
            subMessage: subMessage)
        {
            Owner = this
        };

        var result = dialog.ShowDialog();
        return result == true && dialog.IsConfirmed;
    }
}