namespace InvoiceSystem.Application.Dtos.Collections;

public class DunningLogDto
{
    public long Id { get; set; }
    public DateTime At { get; set; }                 // RemindedAt
    public string Channel { get; set; } = null!;     // EMAIL/PHONE/LETTER
    public string Title { get; set; } = "";
    public string? Memo { get; set; }

    public string? Tone { get; set; }                // SOFT/NORMAL/STRONG
    public DateTime? NextActionDate { get; set; }
}

