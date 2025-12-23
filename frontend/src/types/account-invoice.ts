// src/types/account-invoice.ts
export type AccountInvoiceListItemDto = {
  id: string; // or number/long - APIに合わせて調整
  invoiceNumber: string;
  issuedAt: string; // ISO "2025-12-01"
  dueAt: string;    // ISO
  totalAmount: number;
  statusName: string; // "未入金" / "一部入金" / "入金済み" など表示用
  isOverdue: boolean; // 期限超過ラベル用
};

export type AccountInvoiceListDto = {
  year: number;
  availableYears: number[];
  month: string; // "all" or "1".."12"
  status: string; // "all" | "unpaid" | "partial" | "paid" など（UIは自由）
  q: string;
  page: number;
  pageSize: number;
  totalCount: number;
  items: AccountInvoiceListItemDto[];
};
