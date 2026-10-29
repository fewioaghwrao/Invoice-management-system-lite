using Avalonia.Controls;
using Avalonia.Interactivity;
using InvoiceAvaloniaClient.ViewModels;

namespace InvoiceAvaloniaClient.Views;

/// <summary>
/// 会員用の請求書詳細（会員用シェル内に表示）。
/// </summary>
public partial class MemberInvoiceDetailView : UserControl
{
    private readonly MemberInvoiceDetailViewModel? _viewModel;
    private bool _initialized;

    /// <summary>デザイナー用。</summary>
    public MemberInvoiceDetailView()
    {
        InitializeComponent();
    }

    public MemberInvoiceDetailView(MemberInvoiceDetailViewModel viewModel, string backText)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;
        BackButton.Content = backText;

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
}
