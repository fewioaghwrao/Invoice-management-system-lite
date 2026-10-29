using Avalonia.Controls;
using Avalonia.Interactivity;
using InvoiceAvaloniaClient.Helpers;
using InvoiceAvaloniaClient.Models;
using InvoiceAvaloniaClient.ViewModels;

namespace InvoiceAvaloniaClient.Views;

/// <summary>
/// 請求書の作成 / 編集（管理者用シェル内に表示）。DataContext は AdminInvoiceEditViewModel。
/// 新規作成のときは一覧へ、編集のときは詳細へ戻る。
/// </summary>
public partial class AdminInvoiceEditView : UserControl
{
    private readonly AdminInvoiceEditViewModel _viewModel = null!;
    private readonly IAdminNavigator _navigator = null!;
    private readonly long? _invoiceId;
    private bool _initialized;

    /// <summary>デザイナー用。</summary>
    public AdminInvoiceEditView()
    {
        InitializeComponent();
    }

    public AdminInvoiceEditView(AdminInvoiceEditViewModel viewModel, long? invoiceId, IAdminNavigator navigator)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _invoiceId = invoiceId;
        _navigator = navigator;
        DataContext = _viewModel;

        BackButton.Content = invoiceId is null ? "← 一覧へ" : "← 詳細へ";

        Loaded += async (_, _) =>
        {
            if (_initialized) return;
            _initialized = true;
            await _viewModel.LoadAsync();
            InvoiceNumberTextBox.Focus();
        };
    }

    private void GoBack()
    {
        if (_invoiceId is long id)
        {
            _navigator.GoToInvoiceDetail(id);
        }
        else
        {
            _navigator.GoToInvoiceList();
        }
    }

    private void BackButton_OnClick(object? sender, RoutedEventArgs e) => GoBack();

    private void BreadcrumbDashboard_OnClick(object? sender, RoutedEventArgs e) => _navigator.GoToDashboard();

    private void BreadcrumbList_OnClick(object? sender, RoutedEventArgs e) => _navigator.GoToInvoiceList();

    private void AddLineButton_OnClick(object? sender, RoutedEventArgs e) => _viewModel.AddLine();

    private void RemoveLineButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (SelectLineFrom(sender))
        {
            _viewModel.RemoveSelectedLine();
        }
    }

    private void MoveUpButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (SelectLineFrom(sender))
        {
            _viewModel.MoveSelectedLineUp();
        }
    }

    private void MoveDownButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (SelectLineFrom(sender))
        {
            _viewModel.MoveSelectedLineDown();
        }
    }

    /// <summary>押されたボタンの行を ViewModel の選択行にする（行操作は選択行に対して行われるため）。</summary>
    private bool SelectLineFrom(object? sender)
    {
        if (sender is Control { DataContext: EditableInvoiceLineItem line })
        {
            _viewModel.SelectedLine = line;
            return true;
        }

        return false;
    }

    private async void SaveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var ok = await _viewModel.SaveAsync();
        if (!ok) return;

        await DialogService.ShowInfoAsync(
            _viewModel.IsNewMode ? "請求書を作成しました。" : "請求書を更新しました。",
            _viewModel.IsNewMode ? "作成完了" : "更新完了");

        GoBack();
    }
}
