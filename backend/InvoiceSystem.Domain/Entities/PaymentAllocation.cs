using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvoiceSystem.Domain.Entities;

public class PaymentAllocation
{
    public long Id { get; set; }

    public long PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;

    public long InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public decimal Amount { get; set; }
}

