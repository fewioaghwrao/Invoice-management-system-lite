namespace InvoiceSystem.Api.Endpoints.Sales
{
    // InvoiceSystem.Api/Endpoints/Sales/SalesExportRequest.cs など
    public sealed class SalesExportRequest
    {
        public int? Year { get; init; }
        public int? Month { get; init; }         // null = all
        public string? Status { get; init; }     // "unpaid" / "partial" / "paid" / null
        public string? Q { get; init; }
        public long? MemberId { get; init; }
    }

}
