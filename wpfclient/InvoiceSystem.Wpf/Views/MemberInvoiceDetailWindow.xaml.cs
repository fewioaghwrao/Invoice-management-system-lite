using InvoiceSystem.Wpf.Services;
using InvoiceSystem.Wpf.ViewModels;
using System.Windows;

namespace InvoiceSystem.Wpf.Views;

public partial class MemberInvoiceDetailWindow : Window
{
    private readonly long _invoiceId;
    private readonly object? _currentUser;
    private readonly AuthService _authService;
    private readonly InvoiceService _invoiceService;
    private readonly AccountService _accountService;
    private readonly MemberInvoiceDetailViewModel _viewModel;

    public MemberInvoiceDetailWindow(
        long invoiceId,
        object? currentUser,
        AuthService authService,
        InvoiceService invoiceService,
        AccountService accountService)
    {
        InitializeComponent();

        _invoiceId = invoiceId;
        _currentUser = currentUser;
        _authService = authService;
        _invoiceService = invoiceService;
        _accountService = accountService;

        _viewModel = new MemberInvoiceDetailViewModel(
            invoiceId: _invoiceId,
            invoiceService: _invoiceService,
            openInvoiceList: OpenInvoiceListWindow);

        DataContext = _viewModel;

        Loaded += MemberInvoiceDetailWindow_Loaded;
    }

    private async void MemberInvoiceDetailWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();
    }

    private void OpenInvoiceListWindow()
    {
        var window = new MemberInvoiceListWindow(_currentUser, _authService, _invoiceService, _accountService);
        window.Show();
        Close();
    }
}