import Link from "next/link";
import { apiGetServer } from "@/lib/api.server";
import CsvExportButton from "@/components/CsvExportButton";

/* ========= 型 ========= */

type SalesByMemberRow = {
  memberId: number;
  memberName: string;
  invoiceTotal: number;
  paidTotal: number;
  remainingTotal: number;
  recoveryRate: number;
};

type SalesByMemberResult = {
  year: number;
  month: number | "all";
  keyword: string;
  page: number;
  pageSize: number;
  totalCount: number;
  rows: SalesByMemberRow[];
  summary: {
    invoiceTotal: number;
    paidTotal: number;
    remainingTotal: number;
    recoveryRate: number;
  };
};

/* ========= util ========= */

function formatCurrency(v: number) {
  return v.toLocaleString("ja-JP", {
    style: "currency",
    currency: "JPY",
    maximumFractionDigits: 0,
  });
}

function pick(v: string | string[] | undefined) {
  return Array.isArray(v) ? v[0] : v;
}

function buildQuery(p: {
  year: number;
  month: number | "all";
  keyword: string;
  page: number;
  pageSize: number;
}) {
  const usp = new URLSearchParams();
  usp.set("year", String(p.year));
  if (p.month !== "all") usp.set("month", String(p.month));
  if (p.keyword) usp.set("q", p.keyword);
  usp.set("page", String(p.page));
  usp.set("pageSize", String(p.pageSize));
  return usp.toString();
}

/* ========= API ========= */

async function getSalesByMember(params: {
  year: number;
  month: number | "all";
  keyword: string;
  page: number;
  pageSize: number;
}): Promise<SalesByMemberResult> {
  const qs = buildQuery(params);
  return apiGetServer(`/api/sales/by-member?${qs}`);
}

/* ========= Page ========= */

