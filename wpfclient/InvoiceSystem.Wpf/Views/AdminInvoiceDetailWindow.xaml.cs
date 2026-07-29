using InvoiceSystem.Wpf.Services;
using InvoiceSystem.Wpf.ViewModels;
using System.Windows;

namespace InvoiceSystem.Wpf.Views;

public partial class AdminInvoiceDetailWindow : Window
{
    private readonly InvoiceService _invoiceService;
    private readonly long _invoiceId;
    private readonly AdminInvoiceDetailViewModel _viewModel;

    public AdminInvoiceDetailWindow(InvoiceService invoiceService, long invoiceId)
    {
        InitializeComponent();

        _invoiceService = invoiceService;
        _invoiceId = invoiceId;
        _viewModel = new AdminInvoiceDetailViewModel(invoiceService, invoiceId);
        DataContext = _viewModel;

        Loaded += AdminInvoiceDetailWindow_Loaded;
    }

    private async void AdminInvoiceDetailWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadAsync();
    }

    private async void ReloadButton_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadAsync();
    }

    private async void OpenPdfButton_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.OpenPdfAsync();
    }

    private async void EditButton_OnClick(object sender, RoutedEventArgs e)
    {
        var window = new AdminInvoiceEditWindow(_invoiceService, _invoiceId)
        {
            Owner = this
        };

        var result = window.ShowDialog();
        if (result == true)
        {
            await _viewModel.LoadAsync();
        }
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}