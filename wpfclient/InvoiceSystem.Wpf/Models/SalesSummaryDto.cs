namespace InvoiceSystem.Wpf.Models;

public class SalesSummaryDto
{
    public decimal InvoiceTotal { get; set; }
    public decimal PaidTotal { get; set; }
    public decimal RemainingTotal { get; set; }
    public double RecoveryRate { get; set; }
}