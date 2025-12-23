// src/app/invoices/[id]/page.tsx
import Link from "next/link";
import { apiGetServer } from "@/lib/api.server";
import DeleteInvoiceButton from "./DeleteInvoiceButton";
import type { InvoiceDetailDto } from "@/types/invoice";
import PdfDownloadButton from "./PdfDownloadButton";

function formatCurrency(value: number): string {
  return value.toLocaleString("ja-JP", {
    style: "currency",
    currency: "JPY",
    maximumFractionDigits: 0,
  });
}

function statusPillClass(statusName: string) {
  // UI側は statusName（未入金/一部入金/入金済み）で十分
  if (statusName === "入金済み") return "bg-emerald-500/10 text-emerald-300 border-emerald-500/30";
  if (statusName === "一部入金") return "bg-amber-500/10 text-amber-200 border-amber-500/30";
  return "bg-red-500/10 text-red-300 border-red-500/30";
}

function methodLabel(m?: string | null) {
  // Payment.Method は自由文字列想定なので、軽く寄せる
  if (!m) return "—";
  const u = m.toUpperCase();
  return u.includes("BANK") || u.includes("振込") ? "銀行振込"
    : u.includes("CASH") || u.includes("現金") ? "現金"
    : u.includes("CARD") || u.includes("クレジット") ? "カード"
    : "その他";
}

function ymd(iso?: string | null) {
  return iso ? iso.slice(0, 10) : "-";
}

// ★API：請求書詳細
async function getInvoiceDetail(id: string): Promise<InvoiceDetailDto> {
  // id は route param なので string -> number に変換してもOKだけど、URLはstringのまま使える
  return apiGetServer<InvoiceDetailDto>(`/api/invoices/${id}`);
}

