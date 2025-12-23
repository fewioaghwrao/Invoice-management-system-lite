// src/app/account/invoices/page.tsx
import Link from "next/link";
import { apiGetServer } from "@/lib/api.server";
import type { AccountInvoiceListDto } from "@/types/account-invoice";

const DEFAULT_PAGE_SIZE = 10;

type SearchParams = {
  year?: string;
  month?: string; // "all" or "1".."12"
  status?: string; // "all" | "unpaid" | "partial" | "paid"
  q?: string;
  page?: string;
};

function toInt(v: string | undefined, fallback: number) {
  const n = Number(v);
  return Number.isFinite(n) && n > 0 ? Math.trunc(n) : fallback;
}

function buildQueryString(params: Record<string, string | undefined>) {
  const usp = new URLSearchParams();
  for (const [k, v] of Object.entries(params)) {
    if (v !== undefined && v !== "") usp.set(k, v);
  }
  const qs = usp.toString();
  return qs ? `?${qs}` : "";
}

function formatYmd(iso: string) {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleDateString("ja-JP", { year: "numeric", month: "2-digit", day: "2-digit" });
}

function formatJPY(value: number) {
  return value.toLocaleString("ja-JP", {
    style: "currency",
    currency: "JPY",
    maximumFractionDigits: 0,
  });
}

