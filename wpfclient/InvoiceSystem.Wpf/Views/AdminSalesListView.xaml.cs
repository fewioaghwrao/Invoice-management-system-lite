using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Services;
using InvoiceSystem.Wpf.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace InvoiceSystem.Wpf.Views;

/// <summary>
/// 売上一覧（請求ベース）（管理者用シェル内に表示）。DataContext は SalesListViewModel。
/// シェルがこのインスタンスを保持するため、他の画面から戻っても条件が残る。
/// </summary>
public partial class AdminSalesListView : UserControl
{
    private readonly SalesListViewModel _viewModel;
    private readonly SalesService _salesService;
    private readonly IAdminNavigator _navigator;
    private readonly ObservableCollection<int> _yearOptions = new();

    public AdminSalesListView(SalesListViewModel viewModel, SalesService salesService, IAdminNavigator navigator)
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

    /// <summary>顧客・年で絞り込む（ダッシュボード・顧客別集計から開いた場合）。</summary>
    public void ApplyFilter(int? memberId, string? memberName, int? year)
    {
        _viewModel.MemberId = memberId;
        _viewModel.MemberName = memberName ?? string.Empty;

        if (year.HasValue)
        {
            _viewModel.SelectedYear = year.Value;
            RefreshYearOptions();
        }

        _viewModel.CurrentPage = 1;
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
    }

    private async void SearchButton_OnClick(object sender, RoutedEventArgs e) => await _viewModel.SearchAsync();

    private async void KeywordTextBox_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await _viewModel.SearchAsync();
        }
    }

    private async void ResetButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.SelectedYear = DateTime.Today.Year;
        _viewModel.SelectedMonth = _viewModel.MonthOptions.FirstOrDefault();
        _viewModel.SelectedStatus = _viewModel.StatusOptions.FirstOrDefault();
        _viewModel.Keyword = string.Empty;
        _viewModel.MemberId = null;
        _viewModel.MemberName = string.Empty;
        RefreshYearOptions();

        await _viewModel.SearchAsync();
    }

    private async void ClearMemberFilterButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.MemberId = null;
        _viewModel.MemberName = string.Empty;
        await _viewModel.SearchAsync();
    }

    private async void PrevPageButton_OnClick(object sender, RoutedEventArgs e) => await _viewModel.MovePreviousPageAsync();

    private async void NextPageButton_OnClick(object sender, RoutedEventArgs e) => await _viewModel.MoveNextPageAsync();

    private void OpenByMemberButton_OnClick(object sender, RoutedEventArgs e) => _navigator.GoToSalesByMember(_viewModel.SelectedYear);

    private void InvoiceLinkButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: SalesListItemDto row } && row.InvoiceId > 0)
        {
            _navigator.GoToInvoiceDetail(row.InvoiceId);
        }
    }

    private void BreadcrumbDashboard_OnClick(object sender, RoutedEventArgs e) => _navigator.GoToDashboard();

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

            if (dialog.ShowDialog(Window.GetWindow(this)) != true)
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
