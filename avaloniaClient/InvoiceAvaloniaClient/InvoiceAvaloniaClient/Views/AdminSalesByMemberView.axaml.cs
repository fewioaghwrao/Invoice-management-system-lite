using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using InvoiceAvaloniaClient.Helpers;
using InvoiceAvaloniaClient.Models;
using InvoiceAvaloniaClient.Services;
using InvoiceAvaloniaClient.ViewModels;

namespace InvoiceAvaloniaClient.Views;

/// <summary>
/// 売上集計（顧客別）（管理者用シェル内に表示）。DataContext は SalesByMemberViewModel。
/// 顧客名を押すと、その顧客で絞り込んだ売上一覧へ移動する。
/// </summary>
public partial class AdminSalesByMemberView : UserControl
{
    private readonly SalesByMemberViewModel _viewModel = null!;
    private readonly SalesService _salesService = null!;
    private readonly IAdminNavigator _navigator = null!;
    private readonly ObservableCollection<int> _yearOptions = new();

    /// <summary>デザイナー用。</summary>
    public AdminSalesByMemberView()
    {
        InitializeComponent();
    }

    public AdminSalesByMemberView(SalesByMemberViewModel viewModel, SalesService salesService, IAdminNavigator navigator)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _salesService = salesService;
        _navigator = navigator;
        DataContext = _viewModel;

        YearComboBox.ItemsSource = _yearOptions;
        RefreshYearOptions();
    }

    /// <summary>現在の条件・ページで読み込み直す（シェルから表示のたびに呼ばれる）。</summary>
    public Task ReloadAsync() => _viewModel.LoadAsync();

    /// <summary>年を指定して表示する（ダッシュボード・売上一覧から開いた場合）。</summary>
    public void ApplyYear(int year)
    {
        _viewModel.SelectedYear = year;
        _viewModel.CurrentPage = 1;
        RefreshYearOptions();
    }

    /// <summary>年の選択肢: 今年の翌年〜5年前（選択中の年が範囲外なら追加）。</summary>
    private void RefreshYearOptions()
    {
        var thisYear = DateTime.Today.Year;
        var years = Enumerable.Range(thisYear - 5, 7).ToList();

        if (!years.Contains(_viewModel.SelectedYear))
        {
            years.Add(_viewModel.SelectedYear);
        }

        var selected = _viewModel.SelectedYear;

        _yearOptions.Clear();
        foreach (var y in years.OrderByDescending(x => x))
        {
            _yearOptions.Add(y);
        }

        _viewModel.SelectedYear = selected;
        YearComboBox.SelectedItem = selected;
    }

    private async void SearchButton_OnClick(object? sender, RoutedEventArgs e) => await _viewModel.SearchAsync();

    private async void KeywordTextBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await _viewModel.SearchAsync();
        }
    }

    private async void PrevPageButton_OnClick(object? sender, RoutedEventArgs e) => await _viewModel.MovePreviousPageAsync();

    private async void NextPageButton_OnClick(object? sender, RoutedEventArgs e) => await _viewModel.MoveNextPageAsync();

    private void BackToSalesButton_OnClick(object? sender, RoutedEventArgs e) => _navigator.GoToSalesList();

    private void BreadcrumbDashboard_OnClick(object? sender, RoutedEventArgs e) => _navigator.GoToDashboard();

    private void MemberLinkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: SalesByMemberRowDto row })
        {
            _navigator.GoToSalesList(row.MemberId, row.MemberName, _viewModel.SelectedYear);
        }
    }

    private async void ExportCsvButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var path = await DialogService.PickSaveFilePathAsync(
                this,
                "CSVの保存先",
                $"sales_by_member_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                "CSVファイル",
                "csv");

            if (string.IsNullOrEmpty(path))
                return;

            var bytes = await _salesService.ExportSalesByMemberCsvAsync(
                _viewModel.SelectedYear,
                _viewModel.SelectedMonth?.Value,
                _viewModel.Keyword?.Trim() ?? string.Empty);

            await File.WriteAllBytesAsync(path, bytes);

            await DialogService.ShowInfoAsync("CSVを出力しました。", "完了");
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync(
                $"CSV出力に失敗しました。{Environment.NewLine}{ex.Message}",
                "エラー");
        }
    }
}
