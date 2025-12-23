using InvoiceSystem.Application.Commands.Invoices;
using InvoiceSystem.Application.Dtos.Invoices;
using InvoiceSystem.Application.Queries.Invoices;

namespace InvoiceSystem.Application.Services;

public interface IInvoiceService
{
    /// <summary>
    /// 請求書作成
    /// </summary>
    Task<InvoiceDto> CreateAsync(CreateInvoiceCommand command);

    /// <summary>
    /// 請求書詳細取得
    /// </summary>
    Task<InvoiceDto?> GetByIdAsync(long id);

    // ★追加（詳細専用）
    Task<InvoiceDetailDto?> GetDetailByIdAsync(long id);

    Task<bool> DeleteAsync(long id);

    /// <summary>
    /// 請求書一覧検索
    /// </summary>
    Task<IReadOnlyList<InvoiceListItemDto>> SearchAsync(InvoiceSearchQuery query);

    /// <summary>
    /// 請求書ステータス更新（入金状況・督促など）
    /// </summary>
    Task UpdateStatusAsync(UpdateInvoiceStatusCommand command);

    /// <summary>
    /// 請求書PDFを生成（バイト配列で返す）
    /// </summary>
    Task<byte[]> GeneratePdfAsync(long id);
    Task<InvoiceDto> CreateWithLinesAsync(UpdateInvoiceRequestDto req);

    Task UpdateAsync(long invoiceId, UpdateInvoiceRequestDto req);

    // ★追加：会員（自分）の請求書一覧
    Task<MyInvoiceListResultDto> SearchMyInvoicesAsync(MyInvoiceSearchQuery query);
}

