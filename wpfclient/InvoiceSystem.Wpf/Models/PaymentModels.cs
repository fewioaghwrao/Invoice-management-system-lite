using System;
using System.Collections.Generic;
using System.Linq;

namespace InvoiceSystem.Wpf.Models;

public class PaymentFilterOption
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;

    public override string ToString() => Label;
}

public class PaymentMethodOption
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;

    public override string ToString() => Label;
}

public class PaymentInvoiceLinkDto
{
    public long Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
}

public class PaymentSummaryDto
{
    public decimal TotalAmount { get; set; }
    public decimal AllocatedTotal { get; set; }
    public decimal UnallocatedTotal { get; set; }
}

public class PaymentListItemDto
{
    public long Id { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? PayerName { get; set; }
    public decimal Amount { get; set; }
    public decimal AllocatedAmount { get; set; }
    public List<PaymentInvoiceLinkDto> Invoices { get; set; } = new();
    public string Status { get; set; } = "UNALLOCATED";

    public string PaymentIdText => $"PAY-{Id:D3}";
    public string PaymentDateText => PaymentDate.ToString("yyyy/MM/dd");
    public decimal UnallocatedAmount => Amount - AllocatedAmount < 0 ? 0 : Amount - AllocatedAmount;

    public string StatusText => Status?.ToUpperInvariant() switch
    {
        "ALLOCATED" => "割当済",
        "PARTIAL" => "一部割当",
        _ => "未割当"
    };

    public string InvoiceNumbersText =>
        Invoices == null || Invoices.Count == 0
            ? "—"
            : string.Join(", ", Invoices
                .Where(x => !string.IsNullOrWhiteSpace(x.InvoiceNumber))
                .Select(x => x.InvoiceNumber));
}

public class PaymentListResultDto
{
    public int Year { get; set; }
    public string Month { get; set; } = "all";
    public string Keyword { get; set; } = string.Empty;
    public string Status { get; set; } = "all";
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public List<PaymentListItemDto> Rows { get; set; } = new();
    public PaymentSummaryDto Summary { get; set; } = new();
}

public class PaymentSearchRequest
{
    public int Year { get; set; }
    public string Month { get; set; } = "all";
    public string Q { get; set; } = string.Empty;
    public string Status { get; set; } = "all";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class PaymentAllocationDto
{
    public long AllocationId { get; set; }
    public long InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class PaymentDetailDto
{
    public long Id { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? PayerName { get; set; }
    public string? Method { get; set; }
    public decimal Amount { get; set; }
    public decimal AllocatedAmount { get; set; }
    public decimal UnallocatedAmount { get; set; }
    public string Status { get; set; } = "UNALLOCATED";
    public List<PaymentAllocationDto> Allocations { get; set; } = new();
}

public class CreatePaymentRequestDto
{
    public long MemberId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string? PayerName { get; set; }
    public string? Method { get; set; }
}

public class CreatePaymentResponseEnvelope
{
    public long Id { get; set; }
}

public class PaymentInvoiceSearchCondition
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
}

public class PaymentAllocationEditRow
{
    public long? InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public string AmountText { get; set; } = string.Empty;
}

public class SavePaymentAllocationsRequestDto
{
    public List<PaymentAllocationLineDto> Lines { get; set; } = new();
}

public class PaymentAllocationLineDto
{
    public long InvoiceId { get; set; }
    public decimal Amount { get; set; }
}