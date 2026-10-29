using Avalonia.Controls;
using Avalonia.Interactivity;
using InvoiceAvaloniaClient.ViewModels;

namespace InvoiceAvaloniaClient.Views;

/// <summary>
/// 会員用の請求書一覧（会員用シェル内に表示）。
/// </summary>
public partial class MemberInvoiceListView : UserControl
{
    private readonly MemberInvoiceListViewModel? _viewModel;
    private bool _initialized;

    /// <summary>デザイナー用。</summary>
    public MemberInvoiceListView()
    {
        InitializeComponent();
    }

    public MemberInvoiceListView(MemberInvoiceListViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
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
}
