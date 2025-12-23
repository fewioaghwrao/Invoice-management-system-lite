namespace InvoiceSystem.Application.Commands.Invoices;

public class UpdateInvoiceStatusCommand
{
    public long InvoiceId { get; set; }
    public long StatusId { get; set; }
}
