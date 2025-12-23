using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvoiceSystem.Application.Dtos.Payments
{
    public class CreatePaymentAllocationRequestDto
    {
        public long InvoiceId { get; set; }
        public decimal Amount { get; set; }
    }

}
