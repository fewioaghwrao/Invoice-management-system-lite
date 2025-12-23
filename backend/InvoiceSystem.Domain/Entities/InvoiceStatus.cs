namespace InvoiceSystem.Domain.Entities;

public class InvoiceStatus
{
    public long Id { get; set; }

    /// <summary>
    /// 内部コード（PENDING, PARTIAL, PAID, OVERDUE, DUNNING など）
    /// </summary>
    public string Code { get; set; } = null!;

    /// <summary>
    /// 画面表示用名（未入金, 部分入金, 入金済み, 期限超過, 催促中 など）
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// 期限超過系ステータスかどうか
    /// </summary>
    public bool IsOverdue { get; set; }

    /// <summary>
    /// 完了状態（入金済みなど）か
    /// </summary>
    public bool IsClosed { get; set; }

    public int SortOrder { get; set; }

    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}

