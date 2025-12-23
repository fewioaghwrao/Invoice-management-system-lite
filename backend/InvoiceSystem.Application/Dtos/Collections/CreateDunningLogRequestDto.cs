namespace InvoiceSystem.Application.Dtos.Collections;

public class CreateDunningLogRequestDto
{
    public string Channel { get; set; } = "EMAIL";   // EMAIL/PHONE/LETTER
    public string? Tone { get; set; }                // SOFT/NORMAL/STRONG
    public string Title { get; set; } = "";          // "初回督促（標準）" など
    public string? Memo { get; set; }                // メモ

    public DateTime? NextActionDate { get; set; }

    // 任意：文面保存
    public string? Subject { get; set; }
    public string? BodyText { get; set; }
}

