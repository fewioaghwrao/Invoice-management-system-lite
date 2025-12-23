export type InvoiceLineDto = {
  id: number;
  lineNo: number;
  name: string;
  qty: number;
  unitPrice: number;
};

export type InvoicePaymentAllocationDto = {
  paymentId: number;
  paymentDate: string;
  allocatedAmount: number;
  payerName?: string | null;
  method?: string | null;
  importBatchId?: number | null;
};

export type InvoiceReminderHistoryDto = {
  id: number;
  remindedAt: string;
  method: string;
  note?: string | null;
};

export type InvoiceDetailDto = {
  id: number;
  memberId: number;
  memberName: string;

  invoiceNumber: string;
  invoiceDate: string;
  dueDate: string;

  totalAmount: number;
  paidAmount: number;
  remainingAmount: number;

  statusId: number;
  statusName: string;

  pdfPath?: string | null;
  remarks?: string | null;

  createdAt: string;

  allocations: InvoicePaymentAllocationDto[];
  reminders: InvoiceReminderHistoryDto[];
  lines: InvoiceLineDto[];
};
