namespace InvoiceSystem.Application.Dtos.Invoices;

public record MyInvoiceListItemDto(
    long Id,
    string InvoiceNumber,
    DateTime InvoiceDate,
    DateTime DueDate,
    decimal TotalAmount,
    decimal PaidAmount,        
    decimal RemainingAmount,   
    string StatusCode,
    string StatusName,
    bool IsOverdue
);
