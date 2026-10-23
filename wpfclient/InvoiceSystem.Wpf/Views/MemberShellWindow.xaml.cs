using InvoiceSystem.Wpf.Services;
using InvoiceSystem.Wpf.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace InvoiceSystem.Wpf.Views;

/// <summary>
/// 会員用のシェルウィンドウ。
/// マイページ / 請求書一覧 / 未入金の確認 / 登録情報 / 請求書詳細 を別ウィンドウにせず、
/// 中央の PageHost の中身を差し替えて表示する。
/// 各画面の ViewModel は従来どおりで、画面遷移のコールバックだけをここで渡している。
/// </summary>
public partial class MemberShellWindow : Window
{
    private const string AppName = "Invoice Management System";

    /// <summary>左メニューありの画面の最大幅（max-w-[1440px]）。</summary>
    private const double WideMaxWidth = 1440;

    /// <summary>マイページの最大幅（max-w-5xl）。</summary>
    private const double NarrowMaxWidth = 1024;

    private enum MemberPage
    {
        Dashboard,
        Invoices,
        Unpaid,
        Profile,
        InvoiceDetail
    }

    private readonly object? _currentUser;
    private readonly AuthService _authService;
    private readonly InvoiceService _invoiceService;
    private readonly AccountService _accountService;
    private readonly MemberDashboardViewModel _dashboardViewModel;

    public MemberShellWindow(
        object? currentUser,
        AuthService authService,
        InvoiceService invoiceService,
        AccountService accountService)
    {
        InitializeComponent();

        _currentUser = currentUser;
        _authService = authService;
        _invoiceService = invoiceService;
        _accountService = accountService;

        // ヘッダー（ロール・氏名・ログアウト）とマイページで共用する
        _dashboardViewModel = new MemberDashboardViewModel(
            currentUser: _currentUser,
            authService: _authService,
            invoiceService: _invoiceService,
            accountService: _accountService,
            openInvoiceList: () => NavigateTo(MemberPage.Invoices),
            openPaymentStatus: () => NavigateTo(MemberPage.Unpaid),
            openProfile: () => NavigateTo(MemberPage.Profile),
            openLogin: OpenLoginWindow,
            showConfirmDialog: ShowConfirmDialog);

        DataContext = _dashboardViewModel;

        NavigateTo(MemberPage.Dashboard);
    }

    // ===== 画面切り替え =====

    private void NavigateTo(MemberPage page, long invoiceId = 0, MemberPage detailOrigin = MemberPage.Invoices)
    {
        UserControl view;
        string title;
        string description;
        MemberPage activeMenu = page;

        switch (page)
        {
            case MemberPage.Invoices:
            {
                var vm = new MemberInvoiceListViewModel(
                    currentUser: _currentUser,
                    invoiceService: _invoiceService,
                    openDetail: id => NavigateTo(MemberPage.InvoiceDetail, id, MemberPage.Invoices),
                    openDashboard: () => NavigateTo(MemberPage.Dashboard));

                view = new MemberInvoiceListView(vm);
                title = "請求書一覧";
                description = "請求書の金額・支払期限・入金状況を確認できます。";
                break;
            }

            case MemberPage.Unpaid:
            {
                var vm = new MemberPaymentStatusViewModel(
                    currentUser: _currentUser,
                    invoiceService: _invoiceService,
                    openDetail: id => NavigateTo(MemberPage.InvoiceDetail, id, MemberPage.Unpaid),
                    openDashboard: () => NavigateTo(MemberPage.Dashboard));

                view = new MemberPaymentStatusView(vm, openInvoiceList: () => NavigateTo(MemberPage.Invoices));
                title = "未入金の確認";
                description = "未入金の請求書と支払期限を確認できます。";
                break;
            }

            case MemberPage.Profile:
            {
                var vm = new MemberProfileViewModel(
                    accountService: _accountService,
                    openDashboard: () => NavigateTo(MemberPage.Dashboard),
                    openLogin: OpenLoginWindow,
                    showConfirmDialog: ShowConfirmDialog);

                view = new MemberProfileView(vm);
                title = "登録情報";
                description = "氏名・メールアドレス・住所などを確認・変更できます。";
                break;
            }

            case MemberPage.InvoiceDetail:
            {
                // 「戻る」は開いた元の画面（請求書一覧 or 未入金の確認）へ
                var vm = new MemberInvoiceDetailViewModel(
                    invoiceId: invoiceId,
                    invoiceService: _invoiceService,
                    openInvoiceList: () => NavigateTo(detailOrigin));

                var backText = detailOrigin == MemberPage.Unpaid
                    ? "← 未入金の確認へ戻る"
                    : "← 請求書一覧へ戻る";

                view = new MemberInvoiceDetailView(vm, backText);
                title = "請求書詳細";
                description = "請求内容やお支払い状況を確認できます。";
                activeMenu = detailOrigin;
                break;
            }

            default:
            {
                view = new MemberDashboardView { DataContext = _dashboardViewModel };
                title = "マイページ";
                description = "請求書やお支払い状況を確認できます。";
                activeMenu = MemberPage.Dashboard;
                break;
            }
        }

        PageTitleText.Text = title;
        PageDescriptionText.Text = description;
        Title = $"{title} - {AppName}";

        // マイページだけは左メニューなし・幅狭め（Web 版と同じ）
        ApplyLayout(showSidebar: page != MemberPage.Dashboard);
        UpdateMenuSelection(activeMenu);

        PageHost.Content = view;
        MainScrollViewer.ScrollToTop();
    }

    private void ApplyLayout(bool showSidebar)
    {
        var maxWidth = showSidebar ? WideMaxWidth : NarrowMaxWidth;
        HeaderInner.MaxWidth = maxWidth;
        MainInner.MaxWidth = maxWidth;
        FooterInner.MaxWidth = maxWidth;

        Sidebar.Visibility = showSidebar ? Visibility.Visible : Visibility.Collapsed;
        SidebarColumn.Width = showSidebar ? new GridLength(216) : new GridLength(0);
        SidebarGapColumn.Width = showSidebar ? new GridLength(24) : new GridLength(0);
    }

    private void UpdateMenuSelection(MemberPage activeMenu)
    {
        NavDashboard.IsChecked = activeMenu == MemberPage.Dashboard;
        NavInvoices.IsChecked = activeMenu == MemberPage.Invoices;
        NavUnpaid.IsChecked = activeMenu == MemberPage.Unpaid;
        NavProfile.IsChecked = activeMenu == MemberPage.Profile;
    }

    private void NavDashboard_OnClick(object sender, RoutedEventArgs e) => NavigateTo(MemberPage.Dashboard);

    private void NavInvoices_OnClick(object sender, RoutedEventArgs e) => NavigateTo(MemberPage.Invoices);

    private void NavUnpaid_OnClick(object sender, RoutedEventArgs e) => NavigateTo(MemberPage.Unpaid);

    private void NavProfile_OnClick(object sender, RoutedEventArgs e) => NavigateTo(MemberPage.Profile);

    // ===== ログイン画面へ / 確認ダイアログ =====

    private void OpenLoginWindow()
    {
        var loginWindow = new LoginWindow();
        loginWindow.Show();
        Close();
    }

    private bool ShowConfirmDialog(ConfirmDialogRequest request)
    {
        var dialog = new ConfirmDialogWindow(
            title: request.Title,
            message: request.Message,
            confirmText: request.ConfirmText,
            cancelText: request.CancelText,
            visualType: request.VisualType,
            subMessage: request.SubMessage)
        {
            Owner = this
        };

        var result = dialog.ShowDialog();
        return result == true && dialog.IsConfirmed;
    }
}
