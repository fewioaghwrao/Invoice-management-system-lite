// src/app/invoices/page.tsx
import Link from "next/link";
import { apiGetServer } from "@/lib/api.server";
import type { InvoiceDto } from "@/types/invoice";

const PAGE_SIZE = 5;

// URL の searchParams で受け取る型
type SearchParams = {
  invoiceNumber?: string;
  memberName?: string;
  statusId?: string;
  fromInvoiceDate?: string;
  toInvoiceDate?: string;
  page?: string;
};

function buildQueryString(params: Record<string, string | undefined>) {
  const usp = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== "") {
      usp.set(key, value);
    }
  });
  const qs = usp.toString();
  return qs ? `?${qs}` : "";
}

// ASP.NET の InvoiceSearchRequest に合わせてクエリを投げる関数
async function fetchInvoices(searchParams: SearchParams) {
  const page = searchParams.page ? Number(searchParams.page) || 1 : 1;

  const invoices = await apiGetServer<InvoiceDto[]>("/api/invoices", {
    // 🔸 C# の InvoiceSearchRequest に合わせて PascalCase で投げる
    InvoiceNumber: searchParams.invoiceNumber,
    MemberName: searchParams.memberName,
    StatusId: searchParams.statusId,
    FromInvoiceDate: searchParams.fromInvoiceDate,
    ToInvoiceDate: searchParams.toInvoiceDate,
    Page: page,
    PageSize: PAGE_SIZE,
  });

  // API が totalCount を返していない前提で、
  // 「件数が PAGE_SIZE と同じなら次ページあり」とみなす簡易実装
  const hasNextPage = invoices.length === PAGE_SIZE;

    const qs = buildQueryString({
    invoiceNumber: searchParams.invoiceNumber,
    memberName: searchParams.memberName,
    statusId: searchParams.statusId,
    fromInvoiceDate: searchParams.fromInvoiceDate,
    toInvoiceDate: searchParams.toInvoiceDate,
    page: String(page),
  });

  return { invoices, page, hasNextPage, qs };
}

// ✅ Next.js 16 では searchParams が Promise なので、ここで await する
type PageProps = {
  searchParams: Promise<SearchParams>;
};

function formatCurrency(value: number): string {
  return value.toLocaleString("ja-JP", {
    style: "currency",
    currency: "JPY",
    maximumFractionDigits: 0,
  });
}

function statusBadgeClass(statusName?: string) {
  return [
    "inline-flex items-center rounded-full px-2.5 py-1 text-[10px] font-medium",
    statusName === "未入金"
      ? "bg-red-500/15 text-red-300 ring-1 ring-red-500/40"
      : statusName === "一部入金"
      ? "bg-amber-500/15 text-amber-200 ring-1 ring-amber-500/40"
      : "bg-emerald-500/15 text-emerald-200 ring-1 ring-emerald-500/40",
  ].join(" ");
}

// ★リンク先は仮（後で実装時に合わせて変更OK）
function invoiceDetailHref(inv: InvoiceDto) {
  // 例：/invoices/[id]
  return `/invoices/${inv.id}`;
}
function collectionHref(inv: InvoiceDto) {
  // 例：/collections/[invoiceId]（督促画面）
  return `/collections/${inv.id}`;
}

