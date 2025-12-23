import Link from "next/link";
import { apiGetServer } from "@/lib/api.server";
import type { InvoiceDetailDto } from "@/types/invoice-detail";

function formatYmd(iso: string) {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleDateString("ja-JP", { year: "numeric", month: "2-digit", day: "2-digit" });
}

function formatJPY(value: number) {
  return value.toLocaleString("ja-JP", { style: "currency", currency: "JPY", maximumFractionDigits: 0 });
}

export default async function AccountInvoiceDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;          
  const invoiceId = Number(id);          
  let data: InvoiceDetailDto | null = null;
  let error: string | null = null;

  try {
    data = await apiGetServer<InvoiceDetailDto>(`/api/members/me/invoices/${invoiceId}`);
  } catch (e) {
    error = e instanceof Error ? e.message : "請求書詳細の取得に失敗しました。";
  }

  if (!data) {
    return (
      <div className="space-y-4">
        <Link href="/account/invoices" className="text-xs text-slate-300 hover:text-white">
          ← 請求書一覧へ戻る
        </Link>

        <div className="rounded-2xl border border-slate-800 bg-slate-900/70 p-6 text-slate-100">
          <div className="text-base font-semibold">請求書詳細</div>
          <p className="mt-2 text-sm text-rose-300">{error ?? "見つかりませんでした。"}</p>
        </div>
      </div>
    );
  }

  const isOverdue =
    new Date(data.dueDate).getTime() < new Date().setHours(0, 0, 0, 0) &&
    data.remainingAmount > 0;

  return (
    <div className="space-y-6">
      {/* 上部導線（右側コンテンツ内） */}
      <div className="flex items-center justify-between">
        <Link href="/account/invoices" className="text-xs text-slate-300 hover:text-white">
          ← 請求書一覧へ戻る
        </Link>

        <a
          href={`/account/invoices/${data.id}/pdf`}
          target="_blank"
          rel="noreferrer"
          className="text-xs font-semibold text-slate-200 hover:text-white"
        >
          📄 PDF表示
        </a>
      </div>

      {/* 以降は “黒基調” に合わせたカード */}
      <section className="rounded-2xl border border-slate-800 bg-slate-900/70 p-6 shadow-sm">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <div className="text-[11px] text-slate-400">請求書番号</div>
            <div className="mt-1 text-lg font-semibold text-slate-100">{data.invoiceNumber}</div>

            <div className="mt-3 flex flex-wrap items-center gap-2">
              <span className="inline-flex items-center rounded-full bg-slate-950/40 px-2 py-1 text-[11px] font-semibold text-slate-200 border border-slate-700">
                {data.statusName}
              </span>

              {isOverdue && (
                <span className="inline-flex items-center rounded-full bg-amber-500/10 px-2 py-1 text-[11px] font-semibold text-amber-300 border border-amber-500/30">
                  ⚠ 期限超過
                </span>
              )}
            </div>
          </div>

          <div className="grid gap-1 text-xs text-slate-200 sm:text-right">
            <div>
              <span className="text-slate-400">請求日：</span>
              {formatYmd(data.invoiceDate)}
            </div>
            <div>
              <span className="text-slate-400">支払期限：</span>
              {formatYmd(data.dueDate)}
            </div>
          </div>
        </div>

        <div className="mt-5 grid gap-3 sm:grid-cols-3">
          <div className="rounded-xl border border-slate-800 bg-slate-950/30 p-4">
            <div className="text-[11px] text-slate-400">請求金額</div>
            <div className="mt-1 text-base font-semibold text-slate-100">{formatJPY(data.totalAmount)}</div>
          </div>
          <div className="rounded-xl border border-slate-800 bg-slate-950/30 p-4">
            <div className="text-[11px] text-slate-400">入金済み</div>
            <div className="mt-1 text-base font-semibold text-slate-100">{formatJPY(data.paidAmount)}</div>
          </div>
          <div className="rounded-xl border border-slate-800 bg-slate-950/30 p-4">
            <div className="text-[11px] text-slate-400">残額</div>
            <div className="mt-1 text-base font-semibold text-slate-100">{formatJPY(data.remainingAmount)}</div>
          </div>
        </div>

        {data.remarks && (
          <div className="mt-5">
            <div className="text-[11px] font-semibold text-slate-200">備考</div>
            <div className="mt-2 rounded-xl border border-slate-800 bg-slate-950/30 p-4 text-sm text-slate-200">
              {data.remarks}
            </div>
          </div>
        )}
      </section>
    </div>
  );
}
