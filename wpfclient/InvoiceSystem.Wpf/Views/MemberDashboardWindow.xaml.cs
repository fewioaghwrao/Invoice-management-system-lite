using InvoiceSystem.Wpf.Services;
using InvoiceSystem.Wpf.ViewModels;
using System.Windows;

namespace InvoiceSystem.Wpf.Views;

public partial class MemberDashboardWindow : Window
{
    private readonly object? _currentUser;
    private readonly AuthService _authService;
    private readonly InvoiceService _invoiceService;
    private readonly AccountService _accountService;

    public MemberDashboardWindow(
        object? currentUser,
        AuthService authService,
        InvoiceService invoiceService,
        AccountService accountService)
    {
        InitializeComponent();

        _currentUser = currentUser;
        _authService = authService;
        _invoiceService = invoiceService;
        _accountService = accountService;

        DataContext = new MemberDashboardViewModel(
            currentUser: _currentUser,
            authService: _authService,
            invoiceService: _invoiceService,
            accountService: _accountService,
            openInvoiceList: OpenInvoiceListWindow,
            openPaymentStatus: OpenPaymentStatusWindow,
            openProfile: OpenProfileWindow,
            openLogin: OpenLoginWindow,
            showConfirmDialog: ShowConfirmDialog);
    }

    private void OpenInvoiceListWindow()
    {
        var window = new MemberInvoiceListWindow(_currentUser, _authService, _invoiceService, _accountService);
        window.Show();
        Close();
    }

    private void OpenPaymentStatusWindow()
    {
        var window = new MemberPaymentStatusWindow(_currentUser, _authService, _invoiceService, _accountService);
        window.Show();
        Close();
    }

    private void OpenProfileWindow()
    {
        var window = new MemberProfileWindow(_currentUser, _authService, _invoiceService, _accountService);
        window.Show();
        Close();
    }

    private void OpenLoginWindow()
    {
        var loginWindow = new LoginWindow();
        loginWindow.Show();
        Close();
    }

    private bool ShowConfirmDialog(ConfirmDialogRequest request)
    {
        var dialog = new ConfirmDialogWindow(
            title: request.Title,
            message: request.Message,
            confirmText: request.ConfirmText,
            cancelText: request.CancelText,
            visualType: request.VisualType,
            subMessage: request.SubMessage)
        {
            Owner = this
        };

        var result = dialog.ShowDialog();
        return result == true && dialog.IsConfirmed;
    }
}