using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Services;
using InvoiceSystem.Wpf.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace InvoiceSystem.Wpf.Views;

/// <summary>
/// 管理者用のシェルウィンドウ。ヘッダー（年の切り替え・ユーザー表示・ログアウト）と左メニューを持ち、
/// 中央の PageHost に各画面を差し替えて表示する。
///   ダッシュボード / 請求書（一覧・詳細・督促・作成/編集） / 入金（一覧・詳細・登録）
///   / 会員（一覧・詳細） / 売上一覧 / 顧客別集計
/// </summary>
public partial class AdminShellWindow : Window, IAdminNavigator
{
    private const string AppName = "Invoice Management System（管理者）";

    private enum AdminSection
    {
        Dashboard,
        Invoices,
        Payments,
        Members,
        Sales,
        SalesByMember
    }

    private readonly AdminDashboardViewModel _dashboardViewModel;
    private readonly AdminDashboardView _dashboardView;
    private readonly AuthService _authService;
    private readonly InvoiceService _invoiceService;
    private readonly MemberService _memberService;
    private readonly SalesService _salesService;
    private readonly PaymentService _paymentService;

    // 一覧系の画面は使い回して、詳細などから戻っても検索条件・ページを保つ
    private AdminInvoiceListView? _invoiceListView;
    private AdminPaymentListView? _paymentListView;
    private AdminMemberListView? _memberListView;
    private AdminSalesListView? _salesListView;
    private AdminSalesByMemberView? _salesByMemberView;

    public AdminShellWindow(
        CurrentUser? currentUser,
        AuthService authService,
        InvoiceService invoiceService,
        AccountService accountService,
        AdminService adminService,
        MemberService memberService,
        SalesService salesService,
        PaymentService paymentService)
    {
        InitializeComponent();

        _authService = authService;
        _invoiceService = invoiceService;
        _memberService = memberService;
        _salesService = salesService;
        _paymentService = paymentService;

        _dashboardViewModel = new AdminDashboardViewModel(adminService, currentUser)
        {
            // グラフの見出しは画面側に表示するため、グラフ内には描かない
            IncludeChartTitles = false
        };

        // ヘッダー（年の切り替え）はダッシュボードの ViewModel を使う
        DataContext = _dashboardViewModel;
        DisplayNameText.Text = string.IsNullOrWhiteSpace(currentUser?.Name) ? "管理者" : currentUser!.Name;

        _dashboardView = new AdminDashboardView(_dashboardViewModel);
        _dashboardView.OpenInvoiceRequested += (_, invoiceId) => GoToInvoiceDetail(invoiceId);
        _dashboardView.OpenInvoiceListRequested += (_, _) => GoToInvoiceList();
        _dashboardView.OpenMemberSalesRequested += (_, member) =>
            GoToSalesList(member.MemberId, member.MemberName, _dashboardViewModel.SelectedYear);
        _dashboardView.OpenSalesByMemberRequested += (_, _) => GoToSalesByMember(_dashboardViewModel.SelectedYear);

        ShowPage(_dashboardView, AdminSection.Dashboard, "ダッシュボード");

        Loaded += async (_, _) => await _dashboardViewModel.LoadAsync();
    }

    // ===== 画面切り替え（IAdminNavigator） =====

    public async void GoToDashboard()
    {
        ShowPage(_dashboardView, AdminSection.Dashboard, "ダッシュボード");
        await _dashboardViewModel.LoadAsync();
    }

    // --- 請求書 ---

    public async void GoToInvoiceList()
    {
        _invoiceListView ??= new AdminInvoiceListView(new InvoiceListViewModel(_invoiceService), this);

        ShowPage(_invoiceListView, AdminSection.Invoices, "請求書一覧");
        await _invoiceListView.ReloadAsync();
    }

    public void GoToInvoiceDetail(long invoiceId)
    {
        var vm = new AdminInvoiceDetailViewModel(_invoiceService, invoiceId);
        ShowPage(new AdminInvoiceDetailView(vm, invoiceId, this), AdminSection.Invoices, "請求書詳細");
    }

    public void GoToCollection(long invoiceId)
    {
        var vm = new AdminCollectionViewModel(_invoiceService, invoiceId);
        ShowPage(new AdminCollectionView(vm, invoiceId, this), AdminSection.Invoices, "督促");
    }

    public void GoToInvoiceEditor(long? invoiceId)
    {
        var vm = invoiceId is long id
            ? new AdminInvoiceEditViewModel(_invoiceService, InvoiceEditorMode.Edit, id)
            : new AdminInvoiceEditViewModel(_invoiceService, InvoiceEditorMode.New);

        ShowPage(
            new AdminInvoiceEditView(vm, invoiceId, this),
            AdminSection.Invoices,
            invoiceId is null ? "請求書作成" : "請求書編集");
    }

    // --- 入金 ---

    public async void GoToPaymentList()
    {
        _paymentListView ??= new AdminPaymentListView(new PaymentListViewModel(_paymentService), this);

        ShowPage(_paymentListView, AdminSection.Payments, "入金一覧");
        await _paymentListView.ReloadAsync();
    }

    public void GoToPaymentDetail(long paymentId)
    {
        var vm = new PaymentDetailViewModel(paymentId, _paymentService, _invoiceService);
        ShowPage(new AdminPaymentDetailView(vm, this), AdminSection.Payments, "入金詳細");
    }

    public void GoToPaymentCreate()
    {
        var vm = new PaymentCreateViewModel(_paymentService);
        ShowPage(new AdminPaymentCreateView(vm, this), AdminSection.Payments, "入金登録");
    }

