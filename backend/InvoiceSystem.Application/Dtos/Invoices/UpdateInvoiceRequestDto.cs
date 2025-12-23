using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvoiceSystem.Application.Dtos.Invoices
{
    public class UpdateInvoiceRequestDto
    {
        public string InvoiceNumber { get; set; } = null!;
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }

        public long MemberId { get; set; }   // ★追加
        public long StatusId { get; set; }
        public string? Remarks { get; set; }

        public List<InvoiceLineDto> Lines { get; set; } = new();
    }
}
