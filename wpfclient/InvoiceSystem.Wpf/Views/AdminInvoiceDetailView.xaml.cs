using InvoiceSystem.Wpf.ViewModels;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace InvoiceSystem.Wpf.Views;

/// <summary>
/// 請求書詳細（管理者用シェル内に表示）。DataContext は AdminInvoiceDetailViewModel。
/// </summary>
public partial class AdminInvoiceDetailView : UserControl
{
    private readonly AdminInvoiceDetailViewModel _viewModel;
    private readonly IAdminNavigator _navigator;
    private readonly long _invoiceId;
    private bool _initialized;

    public AdminInvoiceDetailView(AdminInvoiceDetailViewModel viewModel, long invoiceId, IAdminNavigator navigator)
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
            await _viewModel.LoadAsync();
        };
    }

    private void BreadcrumbDashboard_OnClick(object sender, RoutedEventArgs e) => _navigator.GoToDashboard();

    private void BackToListButton_OnClick(object sender, RoutedEventArgs e) => _navigator.GoToInvoiceList();

    private void CollectionButton_OnClick(object sender, RoutedEventArgs e) => _navigator.GoToCollection(_invoiceId);

    private void EditButton_OnClick(object sender, RoutedEventArgs e) => _navigator.GoToInvoiceEditor(_invoiceId);

    private void PaymentCreateButton_OnClick(object sender, RoutedEventArgs e) => _navigator.GoToPaymentCreate();

    private async void OpenPdfButton_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.OpenPdfAsync();
    }
}
