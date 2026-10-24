using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace InvoiceSystem.Wpf.Views;

/// <summary>
/// 請求書一覧（管理者用シェル内に表示）。DataContext は InvoiceListViewModel。
/// シェルがこのインスタンスを保持するため、詳細から戻っても検索条件が残る。
/// </summary>
public partial class AdminInvoiceListView : UserControl
{
    private readonly InvoiceListViewModel _viewModel;
    private readonly IAdminNavigator _navigator;

    public AdminInvoiceListView(InvoiceListViewModel viewModel, IAdminNavigator navigator)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _navigator = navigator;
        DataContext = _viewModel;

        if (_viewModel.SelectedStatus is null && _viewModel.StatusOptions.Count > 0)
        {
            _viewModel.SelectedStatus = _viewModel.StatusOptions[0];
        }
    }

    /// <summary>現在の条件・ページで読み込み直す（シェルから表示のたびに呼ばれる）。</summary>
    public Task ReloadAsync() => _viewModel.LoadAsync();

    private async Task SearchFromFirstPageAsync()
    {
        _viewModel.CurrentPage = 1;
        await _viewModel.LoadAsync();
    }

    private async void SearchButton_OnClick(object sender, RoutedEventArgs e) => await SearchFromFirstPageAsync();

    private async void SearchField_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await SearchFromFirstPageAsync();
        }
    }

    private async void ResetButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.ResetSearch();
        await SearchFromFirstPageAsync();
    }

    /// <summary>クイック検索「FIX」: 請求番号に FIX を含むデモ見本データ。</summary>
    private async void QuickFixButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.ResetSearch();
        _viewModel.InvoiceNumber = "FIX";
        await SearchFromFirstPageAsync();
    }

    /// <summary>クイック検索「OVERDUE」: ステータスが期限超過のもの。</summary>
    private async void QuickOverdueButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.ResetSearch();
        _viewModel.SelectedStatus = _viewModel.StatusOptions.FirstOrDefault(x => x.Name == "期限超過")
                                    ?? _viewModel.SelectedStatus;
        await SearchFromFirstPageAsync();
    }

    private async void PrevPageButton_OnClick(object sender, RoutedEventArgs e) => await _viewModel.MovePrevPageAsync();

    private async void NextPageButton_OnClick(object sender, RoutedEventArgs e) => await _viewModel.MoveNextPageAsync();

    private void CreateButton_OnClick(object sender, RoutedEventArgs e) => _navigator.GoToInvoiceEditor(null);

    private void DetailButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: InvoiceListItemDto invoice })
        {
            _navigator.GoToInvoiceDetail(invoice.Id);
        }
    }

    private void RemindButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: InvoiceListItemDto invoice } && invoice.CanRemind)
        {
            _navigator.GoToCollection(invoice.Id);
        }
    }

    private void BreadcrumbDashboard_OnClick(object sender, RoutedEventArgs e) => _navigator.GoToDashboard();
}
