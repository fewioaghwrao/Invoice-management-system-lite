using InvoiceSystem.Wpf.ViewModels;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace InvoiceSystem.Wpf.Views;

/// <summary>
/// 督促画面（管理者用シェル内に表示）。DataContext は AdminCollectionViewModel。
/// 本文コピー・督促記録の前に確認ダイアログを出し、結果をメッセージで知らせる。
/// </summary>
public partial class AdminCollectionView : UserControl
{
    private readonly AdminCollectionViewModel _viewModel;
    private readonly IAdminNavigator _navigator;
    private readonly long _invoiceId;
    private bool _initialized;

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

    private void BreadcrumbDashboard_OnClick(object sender, RoutedEventArgs e) => _navigator.GoToDashboard();

    private void BreadcrumbList_OnClick(object sender, RoutedEventArgs e) => _navigator.GoToInvoiceList();

    private void BackToDetailButton_OnClick(object sender, RoutedEventArgs e) => _navigator.GoToInvoiceDetail(_invoiceId);

    private void CopyBodyButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_viewModel.BodyPreview))
        {
            MessageBox.Show(
                "コピー対象の本文がありません。",
                "本文コピー",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var confirmed = _navigator.Confirm(
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
            Clipboard.SetText(_viewModel.BodyPreview);

            MessageBox.Show(
                "本文をコピーしました。",
                "本文コピー",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"本文のコピーに失敗しました。{Environment.NewLine}{ex.Message}",
                "コピーエラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void RecordButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.RemainingAmount <= 0)
        {
            MessageBox.Show(
                "未回収残額が0円のため、催促記録は不要です。",
                "催促記録",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var detailMessage =
            $"請求書：{_viewModel.InvoiceNumber}{Environment.NewLine}" +
            $"チャネル：{_viewModel.SelectedChannelLabel}{Environment.NewLine}" +
            $"トーン：{_viewModel.SelectedToneLabel}{Environment.NewLine}" +
            $"次回アクション日：{_viewModel.NextActionDateTextForDialog}{Environment.NewLine}" +
            $"未回収残額：{_viewModel.RemainingText}";

        var confirmed = _navigator.Confirm(
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
            MessageBox.Show(
                string.IsNullOrWhiteSpace(_viewModel.ErrorMessage)
                    ? "催促履歴の登録に失敗しました。"
                    : _viewModel.ErrorMessage,
                "催促記録エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        MessageBox.Show(
            "催促履歴に記録しました。",
            "催促記録",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private async Task LoadSafelyAsync()
    {
        try
        {
            await _viewModel.LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"催促画面の読み込みに失敗しました。{Environment.NewLine}{ex.Message}",
                "読込エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
