namespace InvoiceSystem.Domain.Entities;

public class ReminderHistory
{
    public long Id { get; set; }

    public long InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public DateTime RemindedAt { get; set; }           // 記録日時
    public string Method { get; set; } = null!;        // "EMAIL" | "PHONE" | "LETTER"
    public string? Tone { get; set; }                  // "SOFT" | "NORMAL" | "STRONG"（任意）
    public string? Title { get; set; }                 // 画面履歴のタイトル（任意）
    public string? Note { get; set; }                  // メモ
    public DateTime? NextActionDate { get; set; }       // 次回アクション（任意）

    // テンプレ文を監査的に残したい場合（任意）
    public string? Subject { get; set; }
    public string? BodyText { get; set; }

    public DateTime CreatedAt { get; set; }
}


