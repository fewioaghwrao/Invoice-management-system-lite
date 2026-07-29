using System;
using System.Collections.Generic;

namespace InvoiceSystem.Wpf.Models;

public class AdminSummaryDto
{
    public int Year { get; set; }
    public decimal InvoiceTotal { get; set; }
    public decimal PaidTotal { get; set; }
    public decimal RemainingTotal { get; set; }
    public double RecoveryRate { get; set; }
    public int InvoiceCount { get; set; }
    public int PaymentCount { get; set; }
    public List<AdminMonthlySalesDto> MonthlySales { get; set; } = new();
    public List<AdminUnpaidInvoiceDto> UnpaidTop5 { get; set; } = new();
}

public class AdminMonthlySalesDto
{
    public int Month { get; set; }
    public decimal InvoiceTotal { get; set; }
}

public class AdminUnpaidInvoiceDto
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public decimal InvoiceTotal { get; set; }
    public decimal PaidTotal { get; set; }
    public decimal RemainingTotal { get; set; }
    public bool IsOverdue { get; set; }

    public string OverdueStatusText => IsOverdue ? "有" : "無";
}

public class AdminOperationLogDto
{
    public int Id { get; set; }
    public DateTime At { get; set; }
    public int ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string Summary { get; set; } = string.Empty;
}

public class WorstCustomerDto
{
    public int MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public decimal InvoiceTotal { get; set; }
    public decimal PaidTotal { get; set; }
    public decimal RemainingTotal { get; set; }
    public double RecoveryRate { get; set; }
}

public class WorstTop5ResultDto
{
    public int Year { get; set; }
    public string Month { get; set; } = "all";
    public string Keyword { get; set; } = string.Empty;
    public int Count { get; set; }
    public List<WorstCustomerDto> Rows { get; set; } = new();
}