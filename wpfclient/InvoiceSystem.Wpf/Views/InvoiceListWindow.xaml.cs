using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Services;
using InvoiceSystem.Wpf.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace InvoiceSystem.Wpf.Views;

public partial class InvoiceListWindow : Window
{
    private readonly InvoiceService _invoiceService;
    private readonly InvoiceListViewModel _viewModel;

    public InvoiceListWindow(InvoiceService invoiceService)
    {
        InitializeComponent();

        _invoiceService = invoiceService;
        _viewModel = new InvoiceListViewModel(invoiceService);
        DataContext = _viewModel;

        Loaded += InvoiceListWindow_Loaded;
    }

    private async void InvoiceListWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedStatus is null && _viewModel.StatusOptions.Count > 0)
        {
            _viewModel.SelectedStatus = _viewModel.StatusOptions[0];
        }

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

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void DetailButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is InvoiceListItemDto invoice)
        {
            OpenDetail(invoice);
        }
    }

    private async void RemindButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not InvoiceListItemDto invoice)
            return;

        if (!invoice.CanRemind)
            return;

        try
        {
            var window = new AdminCollectionWindow(_invoiceService, invoice.Id)
            {
                Owner = this
            };

            window.ShowDialog();

            await _viewModel.LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"催促画面を開けませんでした。\n\n{ex.Message}",
                "エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void InvoiceGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.SelectedInvoice is not null)
        {
            OpenDetail(_viewModel.SelectedInvoice);
        }
    }

    private void OpenDetail(InvoiceListItemDto invoice)
    {
        try
        {
            var window = new AdminInvoiceDetailWindow(_invoiceService, invoice.Id)
            {
                Owner = this
            };

            window.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"請求書詳細画面を開けませんでした。\n\n{ex.Message}",
                "エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
    private void CreateButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var window = new AdminInvoiceEditWindow(_invoiceService)
            {
                Owner = this
            };

            var result = window.ShowDialog();
            if (result == true)
            {
                _ = _viewModel.LoadAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"請求書作成画面を開けませんでした。\n\n{ex.Message}",
                "エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}