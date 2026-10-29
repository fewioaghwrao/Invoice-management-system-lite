using Avalonia.Controls;
using Avalonia.Interactivity;
using InvoiceAvaloniaClient.ViewModels;

namespace InvoiceAvaloniaClient.Views;

/// <summary>
/// 会員一覧（管理者用シェル内に表示）。DataContext は MemberListViewModel。
/// 検索・ページ送り・退会は ViewModel のコマンド、詳細はシェルの画面切り替えで開く。
/// </summary>
public partial class AdminMemberListView : UserControl
{
    private readonly MemberListViewModel _viewModel = null!;
    private readonly IAdminNavigator _navigator = null!;

    /// <summary>デザイナー用。</summary>
    public AdminMemberListView()
    {
        InitializeComponent();
    }

    public AdminMemberListView(MemberListViewModel viewModel, IAdminNavigator navigator)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _navigator = navigator;
        DataContext = _viewModel;
    }

    /// <summary>現在の条件・ページで読み込み直す（シェルから表示のたびに呼ばれる）。</summary>
    public Task ReloadAsync() => _viewModel.LoadAsync();

    private void BreadcrumbDashboard_OnClick(object? sender, RoutedEventArgs e) => _navigator.GoToDashboard();
}