function statusPillClass(statusName: string) {
  if (statusName.includes("入金済")) return "bg-emerald-500/10 text-emerald-300 ring-1 ring-emerald-900/40";
  if (statusName.includes("一部")) return "bg-amber-500/10 text-amber-300 ring-1 ring-amber-900/40";
  if (statusName.includes("未入金")) return "bg-rose-500/10 text-rose-300 ring-1 ring-rose-900/40";
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

  const totalPages = Math.max(1, Math.ceil((data.totalCount ?? 0) / DEFAULT_PAGE_SIZE));
  const yearOptions = data.availableYears?.length ? data.availableYears : [year];

  const hasOverdue = (data.items ?? []).some((x) => x.isOverdue && !x.statusName.includes("入金済"));

  const pagerLink = (nextPage: number) =>
    `/account/invoices${buildQueryString({
      year: String(year),
      month,
      status,
      q,
      page: String(nextPage),
    })}`;

  return (
    <div className="space-y-6">
      {/* タイトル（layoutの右カラムに収まる前提） */}
      <div className="flex items-end justify-between gap-4">
        <div className="min-w-0">
          <h2 className="truncate text-base sm:text-lg font-semibold text-slate-100">
            請求書一覧
          </h2>
          <p className="mt-1 text-xs sm:text-sm text-slate-400">
            自分宛ての請求書（支払期限・入金状況）を確認できます。
          </p>
        </div>

        {/* ここは任意：トップ導線を残したい場合だけ */}
        <Link
          href="/dashboards/member"
          className="hidden sm:inline-flex items-center rounded-lg border border-slate-700 bg-slate-900/40 px-3 py-2 text-xs font-medium text-slate-200 hover:bg-slate-800"
        >
          ← ダッシュボード
        </Link>
      </div>

      {/* 期限超過バナー */}
      {hasOverdue && (
        <div className="rounded-2xl border border-amber-900/40 bg-amber-950/40 p-4 text-amber-100">
          <div className="font-semibold">⚠ お支払いの遅れている請求があります</div>
          <div className="mt-1 text-sm text-amber-200/90">
            期限超過の行にラベルが表示されています。ご確認ください。
          </div>
        </div>
      )}

      {/* 検索フォーム */}
      <form
        action="/account/invoices"
        method="get"
        className="rounded-2xl border border-slate-800 bg-slate-900/70 p-4 shadow-sm"
      >
        <div className="grid gap-3 md:grid-cols-4">
          <div>
            <label className="block text-xs text-slate-400">年</label>
            <select
              name="year"
              defaultValue={String(year)}
              className="mt-1 w-full rounded-xl border border-slate-700 bg-slate-950/40 px-3 py-2 text-sm text-slate-100"
            >
              {yearOptions.map((y) => (
                <option key={y} value={String(y)}>
                  {y}年
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-xs text-slate-400">月</label>
            <select
              name="month"
              defaultValue={month}
              className="mt-1 w-full rounded-xl border border-slate-700 bg-slate-950/40 px-3 py-2 text-sm text-slate-100"
            >
              <option value="all">全て</option>
              {Array.from({ length: 12 }).map((_, i) => {
                const m = String(i + 1);
                return (
                  <option key={m} value={m}>
                    {m}月
                  </option>
                );
              })}
            </select>
          </div>

          <div>
            <label className="block text-xs text-slate-400">ステータス</label>
            <select
              name="status"
              defaultValue={status}
              className="mt-1 w-full rounded-xl border border-slate-700 bg-slate-950/40 px-3 py-2 text-sm text-slate-100"
            >
              <option value="all">全て</option>
              <option value="unpaid">未入金</option>
              <option value="partial">一部入金</option>
              <option value="paid">入金済み</option>
            </select>
          </div>

          <div>
            <label className="block text-xs text-slate-400">検索</label>
            <input
              name="q"
              defaultValue={q}
              placeholder="例：INV-2025 / 株式会社…"
              className="mt-1 w-full rounded-xl border border-slate-700 bg-slate-950/40 px-3 py-2 text-sm text-slate-100 placeholder:text-slate-600"
            />
          </div>
        </div>

        <div className="mt-3 flex items-center gap-3">
          <button
            type="submit"
            className="rounded-xl bg-blue-600 px-4 py-2 text-sm font-semibold text-white hover:bg-blue-700"
          >
            検索
          </button>

          <Link href="/account/invoices" className="text-sm text-slate-300 hover:text-slate-100">
            条件クリア
          </Link>

          <input type="hidden" name="page" value="1" />
        </div>

        {error && <p className="mt-3 text-sm text-rose-300">{error}</p>}
      </form>

      {/* 一覧 */}
      <section className="rounded-2xl border border-slate-800 bg-slate-900/70 shadow-sm overflow-hidden">
        <div className="px-4 py-3 border-b border-slate-800 flex items-center justify-between">
          <div className="text-sm text-slate-200">{data.totalCount.toLocaleString("ja-JP")} 件</div>
          <div className="text-xs text-slate-500">※ PDF表示は別タブで開きます</div>
        </div>

        <div className="overflow-x-auto">
          <table className="min-w-full text-sm">
            <thead className="bg-slate-950/40 text-slate-400">
              <tr>
                <th className="px-4 py-3 text-left font-semibold">請求書番号</th>
                <th className="px-4 py-3 text-left font-semibold">請求日</th>
                <th className="px-4 py-3 text-left font-semibold">支払期限</th>
                <th className="px-4 py-3 text-right font-semibold">請求金額</th>
                <th className="px-4 py-3 text-left font-semibold">ステータス</th>
                <th className="px-4 py-3 text-right font-semibold">操作</th>
              </tr>
            </thead>

            <tbody className="divide-y divide-slate-800">
              {data.items.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-4 py-10 text-center text-slate-500">
                    条件に一致する請求書がありません。
                  </td>
                </tr>
              ) : (
                data.items.map((inv) => (
                  <tr key={inv.id} className="hover:bg-slate-950/30">
                    <td className="px-4 py-3">
                      <div className="font-semibold text-slate-100">{inv.invoiceNumber}</div>
                      {inv.isOverdue && !inv.statusName.includes("入金済") && (
                        <div className="mt-1 inline-flex items-center rounded-full bg-amber-500/10 px-2 py-0.5 text-xs font-semibold text-amber-200 ring-1 ring-amber-900/40">
                          ⚠ 期限超過
                        </div>
                      )}
                    </td>

                    <td className="px-4 py-3 text-slate-200">{formatYmd(inv.issuedAt)}</td>
                    <td className="px-4 py-3 text-slate-200">{formatYmd(inv.dueAt)}</td>

                    <td className="px-4 py-3 text-right font-semibold text-slate-100">
                      {formatJPY(inv.totalAmount)}
                    </td>

                    <td className="px-4 py-3">
                      <span className={`inline-flex items-center rounded-full px-2 py-1 text-xs font-semibold ${statusPillClass(inv.statusName)}`}>
                        {inv.statusName}
                      </span>
                    </td>

                    <td className="px-4 py-3">
                      <div className="flex items-center justify-end gap-3">
                        <Link
                          href={`/account/invoices/${inv.id}`}
                          className="text-blue-300 hover:text-blue-200 font-semibold"
                        >
                          詳細
                        </Link>

                        <a
                          href={`/account/invoices/${inv.id}/pdf`}
                          target="_blank"
                          rel="noreferrer"
                          className="text-slate-200 hover:text-slate-100"
                          title="PDF表示"
                        >
                          📄 PDF
                        </a>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {/* ページャ */}
        <div className="px-4 py-3 border-t border-slate-800 flex items-center justify-between">
          <div className="text-xs text-slate-500">
            {page} / {totalPages} ページ
          </div>

          <div className="flex items-center gap-2">
            <Link
              aria-disabled={page <= 1}
              className={`rounded-xl px-3 py-2 text-sm border ${
                page <= 1
                  ? "pointer-events-none border-slate-800 text-slate-600"
                  : "border-slate-700 text-slate-200 hover:bg-slate-950/30"
              }`}
              href={pagerLink(Math.max(1, page - 1))}
            >
              前へ
            </Link>

            <Link
              aria-disabled={page >= totalPages}
              className={`rounded-xl px-3 py-2 text-sm border ${
                page >= totalPages
                  ? "pointer-events-none border-slate-800 text-slate-600"
                  : "border-slate-700 text-slate-200 hover:bg-slate-950/30"
              }`}
              href={pagerLink(Math.min(totalPages, page + 1))}
            >
              次へ
            </Link>
          </div>
        </div>
      </section>

      {/* モバイル向け：下にも戻る導線（任意） */}
      <div className="sm:hidden">
        <Link
          href="/dashboards/member"
          className="inline-flex w-full items-center justify-center rounded-xl border border-slate-700 bg-slate-900/50 px-3 py-2 text-sm font-semibold text-slate-200 hover:bg-slate-800"
        >
          ← ダッシュボードへ戻る
        </Link>
      </div>
    </div>
  );
}
