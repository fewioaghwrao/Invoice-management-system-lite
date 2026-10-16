// src/app/account/invoices/page.tsx
import Link from "next/link";
import { apiGetServer } from "@/lib/api.server";
import type { AccountInvoiceListDto } from "@/types/account-invoice";

const DEFAULT_PAGE_SIZE = 10;

type SearchParams = {
  year?: string;
  month?: string;
  status?: string;
  q?: string;
  page?: string;
};

function toInt(v: string | undefined, fallback: number) {
  const n = Number(v);
  return Number.isFinite(n) && n > 0 ? Math.trunc(n) : fallback;
}

function buildQueryString(params: Record<string, string | undefined>) {
  const usp = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== "") usp.set(key, value);
  }
  const qs = usp.toString();
  return qs ? `?${qs}` : "";
}

function formatYmd(iso: string) {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleDateString("ja-JP", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  });
}

function formatJPY(value: number) {
  return value.toLocaleString("ja-JP", {
    style: "currency",
    currency: "JPY",
    maximumFractionDigits: 0,
  });
}

function statusPillClass(statusName: string) {
  if (statusName.includes("入金済"))
    return "bg-emerald-500/10 text-emerald-300 ring-1 ring-emerald-900/40";
  if (statusName.includes("一部"))
    return "bg-amber-500/10 text-amber-300 ring-1 ring-amber-900/40";
  if (statusName.includes("未入金"))
    return "bg-rose-500/10 text-rose-300 ring-1 ring-rose-900/40";
  return "bg-slate-500/10 text-slate-200 ring-1 ring-slate-800";
}

