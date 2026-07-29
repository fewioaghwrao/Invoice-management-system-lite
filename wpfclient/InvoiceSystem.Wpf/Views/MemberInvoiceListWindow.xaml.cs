using InvoiceSystem.Wpf.Services;
using InvoiceSystem.Wpf.ViewModels;
using System.Windows;

namespace InvoiceSystem.Wpf.Views;

public partial class MemberInvoiceListWindow : Window
{
    private readonly object? _currentUser;
    private readonly AuthService _authService;
    private readonly InvoiceService _invoiceService;
    private readonly AccountService _accountService;
    private readonly MemberInvoiceListViewModel _viewModel;

    public MemberInvoiceListWindow(
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

        _viewModel = new MemberInvoiceListViewModel(
            currentUser: _currentUser,
            invoiceService: _invoiceService,
            openDetail: OpenDetailWindow,
            openDashboard: OpenDashboardWindow);

        DataContext = _viewModel;

        Loaded += MemberInvoiceListWindow_Loaded;
    }

    private async void MemberInvoiceListWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();
    }

    private void OpenDetailWindow(long invoiceId)
    {
        var detailWindow = new MemberInvoiceDetailWindow(
            invoiceId,
            _currentUser,
            _authService,
            _invoiceService,
            _accountService);

        detailWindow.Show();
        Close();
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
}