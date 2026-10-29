using Avalonia.Controls;
using Avalonia.Interactivity;
using InvoiceAvaloniaClient.Models;
using InvoiceAvaloniaClient.ViewModels;

namespace InvoiceAvaloniaClient.Views;

/// <summary>
/// 管理者ダッシュボード（管理者用シェル内に表示）。DataContext は AdminDashboardViewModel。
/// 他の画面を開く操作はイベントでシェルに通知する。
/// </summary>
public partial class AdminDashboardView : UserControl
{
    private readonly AdminDashboardViewModel? _viewModel;

    /// <summary>未入金一覧の請求番号が押された（引数は請求書 ID）。</summary>
    public event EventHandler<long>? OpenInvoiceRequested;

    /// <summary>未入金一覧の「すべて表示 →」が押された。</summary>
    public event EventHandler? OpenInvoiceListRequested;

    /// <summary>回収率ワースト顧客の顧客名が押された。</summary>
    public event EventHandler<WorstCustomerDto>? OpenMemberSalesRequested;

    /// <summary>「顧客別集計を見る →」が押された。</summary>
    public event EventHandler? OpenSalesByMemberRequested;

    /// <summary>デザイナー用。</summary>
    public AdminDashboardView()
    {
        InitializeComponent();
    }

    public AdminDashboardView(AdminDashboardViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private void QuarterButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: string tag } && int.TryParse(tag, out var quarter))
        {
            _viewModel?.SetQuarter(quarter);
        }
    }

    private void UnpaidInvoiceButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: int invoiceId })
        {
            OpenInvoiceRequested?.Invoke(this, invoiceId);
        }
    }

    private void OpenInvoiceListButton_OnClick(object? sender, RoutedEventArgs e)
    {
        OpenInvoiceListRequested?.Invoke(this, EventArgs.Empty);
    }

    private void WorstCustomerButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: WorstCustomerDto customer })
        {
            OpenMemberSalesRequested?.Invoke(this, customer);
        }
    }

    private void OpenSalesByMemberButton_OnClick(object? sender, RoutedEventArgs e)
    {
        OpenSalesByMemberRequested?.Invoke(this, EventArgs.Empty);
    }
}