export default async function InvoiceDetailPage({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ from?: string }>;
}) {
  const { id } = await params;
  const sp = await searchParams;
  const from = sp.from ?? "";
  const backHref = from ? `/invoices?${from}` : "/invoices";

  const inv = await getInvoiceDetail(id);

  // 明細（Lite：DB未実装なので 1行で見せる）
  const items = [
    {
      name: "請求（合計）",
      qty: 1,
      unitPrice: inv.totalAmount,
      amount: inv.totalAmount,
    },
  ];

  return (
    <div className="min-h-screen bg-slate-950 text-slate-50">
      <div className="pointer-events-none fixed inset-0 bg-[radial-gradient(circle_at_top,_#0ea5e9_0,_transparent_55%),radial-gradient(circle_at_bottom,_#6366f1_0,_transparent_55%)] opacity-60" />

      <header className="relative z-10 border-b border-slate-800 bg-slate-950/80 backdrop-blur">
        <div className="mx-auto max-w-6xl px-4 sm:px-6 py-4">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div className="min-w-0">
              <div className="flex items-center gap-2 text-[11px] text-slate-400">
                <Link href="/dashboards/admin" className="hover:text-sky-300">
                  ダッシュボード
                </Link>
                <span>/</span>
                <Link href="/invoices" className="hover:text-sky-300">
                  請求書一覧
                </Link>
                <span>/</span>
                <span className="truncate">{inv.invoiceNumber}</span>
              </div>

              <div className="mt-1 flex flex-wrap items-center gap-2">
                <h1 className="text-lg font-semibold">{inv.invoiceNumber}</h1>
                <span
                  className={`inline-flex items-center rounded-full border px-2 py-0.5 text-[11px] ${statusPillClass(
                    inv.statusName
                  )}`}
                >
                  {inv.statusName}
                </span>
              </div>

              <p className="mt-1 text-xs text-slate-400">
                請求情報・入金割当・督促履歴・残額をまとめて確認できます。
              </p>
            </div>

            <div className="flex flex-wrap items-center gap-2">
              <Link
                href={backHref}
                className="inline-flex items-center justify-center rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-1.5 text-xs text-slate-200 hover:bg-slate-800"
              >
                ← 一覧へ
              </Link>

              {/* 編集/削除はUIのままでOK（API実装時に繋ぐ） */}
              <Link
                href={`/invoices/${inv.id}/edit${from ? `?from=${encodeURIComponent(from)}` : ""}`}
                className="inline-flex items-center justify-center rounded-lg border border-slate-700 bg-slate-900 px-3 py-1.5 text-xs text-slate-200 hover:bg-slate-800"
              >
                編集
              </Link>

              <DeleteInvoiceButton invoiceId={String(inv.id)} />

              <Link
                href={`/collections/${inv.id}`}
                className="inline-flex items-center justify-center rounded-lg border border-amber-500/40 bg-amber-500/10 px-3 py-1.5 text-xs font-medium text-amber-200 hover:bg-amber-500/15"
              >
                督促へ
              </Link>

<Link
  href={`/payments/new?invoiceId=${inv.id}${from ? `&from=${encodeURIComponent(from)}` : ""}`}
  className="rounded-lg border border-slate-700 bg-slate-900 px-3 py-1.5 text-xs text-slate-200 hover:bg-slate-800"
>
  入金登録
</Link>
            </div>
          </div>
        </div>
      </header>

      <main className="relative z-10 mx-auto max-w-6xl px-4 sm:px-6 py-6 sm:py-8 space-y-6">
        {/* KPI */}
        <section className="rounded-2xl border border-slate-800 bg-slate-900/80 p-4 sm:p-5 shadow-md">
          <div className="grid gap-3 md:grid-cols-3">
            <div className="rounded-xl border border-slate-800 bg-slate-950/30 p-4">
              <p className="text-[11px] text-slate-400">請求金額</p>
              <p className="mt-2 text-lg sm:text-xl font-semibold break-words">
                {formatCurrency(inv.totalAmount)}
              </p>
            </div>
            <div className="rounded-xl border border-slate-800 bg-slate-950/30 p-4">
              <p className="text-[11px] text-slate-400">入金済み</p>
              <p className="mt-2 text-lg sm:text-xl font-semibold text-emerald-300 break-words">
                {formatCurrency(inv.paidAmount)}
              </p>
            </div>
            <div className="rounded-xl border border-slate-800 bg-slate-950/30 p-4">
              <p className="text-[11px] text-slate-400">残額</p>
              <p className="mt-2 text-lg sm:text-xl font-semibold text-amber-200 break-words">
                {formatCurrency(inv.remainingAmount)}
              </p>
              <p className="mt-1 text-[11px] text-slate-400">
                支払期限：{ymd(inv.dueDate)}
              </p>
            </div>
          </div>
        </section>

        <section className="grid gap-6 lg:grid-cols-3">
          {/* 左 */}
          <div className="lg:col-span-1 space-y-6">
            <div className="rounded-2xl border border-slate-800 bg-slate-900/80 p-4 sm:p-5">
              <h2 className="text-sm font-semibold">基本情報</h2>
              <dl className="mt-3 space-y-3 text-xs">
                <div>
                  <dt className="text-[11px] text-slate-400">請求日</dt>
                  <dd className="mt-1 text-slate-200">{ymd(inv.invoiceDate)}</dd>
                </div>
                <div>
                  <dt className="text-[11px] text-slate-400">支払期限</dt>
                  <dd className="mt-1 text-slate-200">{ymd(inv.dueDate)}</dd>
                </div>
                <div>
                  <dt className="text-[11px] text-slate-400">会員</dt>
                  <dd className="mt-1 text-slate-200">{inv.memberName}</dd>
                </div>
                <div>
                  <dt className="text-[11px] text-slate-400">備考</dt>
                  <dd className="mt-1 text-slate-300 whitespace-pre-wrap">
                    {inv.remarks ?? "—"}
                  </dd>
                </div>
              </dl>
            </div>

            {/* 督促履歴（ReminderHistories） */}
            <div className="rounded-2xl border border-slate-800 bg-slate-900/80 p-4 sm:p-5">
              <h2 className="text-sm font-semibold">督促履歴</h2>

              {inv.reminders.length === 0 ? (
                <div className="mt-4 rounded-xl border border-slate-800 bg-slate-950/40 px-4 py-6 text-center text-xs text-slate-400">
                  督促履歴がありません。
                </div>
              ) : (
                <div className="mt-4 space-y-3">
                  {inv.reminders.map((r) => (
                    <div key={r.id} className="rounded-xl border border-slate-800 bg-slate-950/40 p-4">
                      <div className="flex items-start justify-between gap-3">
                        <div className="min-w-0">
                          <p className="text-xs text-slate-200">
                            {ymd(r.remindedAt)} / {r.method}
                          </p>
                          {r.note && (
                            <p className="mt-1 text-[11px] text-slate-400 whitespace-pre-wrap">
                              {r.note}
                            </p>
                          )}
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>

          {/* 右 */}
          <div className="lg:col-span-2 space-y-6">
            {/* 明細（Lite） */}
            <div className="rounded-2xl border border-slate-800 bg-slate-900/80 p-4 sm:p-5">
<div className="flex items-center justify-between">
  <h2 className="text-sm font-semibold">請求明細（Lite）</h2>
  <PdfDownloadButton invoiceId={Number(inv.id)} />
</div>
              <div className="mt-4 overflow-hidden rounded-xl border border-slate-800 bg-slate-950/40">
                <table className="min-w-full text-xs">
                  <thead className="bg-slate-900/90">
                    <tr className="text-left text-[11px] text-slate-400">
                      <th className="px-4 py-2 font-medium">項目</th>
                      <th className="px-4 py-2 font-medium text-right">数量</th>
                      <th className="px-4 py-2 font-medium text-right">単価</th>
                      <th className="px-4 py-2 font-medium text-right">金額</th>
                    </tr>
                  </thead>
                  <tbody>
                    {items.map((it, idx) => (
                      <tr key={idx} className="border-t border-slate-800/80">
                        <td className="px-4 py-2 text-slate-200">{it.name}</td>
                        <td className="px-4 py-2 text-right text-slate-200 tabular-nums">{it.qty}</td>
                        <td className="px-4 py-2 text-right text-slate-200 tabular-nums">
                          {formatCurrency(it.unitPrice)}
                        </td>
                        <td className="px-4 py-2 text-right text-slate-50 tabular-nums">
                          {formatCurrency(it.amount)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>

            {/* 入金履歴（Allocations） */}
            <div className="rounded-2xl border border-slate-800 bg-slate-900/80 p-4 sm:p-5">
              <div className="flex items-center justify-between">
                <h2 className="text-sm font-semibold">入金履歴（割当）</h2>
<Link
  href={`/payments/new?invoiceId=${inv.id}${from ? `&from=${encodeURIComponent(from)}` : ""}`}
  className="rounded-lg border border-slate-700 bg-slate-900 px-3 py-1.5 text-xs text-slate-200 hover:bg-slate-800"
>
  入金登録
</Link>
              </div>

              {inv.allocations.length === 0 ? (
                <div className="mt-4 rounded-xl border border-slate-800 bg-slate-950/40 px-4 py-8 text-center text-xs text-slate-400">
                  入金履歴がありません。
                </div>
              ) : (
                <div className="mt-4 space-y-3">
                  {inv.allocations.map((a) => (
                    <div
                      key={`${a.paymentId}-${a.paymentDate}-${a.allocatedAmount}`}
                      className="rounded-xl border border-slate-800 bg-slate-950/40 p-4"
                    >
                      <div className="flex items-start justify-between gap-3">
                        <div className="min-w-0">
                          <p className="text-xs text-slate-200">
                            {ymd(a.paymentDate)} / {methodLabel(a.method)}
                          </p>
                          <p className="mt-1 text-[11px] text-slate-400">
                            {a.payerName ? `名義：${a.payerName}` : "名義：—"}
                            {a.importBatchId ? ` / 取込：#${a.importBatchId}` : ""}
                          </p>
                        </div>
                        <p className="text-sm font-semibold text-emerald-200 tabular-nums">
                          {formatCurrency(a.allocatedAmount)}
                        </p>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>

          </div>
        </section>
      </main>
    </div>
  );
}
