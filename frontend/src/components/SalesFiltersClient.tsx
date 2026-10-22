"use client";

import { useRouter, useSearchParams } from "next/navigation";
import Link from "next/link";

type InvoiceStatus = "UNPAID" | "PARTIAL" | "PAID";

function buildQuery(base: URLSearchParams, patch: Record<string, string>) {
  const sp = new URLSearchParams(base.toString());
  Object.entries(patch).forEach(([k, v]) => {
    if (v === "") sp.delete(k);
    else sp.set(k, v);
  });
  return sp.toString();
}

export default function SalesFiltersClient(props: {
  currentYear: number;
  year: number;
  month: number | "all";
  status: InvoiceStatus | "all";
  keyword: string;
}) {
  const router = useRouter();
  const sp = useSearchParams();

  const push = (patch: Record<string, string>) => {
    const q = buildQuery(new URLSearchParams(sp.toString()), {
      ...patch,
      page: "1", // フィルタ変更時は1ページ目に戻す
    });
    router.push(`/sales?${q}`);
  };

  return (
    <section className="min-w-0 rounded-2xl border border-slate-800 bg-slate-900/80 p-3 shadow-md sm:p-4 xl:p-5">
      <div className="grid min-w-0 grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-3">
      {/* 年 */}
      <div className="flex min-w-0 flex-col gap-1">
        <label htmlFor="sales-year" className="text-[11px] text-slate-400">年</label>
        <select
          id="sales-year"
          defaultValue={String(props.year)}
          className="h-10 min-w-0 w-full rounded-lg border border-slate-700 bg-slate-950/40 px-3 text-xs text-slate-100 focus:border-sky-500 focus:outline-none focus:ring-1 focus:ring-sky-500"
          onChange={(e) => push({ year: e.target.value })}
        >
          {[props.currentYear - 1, props.currentYear, props.currentYear + 1].map((y) => (
            <option key={y} value={String(y)}>
              {y}年
            </option>
          ))}
        </select>
      </div>

      {/* 月 */}
      <div className="flex min-w-0 flex-col gap-1">
        <label htmlFor="sales-month" className="text-[11px] text-slate-400">月</label>
        <select
          id="sales-month"
          defaultValue={String(props.month)}
          className="h-10 min-w-0 w-full rounded-lg border border-slate-700 bg-slate-950/40 px-3 text-xs text-slate-100 focus:border-sky-500 focus:outline-none focus:ring-1 focus:ring-sky-500"
          onChange={(e) => push({ month: e.target.value })}
        >
          <option value="all">すべて</option>
          {Array.from({ length: 12 }, (_, i) => i + 1).map((m) => (
            <option key={m} value={String(m)}>
              {m}月
            </option>
          ))}
        </select>
      </div>

      {/* ステータス */}
      <div className="flex min-w-0 flex-col gap-1">
        <label htmlFor="sales-status" className="text-[11px] text-slate-400">ステータス</label>
        <select
          id="sales-status"
          defaultValue={String(props.status).toLowerCase()}
          className="h-10 min-w-0 w-full rounded-lg border border-slate-700 bg-slate-950/40 px-3 text-xs text-slate-100 focus:border-sky-500 focus:outline-none focus:ring-1 focus:ring-sky-500"
          onChange={(e) => push({ status: e.target.value })}
        >
          <option value="all">すべて</option>
          <option value="unpaid">未入金</option>
          <option value="partial">一部入金</option>
          <option value="paid">入金済</option>
        </select>
      </div>

      {/* 検索 */}
      <form
        className="grid min-w-0 gap-2 sm:col-span-2 sm:grid-cols-[minmax(0,1fr)_auto] sm:items-end xl:col-span-3"
        onSubmit={(e) => {
          e.preventDefault();
          const fd = new FormData(e.currentTarget);
          push({ q: String(fd.get("q") ?? "") });
        }}
      >
        <div className="flex min-w-0 flex-col gap-1">
          <label htmlFor="sales-keyword" className="text-[11px] text-slate-400">検索（請求書ID / 顧客名）</label>
          <input
            id="sales-keyword"
            name="q"
            defaultValue={props.keyword}
            className="h-10 min-w-0 w-full rounded-lg border border-slate-700 bg-slate-950/40 px-3 text-xs text-slate-100 placeholder:text-slate-500 focus:border-sky-500 focus:outline-none focus:ring-1 focus:ring-sky-500"
            placeholder="例: INV-002 / クライアントA"
          />
        </div>

        <div className="flex min-w-0 gap-2">
          <button
            type="submit"
            className="inline-flex h-10 min-w-0 flex-1 items-center justify-center whitespace-nowrap rounded-lg border border-slate-600 bg-slate-900 px-4 text-xs font-medium text-slate-200 hover:bg-slate-800 sm:flex-none"
          >
            検索
          </button>

          <Link
            href={`/sales?year=${props.year}&month=all&status=all&page=1`}
            className="inline-flex h-10 min-w-0 flex-1 items-center justify-center whitespace-nowrap rounded-lg border border-slate-600 bg-slate-900 px-4 text-xs font-medium text-slate-200 hover:bg-slate-800 sm:flex-none"
          >
            リセット
          </Link>
        </div>
      </form>
      </div>
    </section>
  );
}
