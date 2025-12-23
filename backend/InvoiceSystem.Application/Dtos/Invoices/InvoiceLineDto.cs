using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvoiceSystem.Application.Dtos.Invoices
{
    public class InvoiceLineDto
    {
        public long? Id { get; set; }          // 既存行ならIdあり、新規ならnull
        public int LineNo { get; set; }
        public string Name { get; set; } = null!;
        public int Qty { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
