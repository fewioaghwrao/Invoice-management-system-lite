using Avalonia.Controls;
using Avalonia.Interactivity;
using InvoiceAvaloniaClient.Helpers;
using InvoiceAvaloniaClient.ViewModels;

namespace InvoiceAvaloniaClient.Views;

/// <summary>
/// 入金登録（手動）（管理者用シェル内に表示）。DataContext は PaymentCreateViewModel。
/// 登録後は、その入金の詳細（割当）画面へ進む。
/// </summary>
public partial class AdminPaymentCreateView : UserControl
{
    private readonly PaymentCreateViewModel _viewModel = null!;
    private readonly IAdminNavigator _navigator = null!;
    private bool _initialized;

    /// <summary>デザイナー用。</summary>
    public AdminPaymentCreateView()
    {
        InitializeComponent();
    }

    public AdminPaymentCreateView(PaymentCreateViewModel viewModel, IAdminNavigator navigator)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _navigator = navigator;
        DataContext = _viewModel;

        Loaded += async (_, _) =>
        {
            if (_initialized) return;
            _initialized = true;
            await _viewModel.InitializeAsync();
        };
    }

    private void BreadcrumbDashboard_OnClick(object? sender, RoutedEventArgs e) => _navigator.GoToDashboard();

    private void BackToListButton_OnClick(object? sender, RoutedEventArgs e) => _navigator.GoToPaymentList();

    private void PayerNameTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        // 利用者が自分で入力したときだけ「編集済み」にする（会員選択による自動入力は除く）
        if (sender is TextBox { IsKeyboardFocusWithin: true })
        {
            _viewModel.MarkPayerNameTouched();
        }
    }

    private async void SaveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var validationError = _viewModel.Validate();

            if (!string.IsNullOrWhiteSpace(validationError))
            {
                await DialogService.ShowErrorAsync(validationError, "入力エラー");
                return;
            }

            var confirmed = await _navigator.ConfirmAsync(
                title: "入金登録確認",
                message: "入金を登録します。続けて割当画面へ進みます。よろしいですか？",
                confirmText: "登録する",
                visualType: ConfirmDialogWindow.DialogVisualType.SaveConfirm);

            if (!confirmed)
                return;

            var createdId = await _viewModel.SaveAsync();
            _navigator.GoToPaymentDetail(createdId);
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync(ex.Message, "登録エラー");
        }
    }
}
