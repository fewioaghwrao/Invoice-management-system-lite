namespace InvoiceSystem.Wpf.Models;

public class DunningLogDto
{
    public long Id { get; set; }
    public DateTime At { get; set; }

    public string Channel { get; set; } = string.Empty;   // EMAIL / PHONE / LETTER
    public string Title { get; set; } = string.Empty;
    public string? Memo { get; set; }

    public string? Tone { get; set; }                     // SOFT / NORMAL / STRONG
    public DateTime? NextActionDate { get; set; }
}