export default async function SalesByMemberPage({
  searchParams,
}: {
  searchParams: Promise<{ [key: string]: string | string[] | undefined }>;
}) {
  const sp = await searchParams;

  const now = new Date();
  const currentYear = now.getFullYear();

  const year = Number(pick(sp.year) ?? currentYear) || currentYear;
  const monthParam = pick(sp.month) ?? "all";
  const month = monthParam === "all" ? "all" : Number(monthParam) || "all";
  const keyword = String(pick(sp.q) ?? "");
  const page = Math.max(1, Number(pick(sp.page) ?? 1) || 1);
  const pageSize = 10;

  const result = await getSalesByMember({
    year,
    month,
    keyword,
    page,
    pageSize,
  });

  const totalPages = Math.max(
    1,
    Math.ceil(result.totalCount / result.pageSize)
  );

  return (
    <div className="w-full min-w-0 max-w-full text-slate-50">
      {/* 共通アプリヘッダーとサイドバーは sales/layout.tsx 側で表示 */}
      <header className="min-w-0 rounded-2xl border border-slate-800 bg-slate-900/60 shadow-sm">
        <div className="min-w-0 px-4 py-4 sm:px-5 sm:py-5">
          <nav aria-label="パンくずリスト" className="flex flex-wrap items-center gap-2 text-xs text-slate-400">
            <Link href="/dashboards/admin" className="hover:text-sky-300">ダッシュボード</Link>
            <span aria-hidden="true">/</span>
            <Link href="/sales" className="hover:text-sky-300">売上一覧</Link>
            <span aria-hidden="true">/</span>
            <span aria-current="page">顧客別集計</span>
          </nav>
          <h1 className="mt-2 text-lg font-semibold text-slate-50 sm:text-xl">売上集計（顧客別）</h1>
          <p className="mt-1 text-sm leading-relaxed text-slate-400">
            顧客ごとの請求額・入金額・未回収額と回収率を確認できます。
          </p>
        </div>
      </header>

      <main className="min-w-0 space-y-5 pt-5 sm:space-y-6 sm:pt-6">
        <div className="grid gap-2 sm:flex sm:justify-end sm:gap-2">
  <CsvExportButton
    exportPath="/api/sales/by-member/export"
    fileNamePrefix="sales_by_member"
    estimatedCount={result.totalCount}
  />

  <Link
    href={`/sales?${buildQuery({ year, month, keyword, page: 1, pageSize })}`}
    className="inline-flex w-full sm:w-auto items-center justify-center gap-2 rounded-lg
               border border-slate-700 bg-slate-900 px-3 py-2 text-xs text-slate-200 hover:bg-slate-800"
  >
    ← 請求書ベースに戻る
  </Link>

</div>
        {/* サマリー */}
        <section className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
          <SummaryCard label="請求合計" value={formatCurrency(result.summary.invoiceTotal)} />
          <SummaryCard label="入金済み" value={formatCurrency(result.summary.paidTotal)} color="emerald" />
          <SummaryCard label="未回収" value={formatCurrency(result.summary.remainingTotal)} color="amber" />
          <SummaryCard label="回収率" value={`${result.summary.recoveryRate.toFixed(1)}%`} color="sky" />
        </section>

        {/* 一覧：スマホはカード表示、タブレット以上はテーブル */}
        <section className="min-w-0 space-y-3">
          <div className="space-y-3 sm:hidden">
            {result.rows.map((r) => (
              <article key={r.memberId} className="min-w-0 rounded-2xl border border-slate-800 bg-slate-900/80 p-4">
                <Link href={`/sales?memberId=${r.memberId}`} className="block break-words text-sm font-semibold text-sky-200 hover:underline">
                  {r.memberName}
                </Link>
                <dl className="mt-3 grid grid-cols-2 gap-3 text-xs">
                  <div className="min-w-0">
                    <dt className="text-slate-400">請求合計</dt>
                    <dd className="mt-1 break-words font-medium text-slate-100">{formatCurrency(r.invoiceTotal)}</dd>
                  </div>
                  <div className="min-w-0">
                    <dt className="text-slate-400">入金済</dt>
                    <dd className="mt-1 break-words font-medium text-emerald-300">{formatCurrency(r.paidTotal)}</dd>
                  </div>
                  <div className="min-w-0">
                    <dt className="text-slate-400">未回収</dt>
                    <dd className="mt-1 break-words font-medium text-amber-300">{formatCurrency(r.remainingTotal)}</dd>
                  </div>
                  <div className="min-w-0">
                    <dt className="text-slate-400">回収率</dt>
                    <dd className="mt-1 font-medium text-slate-100">{r.recoveryRate.toFixed(1)}%</dd>
                  </div>
                </dl>
              </article>
            ))}
            {result.rows.length === 0 && (
              <p className="rounded-2xl border border-slate-800 bg-slate-900/80 px-4 py-8 text-center text-xs text-slate-400">
                データがありません
              </p>
            )}
          </div>

          <div className="hidden min-w-0 max-w-full overflow-x-auto rounded-2xl border border-slate-800 bg-slate-900/80 sm:block">
            <table className="min-w-[640px] w-full text-xs">
              <thead className="bg-slate-900">
                <tr className="text-slate-400">
                  <th className="px-4 py-3 text-left">顧客</th>
                  <th className="px-4 py-3 text-right">請求合計</th>
                  <th className="px-4 py-3 text-right">入金済</th>
                  <th className="px-4 py-3 text-right">未回収</th>
                  <th className="px-4 py-3 text-right">回収率</th>
                </tr>
              </thead>
              <tbody>
                {result.rows.map((r) => (
                  <tr key={r.memberId} className="border-t border-slate-800 hover:bg-slate-800/70">
                    <td className="px-4 py-3 text-slate-100">
                      <Link href={`/sales?memberId=${r.memberId}`} className="hover:text-sky-300">
                        {r.memberName}
                      </Link>
                    </td>
                    <td className="px-4 py-3 text-right">{formatCurrency(r.invoiceTotal)}</td>
                    <td className="px-4 py-3 text-right text-emerald-300">{formatCurrency(r.paidTotal)}</td>
                    <td className="px-4 py-3 text-right text-amber-300">{formatCurrency(r.remainingTotal)}</td>
                    <td className="px-4 py-3 text-right">{r.recoveryRate.toFixed(1)}%</td>
                  </tr>
                ))}
                {result.rows.length === 0 && (
                  <tr>
                    <td colSpan={5} className="px-4 py-8 text-center text-slate-400">
                      データがありません
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </section>

        {/* ページネーション */}
        <div className="flex flex-col gap-3 text-xs sm:flex-row sm:items-center sm:justify-between">
          <span className="text-slate-400">
            {result.page} / {totalPages} ページ
          </span>

          <div className="flex flex-wrap gap-2">
            <Link
              href={`/sales/by-member?${buildQuery({
                year,
                month,
                keyword,
                page: Math.max(1, page - 1),
                pageSize,
              })}`}
              className={`px-3 py-1 rounded border ${
                page <= 1
                  ? "border-slate-800 text-slate-500 pointer-events-none"
                  : "border-slate-700 hover:bg-slate-800"
              }`}
            >
              ← 前
            </Link>

            <Link
              href={`/sales/by-member?${buildQuery({
                year,
                month,
                keyword,
                page: Math.min(totalPages, page + 1),
                pageSize,
              })}`}
              className={`px-3 py-1 rounded border ${
                page >= totalPages
                  ? "border-slate-800 text-slate-500 pointer-events-none"
                  : "border-slate-700 hover:bg-slate-800"
              }`}
            >
              次 →
            </Link>
          </div>
        </div>
      </main>
    </div>
  );
}

/* ========= 小部品 ========= */

function SummaryCard({
  label,
  value,
  color,
}: {
  label: string;
  value: string;
  color?: "emerald" | "amber" | "sky";
}) {
  const colorClass =
    color === "emerald"
      ? "text-emerald-300"
      : color === "amber"
      ? "text-amber-300"
      : color === "sky"
      ? "text-sky-300"
      : "text-slate-100";

  return (
    <div className="rounded-xl border border-slate-800 bg-slate-900/80 p-4">
      <p className="text-[11px] text-slate-400">{label}</p>
<p className={`mt-2 text-lg font-semibold break-words ${colorClass}`}>{value}</p>
    </div>
  );
}
