namespace InvoiceSystem.Domain.Entities;

public class InvoiceLine
{
    public long Id { get; set; }

    public long InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public int LineNo { get; set; }                 // 表示順（1,2,3...）
    public string Name { get; set; } = null!;       // 品目
    public int Qty { get; set; }                    // 数量
    public decimal UnitPrice { get; set; }          // 単価

    // Lite: 税などは後で。まずは Amountを計算で出す
    public decimal Amount => Qty * UnitPrice;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
