using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Services;
using InvoiceSystem.Wpf.ViewModels;
using System.Windows;

namespace InvoiceSystem.Wpf.Views;

public partial class PaymentListWindow : Window
{
    private readonly PaymentListViewModel _viewModel;
    private readonly PaymentService _paymentService;
    private readonly InvoiceService _invoiceService;

    public PaymentListWindow(
        PaymentService paymentService,
        InvoiceService invoiceService)
    {
        InitializeComponent();

        _paymentService = paymentService;
        _invoiceService = invoiceService;
        _viewModel = new PaymentListViewModel(paymentService);
        DataContext = _viewModel;

        Loaded += PaymentListWindow_Loaded;
    }

    private async void PaymentListWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadAsync();
    }

    private async void SearchButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.CurrentPage = 1;
        await _viewModel.LoadAsync();
    }

    private async void ReloadButton_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadAsync();
    }

    private async void ResetButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.ResetSearch();
        _viewModel.CurrentPage = 1;
        await _viewModel.LoadAsync();
    }

    private async void PrevPageButton_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.MovePrevPageAsync();
    }

    private async void NextPageButton_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.MoveNextPageAsync();
    }

    private async void CreateButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var window = new PaymentCreateWindow(_paymentService, _invoiceService)
            {
                Owner = this
            };

            var result = window.ShowDialog();
            if (result == true)
            {
                await _viewModel.LoadAsync();
            }
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(
                $"入金登録画面を開けませんでした。\n\n{ex.Message}",
                "エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void DetailButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not PaymentListItemDto payment)
            return;

        try
        {
            var window = new PaymentDetailWindow(payment.Id, _paymentService, _invoiceService)
            {
                Owner = this
            };

            window.ShowDialog();
            await _viewModel.LoadAsync();
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(
                $"入金詳細画面を開けませんでした。\n\n{ex.Message}",
                "エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
