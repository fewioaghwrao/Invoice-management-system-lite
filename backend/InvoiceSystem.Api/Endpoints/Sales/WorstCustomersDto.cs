namespace InvoiceSystem.Api.Endpoints.Sales;

public sealed record WorstCustomerDto(
    long MemberId,
    string MemberName,
    decimal InvoiceTotal,
    decimal PaidTotal,
    decimal RemainingTotal,
    decimal RecoveryRate
);

public sealed record WorstCustomersResultDto(
    int Year,
    string Month,     // "all" or "1".."12"
    string Keyword,
    int Count,
    IReadOnlyList<WorstCustomerDto> Rows
);

