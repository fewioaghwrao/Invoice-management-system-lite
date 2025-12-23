"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { apiDeleteClient } from "@/lib/api.client"; // 無ければ fetch でOK

export default function DeleteInvoiceButton({ invoiceId }: { invoiceId: string }) {
  const router = useRouter();
  const sp = useSearchParams();
  const from = sp.get("from") ?? "";
  const backHref = from ? `/invoices?${from}` : "/invoices";

  const onDelete = async () => {
    if (!confirm("この請求書を削除しますか？")) return;

    try {
      await apiDeleteClient(`/api/invoices/${invoiceId}`); // 204想定
      router.push(backHref);
      router.refresh();
    } catch (e: any) {
      // 409(入金割当あり) を想定
      alert(e?.message ?? "削除できませんでした。入金割当がある可能性があります。");
    }
  };

  return (
    <button
      onClick={onDelete}
      className="rounded-lg border border-rose-500/40 bg-rose-500/10 px-3 py-1.5 text-xs font-medium text-rose-200 hover:bg-rose-500/15"
    >
      削除
    </button>
  );
}
