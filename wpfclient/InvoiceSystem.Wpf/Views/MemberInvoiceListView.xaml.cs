using InvoiceSystem.Wpf.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace InvoiceSystem.Wpf.Views;

/// <summary>
/// 会員用の請求書一覧（会員用シェル内に表示）。
/// </summary>
public partial class MemberInvoiceListView : UserControl
{
    private readonly MemberInvoiceListViewModel _viewModel;
    private bool _initialized;

    public MemberInvoiceListView(MemberInvoiceListViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private async void View_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        await _viewModel.InitializeAsync();
    }
}
