using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Services;
using InvoiceSystem.Wpf.ViewModels;
using System.Windows;

namespace InvoiceSystem.Wpf.Views;

public partial class AdminInvoiceEditWindow : Window
{
    private readonly AdminInvoiceEditViewModel _viewModel;

    public AdminInvoiceEditWindow(InvoiceService invoiceService)
    {
        InitializeComponent();

        _viewModel = new AdminInvoiceEditViewModel(
            invoiceService,
            InvoiceEditorMode.New);
        DataContext = _viewModel;

        Loaded += AdminInvoiceEditWindow_Loaded;
    }

    public AdminInvoiceEditWindow(InvoiceService invoiceService, long invoiceId)
    {
        InitializeComponent();

        _viewModel = new AdminInvoiceEditViewModel(
            invoiceService,
            InvoiceEditorMode.Edit,
            invoiceId);
        DataContext = _viewModel;

        Loaded += AdminInvoiceEditWindow_Loaded;
    }

    private async void AdminInvoiceEditWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadAsync();
    }

    private void AddLineButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.AddLine();
    }

    private void RemoveLineButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.RemoveSelectedLine();
    }

    private void MoveUpButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.MoveSelectedLineUp();
    }

    private void MoveDownButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.MoveSelectedLineDown();
    }

    private async void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        var ok = await _viewModel.SaveAsync();
        if (!ok) return;

        MessageBox.Show(
            _viewModel.IsNewMode ? "請求書を作成しました。" : "請求書を更新しました。",
            _viewModel.IsNewMode ? "作成完了" : "更新完了",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        DialogResult = true;
        Close();
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}