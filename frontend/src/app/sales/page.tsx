// src/app/sales/page.tsx
import Link from "next/link";
import CurrentUserBadge from "@/components/CurrentUserBadge";
import LogoutButton from "@/components/LogoutButton";
import SalesFiltersClient from "@/components/SalesFiltersClient";
import { apiGetServer } from "@/lib/api.server";
import CsvExportButton from "@/components/CsvExportButton";

type InvoiceStatus = "UNPAID" | "PARTIAL" | "PAID";

type SalesRow = {
  invoiceId: number; // ★ 文字列IDではなくDBのID
  invoiceNumber: string;
  clientName: string;
  issuedAt: string;
  dueAt: string;
  status: InvoiceStatus;
  invoiceAmount: number;
  paidAmount: number;
  remainingAmount: number;
  lastPaidAt?: string | null;
};

type SalesListResult = {
  year: number;
  month: number | "all";
  keyword: string;
  status: InvoiceStatus | "all";
  page: number;
  pageSize: number;
  totalCount: number;
  memberName?: string; // ★追加
  rows: SalesRow[];
  summary: {
    invoiceTotal: number;
    paidTotal: number;
    remainingTotal: number;
    recoveryRate: number; // 12 みたいに来るので number
  };
};

function formatCurrency(value: number): string {
  return value.toLocaleString("ja-JP", {
    style: "currency",
    currency: "JPY",
    maximumFractionDigits: 0,
  });
}

function formatDate(iso: string): string {
  const d = new Date(iso);
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  return `${y}/${m}/${day}`;
}

function statusLabel(s: InvoiceStatus) {
  return s === "PAID" ? "入金済" : s === "PARTIAL" ? "一部入金" : "未入金";
}
function statusPillClass(s: InvoiceStatus) {
  switch (s) {
    case "PAID":
      return "bg-emerald-500/10 text-emerald-300 border-emerald-500/30";
    case "PARTIAL":
      return "bg-sky-500/10 text-sky-300 border-sky-500/30";
    case "UNPAID":
      return "bg-amber-500/10 text-amber-300 border-amber-500/30";
  }
}

function buildExportQuery(params: {
  year: number;
  month: number | "all";
  status: InvoiceStatus | "all";
  keyword: string;
  memberId?: number;
}) {
  const usp = new URLSearchParams();
  usp.set("year", String(params.year));
  if (params.month !== "all") usp.set("month", String(params.month));
  if (params.status !== "all") usp.set("status", String(params.status).toLowerCase());
  if (params.keyword) usp.set("q", params.keyword);
  if (params.memberId) usp.set("memberId", String(params.memberId));
  return usp.toString();
}

function buildQuery(params: {
  year: number;
  month: number | "all";
  status: InvoiceStatus | "all";
  keyword: string;
  page: number;
  pageSize: number;
  memberId?: number;
}) {
  const usp = new URLSearchParams();
  usp.set("year", String(params.year));
  if (params.month !== "all") usp.set("month", String(params.month));
  if (params.status !== "all") usp.set("status", String(params.status).toLowerCase());
  if (params.keyword) usp.set("q", params.keyword);
  if (params.memberId) usp.set("memberId", String(params.memberId)); // ★追加
  usp.set("page", String(params.page));
  usp.set("pageSize", String(params.pageSize));
  return usp.toString();
}

// ★ API fetch（ここだけ）
async function getSalesList(params: {
  year: number;
  month: number | "all";
  keyword: string;
  status: InvoiceStatus | "all";
  page: number;
  pageSize: number;
  memberId?: number;
}): Promise<SalesListResult>{
  const qs = buildQuery(params);
  // apiGet が /api/* に向くならこのままでOK
  // もしバックエンド別ホストなら apiGet の baseUrl 設定に従ってください
  return apiGetServer(`/api/sales?${qs}`);
}

function buildByMemberQuery(params: {
  year: number;
  month: number | "all";
  keyword: string;
  page: number;
  pageSize: number;
}) {
  const usp = new URLSearchParams();
  usp.set("year", String(params.year));
  if (params.month !== "all") usp.set("month", String(params.month));
  if (params.keyword) usp.set("q", params.keyword);
  usp.set("page", String(params.page));
  usp.set("pageSize", String(params.pageSize));
  return usp.toString();
}