export default async function AccountInvoicesPage({
  searchParams,
}: {
  searchParams: Promise<SearchParams>;
}) {
  const sp = await searchParams;

  const year = toInt(sp.year, new Date().getFullYear());
  const month = sp.month ?? "all";
  const status = sp.status ?? "all";
  const q = sp.q ?? "";
  const page = toInt(sp.page, 1);

  let data: AccountInvoiceListDto;
  let error: string | null = null;

  try {
    data = await apiGetServer("/api/members/me/invoices", {
      year,
      month,
      status,
      q,
      page,
      pageSize: DEFAULT_PAGE_SIZE,
    });
  } catch (e) {
    error = e instanceof Error ? e.message : "請求書一覧の取得に失敗しました。";
    data = {
      year,
      availableYears: [year],
      month,
      status,
      q,
      page,
      pageSize: DEFAULT_PAGE_SIZE,
      totalCount: 0,
      items: [],
    };
  }

  const totalPages = Math.max(
    1,
    Math.ceil((data.totalCount ?? 0) / DEFAULT_PAGE_SIZE)
  );
  const yearOptions = data.availableYears?.length
    ? data.availableYears
    : [year];

  // 判定フラグは既存のAPI応答を維持（業務上の期限判定は今回変更しない）
  const isUnpaidOverdue = (inv: (typeof data.items)[number]) =>
    inv.isOverdue && !inv.statusName.includes("入金済");

  const hasOverdue = (data.items ?? []).some(isUnpaidOverdue);

  const pagerLink = (nextPage: number) =>
    `/account/invoices${buildQueryString({
      year: String(year),
      month,
      status,
      q,
      page: String(nextPage),
    })}`;

  return (
    <div className="min-w-0 space-y-5 sm:space-y-6">
      {/* ページ見出しは共通ヘッダーで表示。戻り先も共通ヘッダーと左メニューに統一 */}
      <p className="text-sm text-slate-300">
        請求書の金額・支払期限・入金状況を確認できます。
      </p>

      {hasOverdue && (
        <div role="status" className="rounded-2xl border border-amber-900/40 bg-amber-950/40 p-4 text-amber-100">
          <div className="text-sm font-semibold">⚠ お支払いの遅れている請求があります</div>
          <p className="mt-1 text-sm text-amber-200/90">
            期限超過の請求書にラベルを表示しています。ご確認ください。
          </p>
        </div>
      )}

      {/* 検索条件：小画面1列 → タブレット2列 → PC4列 */}
      <form
        action="/account/invoices"
        method="get"
        role="search"
        className="rounded-2xl border border-slate-800 bg-slate-900/70 p-4 shadow-sm"
      >
        <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
          <div className="min-w-0">
            <label htmlFor="invoice-year" className="block text-sm text-slate-300">年</label>
            <select
              id="invoice-year"
              name="year"
              defaultValue={String(year)}
              className="mt-1 w-full min-h-11 rounded-xl border border-slate-700 bg-slate-950/40 px-3 py-2 text-sm text-slate-100"
            >
              {yearOptions.map((y) => (
                <option key={y} value={String(y)}>{y}年</option>
              ))}
            </select>
          </div>

          <div className="min-w-0">
            <label htmlFor="invoice-month" className="block text-sm text-slate-300">月</label>
            <select
              id="invoice-month"
              name="month"
              defaultValue={month}
              className="mt-1 w-full min-h-11 rounded-xl border border-slate-700 bg-slate-950/40 px-3 py-2 text-sm text-slate-100"
            >
              <option value="all">すべて</option>
              {Array.from({ length: 12 }, (_, i) => String(i + 1)).map((m) => (
                <option key={m} value={m}>{m}月</option>
              ))}
            </select>
          </div>

          <div className="min-w-0">
            <label htmlFor="invoice-status" className="block text-sm text-slate-300">ステータス</label>
            <select
              id="invoice-status"
              name="status"
              defaultValue={status}
              className="mt-1 w-full min-h-11 rounded-xl border border-slate-700 bg-slate-950/40 px-3 py-2 text-sm text-slate-100"
            >
              <option value="all">すべて</option>
              <option value="unpaid">未入金</option>
              <option value="partial">一部入金</option>
              <option value="paid">入金済み</option>
            </select>
          </div>

          <div className="min-w-0">
            <label htmlFor="invoice-query" className="block text-sm text-slate-300">検索</label>
            <input
              id="invoice-query"
              name="q"
              defaultValue={q}
              placeholder="請求書番号など"
              className="mt-1 w-full min-h-11 rounded-xl border border-slate-700 bg-slate-950/40 px-3 py-2 text-sm text-slate-100 placeholder:text-slate-500"
            />
          </div>
        </div>

        <div className="mt-4 flex flex-wrap items-center gap-3">
          <button
            type="submit"
            className="min-h-11 rounded-xl bg-blue-600 px-5 py-2 text-sm font-semibold text-white hover:bg-blue-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sky-400"
          >
            検索
          </button>
          <Link
            href="/account/invoices"
            className="inline-flex min-h-11 items-center px-2 text-sm text-slate-300 hover:text-white"
          >
            条件クリア
          </Link>
          <input type="hidden" name="page" value="1" />
        </div>
        {error && <p role="alert" className="mt-3 text-sm text-rose-300">{error}</p>}
      </form>

      {/* 同じ請求書データを、PCではテーブル、スマートフォンではカードで表示 */}
      <section aria-label="請求書の検索結果" className="min-w-0 overflow-hidden rounded-2xl border border-slate-800 bg-slate-900/70 shadow-sm">
        <div className="flex flex-wrap items-center justify-between gap-2 border-b border-slate-800 px-4 py-3">
          <h2 className="text-sm font-semibold text-slate-100">
            検索結果：{data.totalCount.toLocaleString("ja-JP")} 件
          </h2>
          <span className="text-xs text-slate-400">PDFは別タブで開きます</span>
        </div>

        {/* スマートフォン版 */}
        <div className="divide-y divide-slate-800 md:hidden">
          {data.items.length === 0 ? (
            <p className="px-4 py-8 text-center text-sm text-slate-400">条件に一致する請求書がありません。</p>
          ) : (
            data.items.map((inv) => (
              <article key={inv.id} className="space-y-3 px-4 py-4">
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <p className="min-w-0 break-words text-sm font-semibold text-slate-100 [overflow-wrap:anywhere]">
                    {inv.invoiceNumber}
                  </p>
                  <span className={`inline-flex shrink-0 items-center rounded-full px-2 py-1 text-xs font-semibold ${statusPillClass(inv.statusName)}`}>
                    {inv.statusName}
                  </span>
                </div>
                {isUnpaidOverdue(inv) && (
                  <span className="inline-flex rounded-full bg-amber-500/10 px-2 py-1 text-xs font-semibold text-amber-200 ring-1 ring-amber-900/40">⚠ 期限超過</span>
                )}
                <dl className="grid grid-cols-2 gap-x-3 gap-y-2 text-sm">
                  <div>
                    <dt className="text-xs text-slate-400">請求日</dt>
                    <dd className="mt-1 whitespace-nowrap text-slate-200">{formatYmd(inv.issuedAt)}</dd>
                  </div>
                  <div>
                    <dt className="text-xs text-slate-400">支払期限</dt>
                    <dd className="mt-1 whitespace-nowrap text-slate-200">{formatYmd(inv.dueAt)}</dd>
                  </div>
                  <div className="col-span-2">
                    <dt className="text-xs text-slate-400">請求金額</dt>
                    <dd className="mt-1 text-base font-semibold text-slate-100">{formatJPY(inv.totalAmount)}</dd>
                  </div>
                </dl>
                <div className="flex flex-wrap justify-end gap-2 border-t border-slate-800/70 pt-3">
                  <Link
                    href={`/account/invoices/${inv.id}`}
                    className="inline-flex min-h-11 items-center justify-center rounded-lg border border-sky-700/50 px-4 text-sm font-semibold text-sky-200 hover:bg-slate-800"
                  >
                    詳細
                  </Link>
                  <a
                    href={`/account/invoices/${inv.id}/pdf`}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="inline-flex min-h-11 items-center justify-center rounded-lg border border-slate-700 px-4 text-sm text-slate-200 hover:bg-slate-800"
                  >
                    📄 PDF
                  </a>
                </div>
              </article>
            ))
          )}
        </div>

        {/* PC・タブレット版：十分な最低幅を取り、必要な場合だけ表内を横スクロール */}
        <div className="hidden overflow-x-auto md:block">
          <table className="w-full min-w-[900px] text-sm">
            <thead className="bg-slate-950/40 text-slate-300">
              <tr>
                <th scope="col" className="min-w-[210px] px-3 py-3 text-left font-semibold">請求書番号</th>
                <th scope="col" className="whitespace-nowrap px-3 py-3 text-left font-semibold">請求日</th>
                <th scope="col" className="whitespace-nowrap px-3 py-3 text-left font-semibold">支払期限</th>
                <th scope="col" className="whitespace-nowrap px-3 py-3 text-right font-semibold">請求金額</th>
                <th scope="col" className="whitespace-nowrap px-3 py-3 text-left font-semibold">ステータス</th>
                <th scope="col" className="whitespace-nowrap px-3 py-3 text-right font-semibold">操作</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800">
              {data.items.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-4 py-10 text-center text-slate-400">条件に一致する請求書がありません。</td>
                </tr>
              ) : (
                data.items.map((inv) => (
                  <tr key={inv.id} className="hover:bg-slate-950/30">
                    <td className="px-3 py-3 align-middle">
                      <div className="whitespace-nowrap font-semibold text-slate-100">{inv.invoiceNumber}</div>
                      {isUnpaidOverdue(inv) && (
                        <span className="mt-1 inline-flex whitespace-nowrap rounded-full bg-amber-500/10 px-2 py-0.5 text-xs font-semibold text-amber-200 ring-1 ring-amber-900/40">
                          ⚠ 期限超過
                        </span>
                      )}
                    </td>
                    <td className="whitespace-nowrap px-3 py-3 text-slate-200">{formatYmd(inv.issuedAt)}</td>
                    <td className="whitespace-nowrap px-3 py-3 text-slate-200">{formatYmd(inv.dueAt)}</td>
                    <td className="whitespace-nowrap px-3 py-3 text-right font-semibold text-slate-100">{formatJPY(inv.totalAmount)}</td>
                    <td className="whitespace-nowrap px-3 py-3">
                      <span className={`inline-flex whitespace-nowrap rounded-full px-2 py-1 text-xs font-semibold ${statusPillClass(inv.statusName)}`}>
                        {inv.statusName}
                      </span>
                    </td>
                    <td className="whitespace-nowrap px-3 py-3">
                      <div className="flex items-center justify-end gap-3">
                        <Link href={`/account/invoices/${inv.id}`} className="font-semibold text-sky-300 hover:text-sky-200">詳細</Link>
                        <a href={`/account/invoices/${inv.id}/pdf`} target="_blank" rel="noopener noreferrer" className="text-slate-200 hover:text-white" title="PDF表示">📄 PDF</a>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {/* ページネーション：検索条件を維持 */}
        <div className="flex flex-wrap items-center justify-between gap-3 border-t border-slate-800 px-4 py-3">
          <span className="text-sm text-slate-400">{page} / {totalPages} ページ</span>
          <div className="flex items-center gap-2">
            <Link
              href={pagerLink(Math.max(1, page - 1))}
              aria-disabled={page <= 1}
              tabIndex={page <= 1 ? -1 : undefined}
              className={`inline-flex min-h-11 items-center rounded-xl border px-4 text-sm ${
                page <= 1
                  ? "pointer-events-none border-slate-800 text-slate-500"
                  : "border-slate-700 text-slate-200 hover:bg-slate-950/30"
              }`}
            >
              前へ
            </Link>
            <Link
              href={pagerLink(Math.min(totalPages, page + 1))}
              aria-disabled={page >= totalPages}
              tabIndex={page >= totalPages ? -1 : undefined}
              className={`inline-flex min-h-11 items-center rounded-xl border px-4 text-sm ${
                page >= totalPages
                  ? "pointer-events-none border-slate-800 text-slate-500"
                  : "border-slate-700 text-slate-200 hover:bg-slate-950/30"
              }`}
            >
              次へ
            </Link>
          </div>
        </div>
      </section>
    </div>
  );
}
