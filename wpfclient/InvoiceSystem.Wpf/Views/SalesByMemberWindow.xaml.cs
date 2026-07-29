using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Services;
using InvoiceSystem.Wpf.ViewModels;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace InvoiceSystem.Wpf.Views;

public partial class SalesByMemberWindow : Window
{
    private readonly SalesByMemberViewModel _viewModel;
    private readonly SalesService _salesService;

    public SalesByMemberWindow(SalesService salesService)
    {
        InitializeComponent();

        _salesService = salesService;
        _viewModel = new SalesByMemberViewModel(salesService);
        DataContext = _viewModel;

        Loaded += SalesByMemberWindow_Loaded;
    }

    private async void SalesByMemberWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadAsync();
    }

    private async void PreviousYearButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.MovePreviousYear();
        _viewModel.CurrentPage = 1;
        await _viewModel.LoadAsync();
    }

    private async void NextYearButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.MoveNextYear();
        _viewModel.CurrentPage = 1;
        await _viewModel.LoadAsync();
    }

    private async void SearchButton_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.SearchAsync();
    }

    private async void PrevPageButton_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.MovePreviousPageAsync();
    }

    private async void NextPageButton_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.MoveNextPageAsync();
    }

    private void BackToSalesButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SalesByMemberGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid grid ||
            grid.SelectedItem is not SalesByMemberRowDto selected)
        {
            return;
        }

        var window = new SalesListWindow(_salesService, selected.MemberId, selected.MemberName)
        {
            Owner = this.Owner ?? this
        };

        window.ShowDialog();
    }
    private async void ExportCsvButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new SaveFileDialog
            {
                Filter = "CSVファイル (*.csv)|*.csv",
                FileName = $"sales_by_member_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (dialog.ShowDialog() != true)
                return;

            var bytes = await _salesService.ExportSalesByMemberCsvAsync(
                _viewModel.SelectedYear,
                _viewModel.SelectedMonth?.Value,
                _viewModel.Keyword?.Trim() ?? string.Empty);

            await System.IO.File.WriteAllBytesAsync(dialog.FileName, bytes);

            MessageBox.Show("CSVを出力しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"CSV出力に失敗しました。{Environment.NewLine}{ex.Message}",
                "エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
