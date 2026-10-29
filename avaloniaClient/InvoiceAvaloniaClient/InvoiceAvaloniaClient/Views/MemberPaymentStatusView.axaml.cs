using Avalonia.Controls;
using Avalonia.Interactivity;
using InvoiceAvaloniaClient.ViewModels;

namespace InvoiceAvaloniaClient.Views;

/// <summary>
/// 会員用の未入金の確認（会員用シェル内に表示）。
/// </summary>
public partial class MemberPaymentStatusView : UserControl
{
    private readonly MemberPaymentStatusViewModel? _viewModel;
    private readonly Action? _openInvoiceList;
    private bool _initialized;

    /// <summary>デザイナー用。</summary>
    public MemberPaymentStatusView()
    {
        InitializeComponent();
    }

    public MemberPaymentStatusView(MemberPaymentStatusViewModel viewModel, Action openInvoiceList)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _openInvoiceList = openInvoiceList;
        DataContext = _viewModel;

        Loaded += View_OnLoaded;
    }

    private async void View_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_initialized || _viewModel is null)
        {
            return;
        }

        _initialized = true;
        await _viewModel.InitializeAsync();
    }

    private void OpenInvoiceListButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _openInvoiceList?.Invoke();
    }
}