export default async function SalesPage({
  searchParams,
}: {
  searchParams: Promise<{ [key: string]: string | string[] | undefined }>;
}) {
  const params = await searchParams;

  const pick = (v: string | string[] | undefined) => (Array.isArray(v) ? v[0] : v);

  const now = new Date();
  const currentYear = now.getFullYear();

  const year = Number(pick(params.year) ?? currentYear) || currentYear;

  const monthParam = pick(params.month) ?? "all";
  const month = monthParam === "all" ? "all" : (Number(monthParam) || "all");

  const keyword = String(pick(params.q) ?? "");
  const statusParam = String(pick(params.status) ?? "all").toUpperCase();
  const status =
    statusParam === "UNPAID" || statusParam === "PARTIAL" || statusParam === "PAID"
      ? (statusParam as InvoiceStatus)
      : "all";

  const page = Math.max(1, Number(pick(params.page) ?? 1) || 1);
  const pageSize = 10;
  const memberId = pick(params.memberId)
  ? Number(pick(params.memberId))
  : undefined;

const result = await getSalesList({
  year,
  month,
  keyword,
  status,
  page,
  pageSize,
  memberId, 
});

  const totalPages = Math.max(1, Math.ceil(result.totalCount / result.pageSize));

  return (
    <div className="min-h-screen bg-slate-950 text-slate-50">
      <div className="pointer-events-none fixed inset-0 bg-[radial-gradient(circle_at_top,_#0ea5e9_0,_transparent_55%),radial-gradient(circle_at_bottom,_#6366f1_0,_transparent_55%)] opacity-60" />

      <header className="relative z-10 border-b border-slate-800 bg-slate-950/80 backdrop-blur">
        <div className="mx-auto max-w-6xl px-4 sm:px-6 py-4">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div className="min-w-0">
              <h1 className="text-lg font-semibold text-slate-50">売上一覧（請求ベース）</h1>
              <p className="mt-1 text-xs text-slate-400">
                請求金額を「売上」として一覧化し、入金済み・残額もあわせて確認できます。
              </p>
            </div>

            <div className="flex flex-wrap items-center gap-2 sm:gap-3">
              <Link href="/dashboards/admin" className="text-xs text-slate-300 hover:text-slate-100">
                ← 管理者トップへ
              </Link>
              <CurrentUserBadge />
              <LogoutButton />
            </div>
          </div>
        </div>
      </header>

      <main className="relative z-10 mx-auto max-w-6xl px-4 sm:px-6 py-6 sm:py-8 space-y-6">
        <SalesFiltersClient
          currentYear={currentYear}
          year={result.year}
          month={result.month}
          status={result.status}
          keyword={result.keyword}
        />
          
<div className="flex flex-wrap justify-end gap-2">
  <CsvExportButton
    estimatedCount={result.totalCount}
    memberName={result.memberName}
  />

  <Link
    href={`/sales/by-member?${buildByMemberQuery({
      year,
      month,
      keyword,
      page: 1,
      pageSize,
    })}`}
    className="inline-flex items-center gap-2 rounded-lg border border-slate-700
               bg-slate-900 px-3 py-2 text-xs text-slate-200
               hover:bg-slate-800"
  >
    顧客別に集計を見る →
  </Link>
</div>


          
{memberId && result.memberName && (
  <div className="text-xs text-slate-300">
    顧客:{" "}
    <span className="font-medium text-slate-100">
      {result.memberName}
    </span>{" "}
    で絞り込み中
    <Link href="/sales" className="ml-2 text-sky-300 underline">
      解除
    </Link>
  </div>
)}

        {/* サマリー */}
        <section className="rounded-2xl border border-slate-800 bg-slate-900/80 p-4 sm:p-5 shadow-md">
          <div className="grid gap-3 md:grid-cols-4">
            <div className="rounded-xl border border-slate-800 bg-slate-950/30 p-4">
              <p className="text-[11px] text-slate-400">請求合計（売上）</p>
              <p className="mt-2 text-lg sm:text-xl font-semibold break-words">
                {formatCurrency(result.summary.invoiceTotal)}
              </p>
            </div>
            <div className="rounded-xl border border-slate-800 bg-slate-950/30 p-4">
              <p className="text-[11px] text-slate-400">入金済み合計</p>
              <p className="mt-2 text-lg sm:text-xl font-semibold text-emerald-300 break-words">
                {formatCurrency(result.summary.paidTotal)}
              </p>
            </div>
            <div className="rounded-xl border border-slate-800 bg-slate-950/30 p-4">
              <p className="text-[11px] text-slate-400">残額（未回収）</p>
              <p className="mt-2 text-lg sm:text-xl font-semibold text-amber-300 break-words">
                {formatCurrency(result.summary.remainingTotal)}
              </p>
            </div>
            <div className="rounded-xl border border-slate-800 bg-slate-950/30 p-4">
              <p className="text-[11px] text-slate-400">回収率</p>
              <p className="mt-2 text-lg sm:text-xl font-semibold text-sky-200 break-words">
                {Number(result.summary.recoveryRate).toFixed(1)}%
              </p>
            </div>
          </div>
        </section>

        {/* 一覧 */}
        <section className="rounded-2xl border border-slate-800 bg-slate-900/80 p-4 sm:p-5 shadow-md space-y-4">
          {/* スマホ：カード */}
          <div className="space-y-3 md:hidden">
            {result.rows.map((r) => (
              <div key={r.invoiceId} className="rounded-xl border border-slate-800 bg-slate-950/40 p-4">
                <div className="flex items-start justify-between gap-3">
                  <div className="min-w-0">
                    <Link
                      href={`/invoices/${r.invoiceId}`} // ★ここ
                      className="text-sm font-semibold text-slate-100 hover:text-sky-200 break-all"
                    >
                      {r.invoiceNumber}
                    </Link>
                    <p className="mt-1 text-xs text-slate-300 truncate">{r.clientName}</p>
                  </div>
                  <span
                    className={`shrink-0 inline-flex items-center rounded-full border px-2 py-0.5 text-[11px] ${statusPillClass(
                      r.status
                    )}`}
                  >
                    {statusLabel(r.status)}
                  </span>
                </div>

                <dl className="mt-3 grid grid-cols-2 gap-3 text-xs">
                  <div>
                    <dt className="text-[11px] text-slate-400">発行日</dt>
                    <dd className="mt-1 text-slate-200">{formatDate(r.issuedAt)}</dd>
                  </div>
                  <div>
                    <dt className="text-[11px] text-slate-400">期限</dt>
                    <dd className="mt-1 text-slate-200">{formatDate(r.dueAt)}</dd>
                  </div>

                  <div className="col-span-2">
                    <div className="grid grid-cols-3 gap-2">
                      <div className="rounded-lg border border-slate-800 bg-slate-950/30 p-2">
                        <p className="text-[11px] text-slate-400">請求</p>
                        <p className="mt-1 text-slate-100 font-semibold break-words">
                          {formatCurrency(r.invoiceAmount)}
                        </p>
                      </div>
                      <div className="rounded-lg border border-slate-800 bg-slate-950/30 p-2">
                        <p className="text-[11px] text-slate-400">入金済</p>
                        <p className="mt-1 text-emerald-200 font-semibold break-words">
                          {formatCurrency(r.paidAmount)}
                        </p>
                      </div>
                      <div className="rounded-lg border border-slate-800 bg-slate-950/30 p-2">
                        <p className="text-[11px] text-slate-400">残額</p>
                        <p className="mt-1 text-amber-200 font-semibold break-words">
                          {formatCurrency(r.remainingAmount)}
                        </p>
                      </div>
                    </div>
                  </div>
                </dl>
              </div>
            ))}

            {result.rows.length === 0 && (
              <div className="rounded-xl border border-slate-800 bg-slate-950/40 px-4 py-8 text-center text-slate-400 text-[11px]">
                条件に一致するデータがありません。
              </div>
            )}
          </div>

          {/* md以上：テーブル */}
          <div className="hidden md:block overflow-x-auto">
            <div className="min-w-[980px] overflow-hidden rounded-xl border border-slate-800 bg-slate-950/40">
              <table className="min-w-full text-xs">
                <thead className="bg-slate-900/90">
                  <tr className="text-left text-[11px] text-slate-400">
                    <th className="px-4 py-2 font-medium">請求書</th>
                    <th className="px-4 py-2 font-medium">顧客</th>
                    <th className="px-4 py-2 font-medium">発行日</th>
                    <th className="px-4 py-2 font-medium">期限</th>
                    <th className="px-4 py-2 font-medium">ステータス</th>
                    <th className="px-4 py-2 font-medium text-right">請求</th>
                    <th className="px-4 py-2 font-medium text-right">入金済</th>
                    <th className="px-4 py-2 font-medium text-right">残額</th>
                    <th className="px-4 py-2 font-medium">最終入金</th>
                  </tr>
                </thead>
                <tbody>
                  {result.rows.map((r) => (
                    <tr key={r.invoiceId} className="border-t border-slate-800/80 hover:bg-slate-800/70">
                      <td className="px-4 py-2 text-slate-100">
                        <Link href={`/invoices/${r.invoiceId}`} className="hover:text-sky-200">
                          {r.invoiceNumber}
                        </Link>
                      </td>
                      <td className="px-4 py-2 text-slate-200">{r.clientName}</td>
                      <td className="px-4 py-2 text-slate-200">{formatDate(r.issuedAt)}</td>
                      <td className="px-4 py-2 text-slate-200">{formatDate(r.dueAt)}</td>
                      <td className="px-4 py-2">
                        <span className={`inline-flex items-center rounded-full border px-2 py-0.5 text-[11px] ${statusPillClass(r.status)}`}>
                          {statusLabel(r.status)}
                        </span>
                      </td>
                      <td className="px-4 py-2 text-right text-slate-100">{formatCurrency(r.invoiceAmount)}</td>
                      <td className="px-4 py-2 text-right text-emerald-200">{formatCurrency(r.paidAmount)}</td>
                      <td className="px-4 py-2 text-right text-amber-200">{formatCurrency(r.remainingAmount)}</td>
                      <td className="px-4 py-2 text-slate-200">
                        {r.lastPaidAt ? formatDate(r.lastPaidAt) : "—"}
                      </td>
                    </tr>
                  ))}

                  {result.rows.length === 0 && (
                    <tr>
                      <td colSpan={9} className="px-4 py-8 text-center text-slate-400 text-[11px]">
                        条件に一致するデータがありません。
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>

          {/* ページネーション */}
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <p className="text-[11px] text-slate-400">
              {result.page} / {totalPages} ページ
            </p>

            <div className="flex flex-col gap-2 sm:flex-row sm:gap-2">
              <Link
                aria-disabled={result.page <= 1}
href={`/sales?${buildQuery({
  year: result.year,
  month: result.month,
  status: result.status,
  keyword: result.keyword,
  page: Math.max(1, result.page - 1),
  pageSize: result.pageSize,
  memberId, // ★追加
})}`}
                className={`w-full sm:w-auto text-center rounded-lg border px-3 py-2 sm:py-1 text-xs ${
                  result.page <= 1
                    ? "border-slate-800 bg-slate-900/30 text-slate-500 pointer-events-none"
                    : "border-slate-700 bg-slate-900 text-slate-200 hover:bg-slate-800"
                }`}
              >
                ← 前へ
              </Link>

              <Link
                aria-disabled={result.page >= totalPages}
                href={`/sales?${buildQuery({
                  year: result.year,
                  month: result.month,
                  status: result.status,
                  keyword: result.keyword,
                  page: Math.min(totalPages, result.page + 1),
                  pageSize: result.pageSize,
                  memberId, 
                })}`}
                className={`w-full sm:w-auto text-center rounded-lg border px-3 py-2 sm:py-1 text-xs ${
                  result.page >= totalPages
                    ? "border-slate-800 bg-slate-900/30 text-slate-500 pointer-events-none"
                    : "border-slate-700 bg-slate-900 text-slate-200 hover:bg-slate-800"
                }`}
              >
                次へ →
              </Link>
            </div>
          </div>
        </section>
      </main>
    </div>
  );
}
