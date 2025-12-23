using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvoiceSystem.Domain.Entities;

public class Payment
{
    public long Id { get; set; }

    public long MemberId { get; set; }
    public Member Member { get; set; } = null!;

    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }

    /// <summary>
    /// 振込名義など
    /// </summary>
    public string? PayerName { get; set; }

    /// <summary>
    /// 入金方法（振込, 現金, クレジットカード など）
    /// </summary>
    public string? Method { get; set; }

    public long? ImportBatchId { get; set; }
    public PaymentImportBatch? ImportBatch { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<PaymentAllocation> PaymentAllocations { get; set; }
    = new List<PaymentAllocation>();
}

