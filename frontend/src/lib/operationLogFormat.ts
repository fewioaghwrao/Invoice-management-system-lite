export function formatOperationActionLabel(code: string): string {
  const x = (code ?? "").toUpperCase();

  return x === "PAYMENT_CREATED" ? "入金登録"
    : x === "PAYMENT_ALLOCATION_ADDED" ? "割当追加"
    : x === "PAYMENT_ALLOCATION_DELETED" ? "割当削除"
    : x === "PAYMENT_ALLOCATIONS_REPLACED" ? "割当保存（置換）"
    : x === "PAYMENT_ALLOCATIONS_CLEARED" ? "割当クリア"
    : x === "INVOICE_CREATED" ? "請求書作成"
    : x === "INVOICE_UPDATED" ? "請求書更新"
    : x === "INVOICE_DELETED" ? "請求書削除"
    : x === "INVOICE_STATUS_UPDATED" ? "ステータス更新"
    : x === "DUNNING_LOG_CREATED" ? "催促ログ作成"
    : code;
}

export function formatOperationTarget(
  entity: string,
  entityId?: string | null
): string {
  const e = (entity ?? "").toUpperCase();

  const label = e === "PAYMENT" ? "入金"
    : e === "PAYMENTALLOCATION" ? "入金割当"
    : e === "INVOICE" ? "請求書"
    : e === "MEMBER" ? "会員"
    : e === "REMINDERHISTORY" ? "催促履歴"
    : entity;

  return entityId ? `${label} #${entityId}` : label;
}