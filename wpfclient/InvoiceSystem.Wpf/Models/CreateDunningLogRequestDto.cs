namespace InvoiceSystem.Wpf.Models;

public class CreateDunningLogRequestDto
{
    public string Channel { get; set; } = "EMAIL";
    public string Tone { get; set; } = "NORMAL";
    public string Title { get; set; } = string.Empty;
    public string? Memo { get; set; }
    public DateTime? NextActionDate { get; set; }

    public string Subject { get; set; } = string.Empty;
    public string BodyText { get; set; } = string.Empty;
}