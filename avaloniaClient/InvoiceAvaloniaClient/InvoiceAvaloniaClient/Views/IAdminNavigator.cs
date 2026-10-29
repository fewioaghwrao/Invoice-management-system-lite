namespace InvoiceAvaloniaClient.Views;

/// <summary>
/// 管理者用シェル（AdminShellWindow）内での画面移動。各画面はこれを通して移動を依頼する。
/// </summary>
public interface IAdminNavigator
{
    void GoToDashboard();

    void GoToInvoiceList();

    void GoToInvoiceDetail(long invoiceId);

    void GoToCollection(long invoiceId);

    /// <summary>請求書の新規作成（invoiceId = null）／編集画面へ。</summary>
    void GoToInvoiceEditor(long? invoiceId);

    void GoToPaymentList();

    void GoToPaymentDetail(long paymentId);

    void GoToPaymentCreate();

    void GoToMemberList();

    void GoToMemberDetail(int memberId);

    /// <summary>売上一覧へ。顧客・年を指定した場合はその条件で表示する。</summary>
    void GoToSalesList(int? memberId = null, string? memberName = null, int? year = null);

    /// <summary>顧客別集計へ。年を指定した場合はその年で表示する。</summary>
    void GoToSalesByMember(int? year = null);

    /// <summary>確認ダイアログ（Avalonia のダイアログは非同期なので await する）。</summary>
    Task<bool> ConfirmAsync(string title, string message, string confirmText, ConfirmDialogWindow.DialogVisualType visualType, string? subMessage = null);
}
