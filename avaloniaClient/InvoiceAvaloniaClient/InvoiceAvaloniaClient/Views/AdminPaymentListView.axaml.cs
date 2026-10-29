using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using InvoiceAvaloniaClient.Models;
using InvoiceAvaloniaClient.ViewModels;

namespace InvoiceAvaloniaClient.Views;

/// <summary>
/// 入金一覧（管理者用シェル内に表示）。DataContext は PaymentListViewModel。
/// シェルがこのインスタンスを保持するため、詳細から戻っても検索条件が残る。
/// </summary>
public partial class AdminPaymentListView : UserControl
{
    private readonly PaymentListViewModel _viewModel = null!;
    private readonly IAdminNavigator _navigator = null!;

    /// <summary>デザイナー用。</summary>
    public AdminPaymentListView()
    {
        InitializeComponent();
    }

    public AdminPaymentListView(PaymentListViewModel viewModel, IAdminNavigator navigator)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _navigator = navigator;
        DataContext = _viewModel;
    }

    /// <summary>現在の条件・ページで読み込み直す（シェルから表示のたびに呼ばれる）。</summary>
    public Task ReloadAsync() => _viewModel.LoadAsync();

    private async Task SearchFromFirstPageAsync()
    {
        _viewModel.CurrentPage = 1;
        await _viewModel.LoadAsync();
    }

    private async void SearchButton_OnClick(object? sender, RoutedEventArgs e) => await SearchFromFirstPageAsync();

    private async void KeywordTextBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await SearchFromFirstPageAsync();
        }
    }

    private async void ResetButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _viewModel.ResetSearch();
        await SearchFromFirstPageAsync();
    }

    private async void PrevPageButton_OnClick(object? sender, RoutedEventArgs e) => await _viewModel.MovePrevPageAsync();

    private async void NextPageButton_OnClick(object? sender, RoutedEventArgs e) => await _viewModel.MoveNextPageAsync();

    private void CreateButton_OnClick(object? sender, RoutedEventArgs e) => _navigator.GoToPaymentCreate();

    private void DetailButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: PaymentListItemDto payment })
        {
            _navigator.GoToPaymentDetail(payment.Id);
        }
    }

    /// <summary>請求書番号のリンク: 割当先の請求書（複数ある場合は先頭）の詳細へ。</summary>
    private void InvoiceLinkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: PaymentListItemDto payment })
        {
            var invoice = payment.Invoices?.FirstOrDefault(x => x.Id > 0);

            if (invoice is not null)
            {
                _navigator.GoToInvoiceDetail(invoice.Id);
            }
        }
    }

    private void BreadcrumbDashboard_OnClick(object? sender, RoutedEventArgs e) => _navigator.GoToDashboard();
}
