using InvoiceSystem.Wpf.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace InvoiceSystem.Wpf.Views;

/// <summary>
/// 会員用の請求書詳細（会員用シェル内に表示）。
/// 「戻る」の文言は開いた元の画面（請求書一覧 / 未入金の確認）に合わせて変える。
/// </summary>
public partial class MemberInvoiceDetailView : UserControl
{
    private readonly MemberInvoiceDetailViewModel _viewModel;
    private bool _initialized;

    public MemberInvoiceDetailView(MemberInvoiceDetailViewModel viewModel, string backText)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;
        BackButton.Content = backText;
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
