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
    <section className="rounded-2xl border border-slate-800 bg-slate-900/80 p-5 shadow-md">
      <div className="flex flex-wrap items-end gap-3">
        {/* 年 */}
        <div className="flex flex-col gap-1">
          <label className="text-[11px] text-slate-400">年</label>
          <select
            defaultValue={String(props.year)}
            className="h-9 rounded-lg border border-slate-700 bg-slate-950/40 px-3 text-xs text-slate-100"
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
        <div className="flex flex-col gap-1">
          <label className="text-[11px] text-slate-400">月</label>
          <select
            defaultValue={String(props.month)}
            className="h-9 rounded-lg border border-slate-700 bg-slate-950/40 px-3 text-xs text-slate-100"
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
        <div className="flex flex-col gap-1">
          <label className="text-[11px] text-slate-400">ステータス</label>
          <select
            defaultValue={String(props.status).toLowerCase()}
            className="h-9 rounded-lg border border-slate-700 bg-slate-950/40 px-3 text-xs text-slate-100"
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
          className="flex items-end gap-2"
          onSubmit={(e) => {
            e.preventDefault();
            const fd = new FormData(e.currentTarget);
            push({ q: String(fd.get("q") ?? "") });
          }}
        >
          <div className="flex flex-col gap-1">
            <label className="text-[11px] text-slate-400">検索（請求書ID / 顧客名）</label>
            <input
              name="q"
              defaultValue={props.keyword}
              className="h-9 w-72 max-w-[70vw] rounded-lg border border-slate-700 bg-slate-950/40 px-3 text-xs text-slate-100 placeholder:text-slate-500"
              placeholder="例: INV-002 / クライアントA"
            />
          </div>

          <button
            type="submit"
            className="h-9 rounded-lg border border-slate-600 bg-slate-900 px-4 text-xs font-medium text-slate-200 hover:bg-slate-800"
          >
            検索
          </button>

          <Link
            href={`/sales?year=${props.year}&month=all&status=all&page=1`}
            className="h-9 inline-flex items-center justify-center rounded-lg border border-slate-600 bg-slate-900 px-4 text-xs font-medium text-slate-200 hover:bg-slate-800"
          >
            リセット
          </Link>
        </form>
      </div>
    </section>
  );
}
