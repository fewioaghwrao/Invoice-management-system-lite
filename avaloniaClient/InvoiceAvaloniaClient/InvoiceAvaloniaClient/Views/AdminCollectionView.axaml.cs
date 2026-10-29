using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using InvoiceAvaloniaClient.Helpers;
using InvoiceAvaloniaClient.ViewModels;

namespace InvoiceAvaloniaClient.Views;

/// <summary>
/// 督促画面（管理者用シェル内に表示）。DataContext は AdminCollectionViewModel。
/// 本文コピー・督促記録の前に確認ダイアログを出し、結果をメッセージで知らせる。
/// </summary>
public partial class AdminCollectionView : UserControl
{
    private readonly AdminCollectionViewModel _viewModel = null!;
    private readonly IAdminNavigator _navigator = null!;
    private readonly long _invoiceId;
    private bool _initialized;

    /// <summary>デザイナー用。</summary>
    public AdminCollectionView()
    {
        InitializeComponent();
    }

    public AdminCollectionView(AdminCollectionViewModel viewModel, long invoiceId, IAdminNavigator navigator)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _invoiceId = invoiceId;
        _navigator = navigator;
        DataContext = _viewModel;

        Loaded += async (_, _) =>
        {
            if (_initialized) return;
            _initialized = true;
            await LoadSafelyAsync();
        };
    }

    private void BreadcrumbDashboard_OnClick(object? sender, RoutedEventArgs e) => _navigator.GoToDashboard();

    private void BreadcrumbList_OnClick(object? sender, RoutedEventArgs e) => _navigator.GoToInvoiceList();

    private void BackToDetailButton_OnClick(object? sender, RoutedEventArgs e) => _navigator.GoToInvoiceDetail(_invoiceId);

    private async void CopyBodyButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_viewModel.BodyPreview))
        {
            await DialogService.ShowInfoAsync("コピー対象の本文がありません。", "本文コピー");
            return;
        }

        var confirmed = await _navigator.ConfirmAsync(
            title: "催促本文コピー",
            message: "催促本文をクリップボードにコピーしますか？",
            confirmText: "コピーする",
            visualType: ConfirmDialogWindow.DialogVisualType.Default,
            subMessage: "コピー後はメール文面やメモへ貼り付けて利用できます。");

        if (!confirmed)
        {
            return;
        }

        try
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard
                            ?? throw new InvalidOperationException("クリップボードを利用できません。");

            await clipboard.SetTextAsync(_viewModel.BodyPreview);

            await DialogService.ShowInfoAsync("本文をコピーしました。", "本文コピー");
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync(
                $"本文のコピーに失敗しました。{Environment.NewLine}{ex.Message}",
                "コピーエラー");
        }
    }

    private async void RecordButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.RemainingAmount <= 0)
        {
            await DialogService.ShowInfoAsync("未回収残額が0円のため、催促記録は不要です。", "催促記録");
            return;
        }

        var detailMessage =
            $"請求書：{_viewModel.InvoiceNumber}{Environment.NewLine}" +
            $"チャネル：{_viewModel.SelectedChannelLabel}{Environment.NewLine}" +
            $"トーン：{_viewModel.SelectedToneLabel}{Environment.NewLine}" +
            $"次回アクション日：{_viewModel.NextActionDateTextForDialog}{Environment.NewLine}" +
            $"未回収残額：{_viewModel.RemainingText}";

        var confirmed = await _navigator.ConfirmAsync(
            title: "催促記録確認",
            message: "催促を実施済みとして記録します。よろしいですか？",
            confirmText: "記録する",
            visualType: ConfirmDialogWindow.DialogVisualType.Default,
            subMessage: detailMessage);

        if (!confirmed)
        {
            return;
        }

        var success = await _viewModel.RecordAsync();

        if (!success)
        {
            await DialogService.ShowErrorAsync(
                string.IsNullOrWhiteSpace(_viewModel.ErrorMessage)
                    ? "催促履歴の登録に失敗しました。"
                    : _viewModel.ErrorMessage,
                "催促記録エラー");
            return;
        }

        await DialogService.ShowInfoAsync("催促履歴に記録しました。", "催促記録");
    }

    private async Task LoadSafelyAsync()
    {
        try
        {
            await _viewModel.LoadAsync();
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync(
                $"催促画面の読み込みに失敗しました。{Environment.NewLine}{ex.Message}",
                "読込エラー");
        }
    }
}
