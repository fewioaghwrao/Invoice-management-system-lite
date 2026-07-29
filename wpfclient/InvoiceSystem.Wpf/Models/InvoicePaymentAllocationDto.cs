using System;
using System.Text.Json.Serialization;

namespace InvoiceSystem.Wpf.Models;

public class InvoicePaymentAllocationDto
{
    [JsonPropertyName("paymentId")]
    public long PaymentId { get; set; }

    [JsonPropertyName("paymentDate")]
    public DateTime PaymentDate { get; set; }

    [JsonPropertyName("allocatedAmount")]
    public decimal AllocatedAmount { get; set; }

    [JsonPropertyName("payerName")]
    public string? PayerName { get; set; }

    [JsonPropertyName("method")]
    public string? Method { get; set; }

    [JsonPropertyName("importBatchId")]
    public long? ImportBatchId { get; set; }
}