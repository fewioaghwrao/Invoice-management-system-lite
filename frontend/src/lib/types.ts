// src/lib/types.ts
export type InvoiceStatus = 'DRAFT' | 'ISSUED' | 'PARTIAL' | 'PAID' | 'OVERDUE' | 'CANCELED';

export interface Invoice {
  id: number;
  invoiceNumber: string;
  customerName: string;
  invoiceDate: string; // ISO文字列
  dueDate: string;
  totalAmount: number;
  paidAmount: number;
  status: InvoiceStatus;
}

export interface DashboardSummary {
  totalInvoicedThisMonth: number;
  totalPaidThisMonth: number;
  totalUnpaid: number;
  totalOverdue: number;
}
