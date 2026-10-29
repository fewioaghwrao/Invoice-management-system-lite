using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using InvoiceAvaloniaClient.Helpers;
using InvoiceAvaloniaClient.Models;
using InvoiceAvaloniaClient.ViewModels;

namespace InvoiceAvaloniaClient.Views;

/// <summary>
/// 入金詳細（割当）（管理者用シェル内に表示）。DataContext は PaymentDetailViewModel。
/// 各行の入力欄に請求書番号を入れて検索 → 候補から「選択」でその行に設定する。
/// </summary>
public partial class AdminPaymentDetailView : UserControl
{
    private readonly PaymentDetailViewModel _viewModel = null!;
    private readonly IAdminNavigator _navigator = null!;
    private bool _initialized;

    /// <summary>候補を検索した行（「選択」でこの行に設定する）。</summary>
    private PaymentAllocationEditRow? _searchingRow;

    /// <summary>デザイナー用。</summary>
    public AdminPaymentDetailView()
    {
        InitializeComponent();
    }

    public AdminPaymentDetailView(PaymentDetailViewModel viewModel, IAdminNavigator navigator)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _navigator = navigator;
        DataContext = _viewModel;

        Loaded += async (_, _) =>
        {
            if (_initialized) return;
            _initialized = true;
            await _viewModel.LoadAsync();
        };
    }

    private void BreadcrumbDashboard_OnClick(object? sender, RoutedEventArgs e) => _navigator.GoToDashboard();

    private void BackToListButton_OnClick(object? sender, RoutedEventArgs e) => _navigator.GoToPaymentList();

    // ===== 割当行 =====

    private void AddRowButton_OnClick(object? sender, RoutedEventArgs e) => _viewModel.AddRow();

    private void RemoveRowButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (RowOf(sender) is { } row)
        {
            if (ReferenceEquals(row, _searchingRow))
            {
                CloseCandidates();
            }

            _viewModel.RemoveRow(row);
        }
    }

    private void AmountTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        // 割当行はプロパティ変更通知を持たないため、入力中合計はここで更新する
        // （入力値が行に反映された後に計算するため、少し後で実行する）
        Dispatcher.UIThread.Post(() => _viewModel.NotifyAllocationSummaryChanged());
    }

    private async void SearchOrClearButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (RowOf(sender) is not { } row)
        {
            return;
        }

        if (row.InvoiceId.HasValue)
        {
            _viewModel.ClearRowInvoice(row);
            RefreshRows();
            return;
        }

        await SearchForRowAsync(row);
    }

    private async void InvoiceSearchBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || RowOf(sender) is not { } row || row.InvoiceId.HasValue)
        {
            return;
        }

        e.Handled = true;
        await SearchForRowAsync(row);
    }

    private async Task SearchForRowAsync(PaymentAllocationEditRow row)
    {
        _searchingRow = row;

        _viewModel.SelectedAllocationRow = row;
        _viewModel.SearchInvoiceNumber = row.InvoiceNumber ?? string.Empty;
        _viewModel.SearchMemberName = string.Empty;

        await _viewModel.SearchCandidatesAsync();

        var index = _viewModel.AllocationRows.IndexOf(row) + 1;
        CandidateTitleText.Text = _viewModel.CandidateInvoices.Count == 0
            ? string.Empty
            : $"検索結果（「選択」で {index} 行目に設定されます）";

        if (_viewModel.CandidateInvoices.Count == 0 && !_viewModel.HasError)
        {
            await DialogService.ShowInfoAsync("該当する請求書が見つかりませんでした。", "請求書の検索");
        }
    }

    private void ApplyCandidateButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { DataContext: InvoiceListItemDto candidate } || _searchingRow is null)
        {
            return;
        }

        _viewModel.SelectedAllocationRow = _searchingRow;
        _viewModel.SelectedCandidate = candidate;
        _viewModel.ApplyCandidateToSelectedRow();

        // 割当金額が空なら、請求金額と未割当額の小さい方を初期値にする（入力の手間を減らすため。変更可）
        if (string.IsNullOrWhiteSpace(_searchingRow.AmountText))
        {
            var suggested = Math.Min(candidate.TotalAmount, Math.Max(0m, _viewModel.Amount - _viewModel.InputAllocatedSum));

            if (suggested > 0)
            {
                _searchingRow.AmountText = suggested.ToString("0");
            }
        }

        CloseCandidates();
        RefreshRows();
        _viewModel.NotifyAllocationSummaryChanged();
    }

    private void CloseCandidatesButton_OnClick(object? sender, RoutedEventArgs e) => CloseCandidates();

    private void CloseCandidates()
    {
        _searchingRow = null;
        _viewModel.CandidateInvoices.Clear();
    }

    /// <summary>
    /// 割当行は変更通知を持たないため、行の表示を作り直す
    /// （WPF 版の CollectionViewSource.Refresh の代わりに ItemsSource を差し直す）。
    /// </summary>
    private void RefreshRows()
    {
        AllocationItems.ItemsSource = null;
        AllocationItems.ItemsSource = _viewModel.AllocationRows;
    }

    private static PaymentAllocationEditRow? RowOf(object? sender)
        => (sender as Control)?.DataContext as PaymentAllocationEditRow;

    // ===== 保存 =====

    private async void SaveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var validation = _viewModel.Validate();

            if (!string.IsNullOrWhiteSpace(validation))
            {
                await DialogService.ShowErrorAsync(validation, "入力エラー");
                return;
            }

            var confirmed = await _navigator.ConfirmAsync(
                title: "割当保存確認",
                message: "入金割当の内容を保存します。よろしいですか？",
                confirmText: "保存する",
                visualType: ConfirmDialogWindow.DialogVisualType.SaveConfirm,
                subMessage: "保存後は現在の割当内容が反映されます。");

            if (!confirmed)
                return;

            CloseCandidates();

            await _viewModel.SaveAsync();
            RefreshRows();

            await DialogService.ShowInfoAsync("保存しました。", "完了");
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync(ex.Message, "保存エラー");
        }
    }
}
