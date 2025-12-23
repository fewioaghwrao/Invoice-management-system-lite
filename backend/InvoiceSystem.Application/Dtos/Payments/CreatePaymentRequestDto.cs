using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvoiceSystem.Application.Dtos.Payments
{
    public class CreatePaymentRequestDto
    {
        public long MemberId { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string? PayerName { get; set; }
        public string? Method { get; set; }
    }
}