    // --- 会員 ---

    public async void GoToMemberList()
    {
        _memberListView ??= new AdminMemberListView(
            new MemberListViewModel(_memberService, openDetail: GoToMemberDetail),
            this);

        ShowPage(_memberListView, AdminSection.Members, "会員一覧");
        await _memberListView.ReloadAsync();
    }

    public void GoToMemberDetail(int memberId)
    {
        // 「戻る」「保存後」「退会後」は一覧へ（一覧は表示時に読み込み直す）
        var vm = new MemberDetailViewModel(_memberService, memberId, onClose: _ => GoToMemberList());
        ShowPage(new AdminMemberDetailView(vm, this), AdminSection.Members, "会員詳細");
    }

    // --- 売上 ---

    public async void GoToSalesList(int? memberId = null, string? memberName = null, int? year = null)
    {
        _salesListView ??= new AdminSalesListView(new SalesListViewModel(_salesService), _salesService, this);

        // 顧客や年が指定された場合（ダッシュボード・顧客別集計から）は、その条件で表示する
        if (memberId.HasValue || year.HasValue)
        {
            _salesListView.ApplyFilter(memberId, memberName, year);
        }

        ShowPage(_salesListView, AdminSection.Sales, "売上一覧");
        await _salesListView.ReloadAsync();
    }

    public async void GoToSalesByMember(int? year = null)
    {
        _salesByMemberView ??= new AdminSalesByMemberView(new SalesByMemberViewModel(_salesService), _salesService, this);

        if (year.HasValue)
        {
            _salesByMemberView.ApplyYear(year.Value);
        }

        ShowPage(_salesByMemberView, AdminSection.SalesByMember, "顧客別集計");
        await _salesByMemberView.ReloadAsync();
    }

    // --- 共通 ---

    public bool Confirm(
        string title,
        string message,
        string confirmText,
        ConfirmDialogWindow.DialogVisualType visualType,
        string? subMessage = null)
    {
        var dialog = new ConfirmDialogWindow(
            title: title,
            message: message,
            confirmText: confirmText,
            cancelText: "キャンセル",
            visualType: visualType,
            subMessage: subMessage)
        {
            Owner = this
        };

        var result = dialog.ShowDialog();
        return result == true && dialog.IsConfirmed;
    }

    private void ShowPage(UserControl page, AdminSection section, string pageTitle)
    {
        var isDashboard = section == AdminSection.Dashboard && page == _dashboardView;

        // ヘッダー: ダッシュボードとそれ以外で表示を変える（Web 版の AdminHeader と同じ）
        PageTitleText.Text = isDashboard
            ? "請求・入金ステータスダッシュボード（管理者）"
            : "請求・入金管理システム（管理者）";
        PageDescriptionText.Text = isDashboard
            ? "売上・未入金・請求書と入金の状況を確認できます。"
            : "請求書・入金・会員・売上を管理できます。";
        YearSwitcher.Visibility = isDashboard ? Visibility.Visible : Visibility.Collapsed;

        Title = $"{pageTitle} - {AppName}";

        SetNavStyle(NavDashboard, section == AdminSection.Dashboard);
        SetNavStyle(NavInvoices, section == AdminSection.Invoices);
        SetNavStyle(NavPayments, section == AdminSection.Payments);
        SetNavStyle(NavMembers, section == AdminSection.Members);
        SetNavStyle(NavSales, section == AdminSection.Sales);
        SetNavStyle(NavSalesByMember, section == AdminSection.SalesByMember);

        PageHost.Content = page;
        MainScrollViewer.ScrollToTop();
    }

    private void SetNavStyle(Button button, bool active)
    {
        button.Style = (Style)FindResource(active ? "AdminNavItemActive" : "AdminNavItem");
    }

    // ===== 年の切り替え =====

    private async void PreviousYearButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_dashboardViewModel.IsLoading) return;

        _dashboardViewModel.MovePreviousYear();
        await _dashboardViewModel.LoadAsync();
    }

    private async void NextYearButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_dashboardViewModel.IsLoading) return;

        _dashboardViewModel.MoveNextYear();
        await _dashboardViewModel.LoadAsync();
    }

    // ===== 左メニュー =====

    private void NavDashboard_OnClick(object sender, RoutedEventArgs e) => GoToDashboard();

    private void NavInvoices_OnClick(object sender, RoutedEventArgs e) => GoToInvoiceList();

    private void NavPayments_OnClick(object sender, RoutedEventArgs e) => GoToPaymentList();

    private void NavMembers_OnClick(object sender, RoutedEventArgs e) => GoToMemberList();

    private void NavSales_OnClick(object sender, RoutedEventArgs e) => GoToSalesList();

    private void NavSalesByMember_OnClick(object sender, RoutedEventArgs e) => GoToSalesByMember();

    // ===== ログアウト =====

    private void LogoutButton_OnClick(object sender, RoutedEventArgs e)
    {
        var confirmed = Confirm(
            title: "ログアウト確認",
            message: "管理者セッションを終了してログアウトしますか？",
            confirmText: "ログアウト",
            visualType: ConfirmDialogWindow.DialogVisualType.DangerConfirm,
            subMessage: "ログアウトすると、再度ログインが必要になります。");

        if (!confirmed)
        {
            return;
        }

        try
        {
            _authService.Logout();
        }
        catch (Exception)
        {
            // ログアウト API の失敗に関わらずログイン画面へ戻す（従来と同じ）
        }

        var loginWindow = new LoginWindow();
        loginWindow.Show();
        Close();
    }
}
