namespace InvoiceSystem.Wpf.Models;

public class SalesByMemberRowDto
{
    public int MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public decimal InvoiceTotal { get; set; }
    public decimal PaidTotal { get; set; }
    public decimal RemainingTotal { get; set; }
    public double RecoveryRate { get; set; }

    public string RecoveryRateText => $"{RecoveryRate:F1}%";
}