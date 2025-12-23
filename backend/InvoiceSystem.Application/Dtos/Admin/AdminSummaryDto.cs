namespace InvoiceSystem.Application.Dtos.Admin;

public record MonthlySalesDto(
    int Month,          // 1..12
    decimal InvoiceTotal
);

public record UnpaidInvoiceDto(
    long InvoiceId,
    string InvoiceNumber,
    string ClientName,
    DateTime DueDate,
    decimal InvoiceTotal,
    decimal PaidTotal,
    decimal RemainingTotal,
    bool IsOverdue
);

public record AdminSummaryDto(
    int Year,
    decimal InvoiceTotal,     // 売上（請求総額）
    decimal PaidTotal,        // 入金総額（割当合計）
    decimal RemainingTotal,   // 未回収（残額合計）
    decimal RecoveryRate,     // 0..100
    int InvoiceCount,
    int PaymentCount,
    IReadOnlyList<MonthlySalesDto> MonthlySales,  // 12ヶ月
    IReadOnlyList<UnpaidInvoiceDto> UnpaidTop5    // 未回収TOP5（期限超過優先）
);
