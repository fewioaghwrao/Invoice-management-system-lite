using InvoiceSystem.Wpf.Services;
using InvoiceSystem.Wpf.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace InvoiceSystem.Wpf.Views;

public partial class PaymentCreateWindow : Window
{
    private readonly PaymentCreateViewModel _viewModel;
    private readonly PaymentService _paymentService;
    private readonly InvoiceService _invoiceService;

    public long CreatedPaymentId { get; private set; }

    public PaymentCreateWindow(
        PaymentService paymentService,
        InvoiceService invoiceService)
    {
        InitializeComponent();

        _paymentService = paymentService;
        _invoiceService = invoiceService;
        _viewModel = new PaymentCreateViewModel(paymentService);
        DataContext = _viewModel;

        Loaded += PaymentCreateWindow_Loaded;
    }

    private async void PaymentCreateWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();
    }

    private void PayerNameTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        _viewModel.MarkPayerNameTouched();
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BackButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var validationError = _viewModel.Validate();
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                MessageBox.Show(
                    validationError,
                    "入力エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var ok = MessageBox.Show(
                "入金を登録します。続けて詳細へ進みます。よろしいですか？",
                "入金登録確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (ok != MessageBoxResult.Yes)
                return;

            var createdId = await _viewModel.SaveAsync();
            CreatedPaymentId = createdId;

            DialogResult = true;
            Close();

            var detailWindow = new PaymentDetailWindow(createdId, _paymentService, _invoiceService)
            {
                Owner = Owner
            };
            detailWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "登録エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}