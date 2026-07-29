using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Services;
using InvoiceSystem.Wpf.ViewModels;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;

namespace InvoiceSystem.Wpf.Views;

public partial class SalesListWindow : Window
{
    private readonly SalesListViewModel _viewModel;
    private readonly SalesService _salesService;

    public SalesListWindow(SalesService salesService, int? memberId = null, string? memberName = null)
    {
        InitializeComponent();

        _salesService = salesService;
        _viewModel = new SalesListViewModel(salesService)
        {
            MemberId = memberId,
            MemberName = memberName ?? string.Empty
        };

        DataContext = _viewModel;
        Loaded += SalesListWindow_Loaded;
    }

    private async void SalesListWindow_Loaded(object sender, RoutedEventArgs e)
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

    private async void ReloadButton_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadAsync();
    }

    private async void PrevPageButton_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.MovePreviousPageAsync();
    }

    private async void NextPageButton_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.MoveNextPageAsync();
    }

    private void OpenByMemberButton_OnClick(object sender, RoutedEventArgs e)
    {
        var window = new SalesByMemberWindow(_salesService)
        {
            Owner = this
        };

        window.ShowDialog();
    }

    private async void ExportCsvButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var request = new SalesSearchRequest
            {
                Year = _viewModel.SelectedYear,
                Month = _viewModel.SelectedMonth?.Value,
                Status = _viewModel.SelectedStatus?.Value ?? "all",
                Keyword = _viewModel.Keyword?.Trim() ?? string.Empty,
                Page = 1,
                PageSize = _viewModel.PageSize,
                MemberId = _viewModel.MemberId
            };

            var dialog = new SaveFileDialog
            {
                Filter = "CSVファイル (*.csv)|*.csv",
                FileName = $"sales_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (dialog.ShowDialog() != true)
                return;

            var bytes = await _salesService.ExportSalesCsvAsync(request);
            await File.WriteAllBytesAsync(dialog.FileName, bytes);

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