export default async function InvoicesPage(props: PageProps) {
  const searchParams = await props.searchParams;

const { invoices, page, hasNextPage, qs } = await fetchInvoices(searchParams);
const from = encodeURIComponent(qs.replace(/^\?/, "")); // 先頭?を外して詰める
  const hasPrevPage = page > 1;

  const currentFrom = invoices.length > 0 ? (page - 1) * PAGE_SIZE + 1 : 0;
  const currentTo =
    invoices.length > 0 ? (page - 1) * PAGE_SIZE + invoices.length : 0;

  return (
    <div className="min-h-screen bg-slate-950 text-slate-50">
      {/* 背景グラデーション */}
      <div className="pointer-events-none fixed inset-0 bg-[radial-gradient(circle_at_top,_#0ea5e9_0,_transparent_55%),radial-gradient(circle_at_bottom,_#6366f1_0,_transparent_55%)] opacity-60" />

      {/* ヘッダー */}
      <header className="relative z-10 border-b border-slate-800 bg-slate-950/80 backdrop-blur">
        <div className="mx-auto max-w-6xl px-4 sm:px-6 py-4">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div className="min-w-0">
              <div className="flex items-center gap-2 text-[11px] text-slate-400">
                <Link href="/dashboards/admin" className="hover:text-sky-300">
                  ダッシュボード
                </Link>
                <span>/</span>
                <span>請求書一覧</span>
              </div>
              <h1 className="mt-1 text-lg font-semibold text-slate-50">
                請求書一覧（管理者）
              </h1>
              <p className="mt-1 text-xs text-slate-400">
                請求番号・会員名・ステータス・請求日で検索し、入金状況や支払期限を一覧で確認できます。
              </p>
            </div>

            <div className="flex flex-wrap items-center gap-2">
              <Link
                href="/dashboards/admin"
                className="inline-flex items-center gap-1 rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-1.5 text-xs font-medium text-slate-200 hover:bg-slate-800 hover:text-sky-300 transition"
              >
                ← 管理トップへ
              </Link>
              <Link
  href="/invoices/new"
  className="inline-flex items-center justify-center rounded-lg bg-sky-600 px-3 py-1.5 text-xs font-medium text-slate-50 shadow-sm shadow-sky-500/30 hover:bg-sky-500"
>
  ＋ 新規作成
</Link>
            </div>
          </div>
        </div>
      </header>

      {/* メインコンテンツ */}
      <main className="relative z-10 mx-auto max-w-6xl px-4 sm:px-6 py-6 sm:py-8 space-y-6">
        {/* 🔍 検索フォーム（スマホ=1列〜2列 / md以上=今まで通り） */}
        <form
          className="grid gap-4 rounded-2xl border border-slate-800 bg-slate-900/80 p-4 shadow-md shadow-sky-900/20 sm:grid-cols-2 md:grid-cols-4 lg:grid-cols-6"
          method="get"
        >
{/* ✅ デモ用クイックフィルタ（保存済み検索） */}
<div className="flex flex-wrap items-center gap-2 rounded-2xl border border-slate-800 bg-slate-900/80 p-3 shadow-md shadow-sky-900/10">
  <span className="text-[11px] text-slate-400 mr-1">デモ：</span>

  <Link
    href="/invoices?invoiceNumber=FIX&fromInvoiceDate=2025-11-01&toInvoiceDate=2025-12-31"
    className="inline-flex items-center justify-center min-w-[120px] rounded-lg border border-slate-700 bg-slate-950/40 px-3 py-1.5 text-xs font-medium text-slate-100 hover:bg-slate-800 hover:text-sky-200 transition"
  >
    FIX（デモ見本）
  </Link>

  <Link
    href="/invoices?statusId=4"
    className="inline-flex items-center justify-center min-w-[120px] rounded-lg border border-slate-700 bg-slate-950/40 px-3 py-1.5 text-xs font-medium text-slate-100 hover:bg-slate-800 hover:text-sky-200 transition"
  >
    OVERDUE（期限超過）
  </Link>

  <Link
    href="/invoices"
    className="ml-auto inline-flex items-center justify-center min-w-[80px] rounded-lg border border-slate-600 bg-slate-900 px-3 py-1.5 text-xs font-medium text-slate-200 hover:bg-slate-800 whitespace-nowrap"
  >
    クリア
  </Link>
</div>

          {/* 請求番号 */}
          <div className="flex flex-col gap-1">
            <label className="text-[11px] font-medium text-slate-300">
              請求番号
            </label>
            <input
              type="text"
              name="invoiceNumber"
              defaultValue={searchParams.invoiceNumber ?? ""}
              className="rounded-lg border border-slate-700 bg-slate-950/60 px-3 py-2 text-xs text-slate-100 outline-none ring-0 placeholder:text-slate-500 focus:border-sky-500 focus:ring-1 focus:ring-sky-500"
              placeholder="例：INV-001"
            />
          </div>

          {/* 会員名 */}
          <div className="flex flex-col gap-1">
            <label className="text-[11px] font-medium text-slate-300">
              会員名
            </label>
            <input
              type="text"
              name="memberName"
              defaultValue={searchParams.memberName ?? ""}
              className="rounded-lg border border-slate-700 bg-slate-950/60 px-3 py-2 text-xs text-slate-100 outline-none ring-0 placeholder:text-slate-500 focus:border-sky-500 focus:ring-1 focus:ring-sky-500"
              placeholder="例：Test User"
            />
          </div>

          {/* ステータス */}
          <div className="flex flex-col gap-1">
            <label className="text-[11px] font-medium text-slate-300">
              ステータス
            </label>
            <select
              name="statusId"
              defaultValue={searchParams.statusId ?? ""}
              className="rounded-lg border border-slate-700 bg-slate-950/60 px-3 py-2 text-xs text-slate-100 outline-none ring-0 focus:border-sky-500 focus:ring-1 focus:ring-sky-500"
            >
              <option value="">すべて</option>
              <option value="1">未入金</option>
              <option value="2">一部入金</option>
              <option value="3">入金済み</option>
            </select>
          </div>

          {/* 請求日 From */}
          <div className="flex flex-col gap-1">
            <label className="text-[11px] font-medium text-slate-300">
              請求日（From）
            </label>
<input
  type="date"
  name="fromInvoiceDate"
  defaultValue={searchParams.fromInvoiceDate ?? ""}
  className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-xs text-slate-900 outline-none ring-0 focus:border-sky-500 focus:ring-2 focus:ring-sky-200"
/>
          </div>

          {/* 請求日 To */}
          <div className="flex flex-col gap-1">
            <label className="text-[11px] font-medium text-slate-300">
              請求日（To）
            </label>
<input
  type="date"
  name="toInvoiceDate"
  defaultValue={searchParams.toInvoiceDate ?? ""}
  className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-xs text-slate-900 outline-none ring-0 focus:border-sky-500 focus:ring-2 focus:ring-sky-200"
/>
          </div>

          {/* ボタンエリア：スマホでは2列幅で横並び */}
          <div className="flex items-end gap-2 sm:col-span-2 md:col-span-2 lg:col-span-1">
            <button
              type="submit"
              className="inline-flex w-full items-center justify-center rounded-lg bg-sky-600 px-4 py-2 text-xs font-medium text-slate-50 shadow-sm shadow-sky-500/30 transition hover:bg-sky-500 focus:outline-none focus:ring-2 focus:ring-sky-400 focus:ring-offset-1 focus:ring-offset-slate-950"
            >
              検索
            </button>
            <Link
              href="/invoices"
              className="inline-flex w-full items-center justify-center rounded-lg border border-slate-600 bg-slate-900 px-4 py-2 text-xs font-medium text-slate-200 hover:bg-slate-800 whitespace-nowrap"
            >
              リセット
            </Link>
          </div>
        </form>

        {/* ====== スマホ：カード表示 ====== */}
        <section className="md:hidden space-y-3">
          {invoices.length === 0 ? (
            <div className="rounded-2xl border border-slate-800 bg-slate-900/80 px-4 py-8 text-center text-xs text-slate-400">
              該当する請求書はありません。
            </div>
          ) : (
            invoices.map((inv) => (
              <div
                key={inv.id}
                className="rounded-2xl border border-slate-800 bg-slate-900/80 p-4 shadow-md"
              >
                <div className="flex items-start justify-between gap-3">
                  <div className="min-w-0">
                    <p className="text-[11px] text-slate-400">
                      請求日：{inv.invoiceDate?.slice(0, 10) ?? "-"}
                    </p>

<Link
  href={`${invoiceDetailHref(inv)}?from=${from}`}
  className="mt-1 block text-sm font-semibold text-sky-300 hover:text-sky-200 break-all"
>
  {inv.invoiceNumber}
</Link>

                    <p className="mt-1 text-xs text-slate-200 truncate">
                      {inv.memberName}
                    </p>
                  </div>

                  <span className={statusBadgeClass(inv.statusName)}>
                    {inv.statusName}
                  </span>
                </div>

                <dl className="mt-3 grid grid-cols-2 gap-3 text-xs">
                  <div>
                    <dt className="text-[11px] text-slate-400">請求金額</dt>
                    <dd className="mt-1 font-semibold text-slate-50 break-words">
                      {formatCurrency(inv.totalAmount)}
                    </dd>
                  </div>
                  <div>
                    <dt className="text-[11px] text-slate-400">支払期限</dt>
                    <dd className="mt-1 text-slate-200">
                      {inv.dueDate?.slice(0, 10) ?? "-"}
                    </dd>
                  </div>
                </dl>

                {/* 操作ボタン */}
                <div className="mt-4 grid grid-cols-2 gap-2">
<Link
  href={`${invoiceDetailHref(inv)}?from=${from}`}
  className="inline-flex items-center justify-center rounded-lg border border-slate-700 bg-slate-950/40 px-3 py-2 text-xs font-medium text-slate-100 hover:bg-slate-800 hover:text-sky-200 transition"
>
  請求書詳細
</Link>
                  <Link
                    href={collectionHref(inv)}
                    className="inline-flex items-center justify-center rounded-lg border border-amber-500/40 bg-amber-500/10 px-3 py-2 text-xs font-medium text-amber-200 hover:bg-amber-500/15 transition"
                  >
                    督促
                  </Link>
                </div>
              </div>
            ))
          )}
        </section>

        {/* ====== md以上：テーブル表示（操作列追加） ====== */}
        <div className="hidden md:block overflow-x-auto">
          <div className="min-w-[980px] overflow-hidden rounded-2xl border border-slate-800 bg-slate-900/80 shadow-md">
            <table className="min-w-full text-xs">
              <thead className="bg-slate-900/90 text-[11px] font-medium uppercase tracking-wide text-slate-400">
                <tr>
                  <th className="px-4 py-3 text-left">請求日</th>
                  <th className="px-4 py-3 text-left">請求番号</th>
                  <th className="px-4 py-3 text-left">会員名</th>
                  <th className="px-4 py-3 text-right">請求金額</th>
                  <th className="px-4 py-3 text-left">支払期限</th>
                  <th className="px-4 py-3 text-left">ステータス</th>
                  <th className="px-4 py-3 text-right">操作</th>
                </tr>
              </thead>

              <tbody className="divide-y divide-slate-800/80">
                {invoices.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="px-4 py-6 text-center text-xs text-slate-400">
                      該当する請求書はありません。
                    </td>
                  </tr>
                ) : (
                  invoices.map((inv) => (
                    <tr key={inv.id} className="bg-slate-900/60 hover:bg-slate-800/80">
                      <td className="px-4 py-3 whitespace-nowrap align-middle text-slate-200">
                        {inv.invoiceDate?.slice(0, 10) ?? "-"}
                      </td>

                      <td className="px-4 py-3 align-middle font-semibold text-sky-300">
<Link href={`${invoiceDetailHref(inv)}?from=${from}`} className="hover:text-sky-200">
  {inv.invoiceNumber}
</Link>
                      </td>

                      <td className="px-4 py-3 align-middle text-slate-200">
                        {inv.memberName}
                      </td>

                      <td className="px-4 py-3 align-middle text-right tabular-nums text-slate-50">
                        {formatCurrency(inv.totalAmount)}
                      </td>

                      <td className="px-4 py-3 whitespace-nowrap align-middle text-slate-200">
                        {inv.dueDate?.slice(0, 10) ?? "-"}
                      </td>

                      <td className="px-4 py-3 align-middle">
                        <span className={statusBadgeClass(inv.statusName)}>{inv.statusName}</span>
                      </td>

                      {/* 操作 */}
                      <td className="px-4 py-3 align-middle">
                        <div className="flex justify-end gap-2">
 <Link
  href={`${invoiceDetailHref(inv)}?from=${from}`}
  className="inline-flex items-center justify-center rounded-lg border border-slate-700 bg-slate-950/40 px-3 py-1.5 text-xs font-medium text-slate-100 hover:bg-slate-800 hover:text-sky-200 transition"
>
  詳細
</Link>
                          <Link
                            href={collectionHref(inv)}
                            className="inline-flex items-center justify-center rounded-lg border border-amber-500/40 bg-amber-500/10 px-3 py-1.5 text-xs font-medium text-amber-200 hover:bg-amber-500/15 transition"
                          >
                            督促
                          </Link>
                        </div>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>

        {/* ◀ ページネーション ▶（スマホは縦並び） */}
        <div className="flex flex-col gap-3 text-xs text-slate-300 sm:flex-row sm:items-center sm:justify-between">
          <p>
            {currentFrom === 0
              ? "0件"
              : `${currentFrom}–${currentTo}件を表示（1ページあたり ${PAGE_SIZE}件）`}
          </p>

          <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:gap-2">
            {/* 前へ */}
            <Link
              aria-disabled={!hasPrevPage}
              href={
                hasPrevPage
                  ? (() => {
                      const qs = buildQueryString({
                        invoiceNumber: searchParams.invoiceNumber,
                        memberName: searchParams.memberName,
                        statusId: searchParams.statusId,
                        fromInvoiceDate: searchParams.fromInvoiceDate,
                        toInvoiceDate: searchParams.toInvoiceDate,
                        page: String(page - 1),
                      });
                      return `/invoices${qs}`;
                    })()
                  : "#"
              }
              className={[
                "inline-flex w-full sm:w-auto justify-center items-center gap-1 rounded-lg px-3 py-2 sm:py-1.5 text-xs font-medium",
                hasPrevPage
                  ? "border border-slate-600 bg-slate-900 text-slate-100 hover:bg-slate-800"
                  : "border border-slate-800 bg-slate-900/60 text-slate-500 cursor-not-allowed",
              ].join(" ")}
            >
              <span aria-hidden>←</span>
              <span>前へ</span>
            </Link>

            {/* 次へ */}
            <Link
              aria-disabled={!hasNextPage}
              href={
                hasNextPage
                  ? (() => {
                      const qs = buildQueryString({
                        invoiceNumber: searchParams.invoiceNumber,
                        memberName: searchParams.memberName,
                        statusId: searchParams.statusId,
                        fromInvoiceDate: searchParams.fromInvoiceDate,
                        toInvoiceDate: searchParams.toInvoiceDate,
                        page: String(page + 1),
                      });
                      return `/invoices${qs}`;
                    })()
                  : "#"
              }
              className={[
                "inline-flex w-full sm:w-auto justify-center items-center gap-1 rounded-lg px-3 py-2 sm:py-1.5 text-xs font-medium",
                hasNextPage
                  ? "border border-slate-600 bg-slate-900 text-slate-100 hover:bg-slate-800"
                  : "border border-slate-800 bg-slate-900/60 text-slate-500 cursor-not-allowed",
              ].join(" ")}
            >
              <span>次へ</span>
              <span aria-hidden>→</span>
            </Link>
          </div>
        </div>
      </main>
    </div>
  );
}
