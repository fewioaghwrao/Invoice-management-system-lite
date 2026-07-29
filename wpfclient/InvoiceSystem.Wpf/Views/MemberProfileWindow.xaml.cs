using InvoiceSystem.Wpf.Services;
using InvoiceSystem.Wpf.ViewModels;
using System.Windows;

namespace InvoiceSystem.Wpf.Views;

public partial class MemberProfileWindow : Window
{
    private readonly object? _currentUser;
    private readonly AuthService _authService;
    private readonly InvoiceService _invoiceService;
    private readonly AccountService _accountService;
    private readonly MemberProfileViewModel _viewModel;

    public MemberProfileWindow(
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

        _viewModel = new MemberProfileViewModel(
            accountService: _accountService,
            openDashboard: OpenDashboardWindow,
            openLogin: OpenLoginWindow,
            showConfirmDialog: ShowConfirmDialog);

        DataContext = _viewModel;

        Loaded += MemberProfileWindow_Loaded;
    }

    private async void MemberProfileWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();
    }

    private void OpenDashboardWindow()
    {
        var dashboard = new MemberDashboardWindow(
            _currentUser,
            _authService,
            _invoiceService,
            _accountService);

        dashboard.Show();
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