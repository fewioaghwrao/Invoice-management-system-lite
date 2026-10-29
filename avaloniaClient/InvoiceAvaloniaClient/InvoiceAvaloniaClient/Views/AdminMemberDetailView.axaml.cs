using Avalonia.Controls;
using Avalonia.Interactivity;
using InvoiceAvaloniaClient.ViewModels;

namespace InvoiceAvaloniaClient.Views;

/// <summary>
/// 会員詳細・編集（管理者用シェル内に表示）。DataContext は MemberDetailViewModel。
/// 保存・退会・戻るは ViewModel のコマンドで行い、終わったら一覧へ戻る（シェルが渡したコールバック）。
/// </summary>
public partial class AdminMemberDetailView : UserControl
{
    private readonly MemberDetailViewModel _viewModel = null!;
    private readonly IAdminNavigator _navigator = null!;
    private bool _initialized;

    /// <summary>デザイナー用。</summary>
    public AdminMemberDetailView()
    {
        InitializeComponent();
    }

    public AdminMemberDetailView(MemberDetailViewModel viewModel, IAdminNavigator navigator)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _navigator = navigator;
        DataContext = _viewModel;

        Loaded += async (_, _) =>
        {
            if (_initialized) return;
            _initialized = true;
            await _viewModel.LoadAsync();
        };
    }

    private void BreadcrumbDashboard_OnClick(object? sender, RoutedEventArgs e) => _navigator.GoToDashboard();
}
