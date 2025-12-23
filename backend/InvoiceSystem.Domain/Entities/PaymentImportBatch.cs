using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvoiceSystem.Domain.Entities;

public class PaymentImportBatch
{
    public long Id { get; set; }

    /// <summary>
    /// CSV, MANUAL など
    /// </summary>
    public string Source { get; set; } = "CSV";

    public string? FileName { get; set; }
    public DateTime ImportedAt { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

