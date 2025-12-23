// src/types/invoice.ts

// 既存：古い/簡易モデル（使ってるなら残す）
export interface Invoice {
  id: number;
  memberId: number;
  amount: number;
  status: string;
  invoiceDate: string; // ISO文字列として返るため
  dueDate: string;
}

// 既存：一覧用DTO（現状のまま）
export type InvoiceDto = {
  id: number;
  invoiceNumber: string;
  invoiceDate: string; // ISO 文字列
  dueDate: string;     // ISO 文字列
  totalAmount: number;
  memberName: string;
  statusName: string;
};

// ==============================
// ★追加：詳細画面用DTO（バックエンドの InvoiceDetailDto に合わせる）
// ==============================

export type InvoiceAllocationDto = {
  paymentId: number;
  paymentDate: string;        // ISO
  allocatedAmount: number;

  payerName?: string | null;
  method?: string | null;
  importBatchId?: number | null;
};

export type InvoiceReminderDto = {
  id: number;
  remindedAt: string;         // ISO
  method: string;
  note?: string | null;
};

export type InvoiceDetailDto = {
  id: number;

  memberId: number;
  memberName: string;

  invoiceNumber: string;
  invoiceDate: string;        // ISO
  dueDate: string;            // ISO

  totalAmount: number;
  paidAmount: number;
  remainingAmount: number;

  statusId: number;
  statusName: string;

  pdfPath?: string | null;
  remarks?: string | null;

  createdAt: string;          // ISO

  allocations: InvoiceAllocationDto[];
  reminders: InvoiceReminderDto[];
};
