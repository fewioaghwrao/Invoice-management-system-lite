// src/app/payments/page.tsx
import Link from "next/link";
import CurrentUserBadge from "@/components/CurrentUserBadge";
import LogoutButton from "@/components/LogoutButton";

type PaymentStatus = "UNALLOCATED" | "PARTIAL" | "ALLOCATED";

type PaymentRow = {
  id: string;              // 表示用: PAY-123
  paymentId: number;       // DB ID: 123
  paidAt: string;          // "2025-07-01"
  payerName: string;
  amount: number;
  allocatedAmount: number;
  invoiceIds: string[];    // InvoiceNumber の配列（INV-001等）
  status: PaymentStatus;
};

type PaymentListResult = {
  year: number;
  month: number | "all";
  keyword: string;
  status: PaymentStatus | "all";
  page: number;
  pageSize: number;
  totalCount: number;
  rows: PaymentRow[];
  summary: {
    totalAmount: number;
    allocatedTotal: number;
    unallocatedTotal: number;
  };
};

// ---- API DTO（C#の PaymentListResultDto に合わせる）----
type PaymentListItemDto = {
  id: number;
  paymentDate: string; // ISO
  payerName: string | null;
  amount: number;
  allocatedAmount: number;
  invoiceIds: string[]; // InvoiceNumber
  status: string; // "UNALLOCATED" | "PARTIAL" | "ALLOCATED"
};

type PaymentListResultDto = {
  year: number;
  month: string; // "all" or "1".."12"
  keyword: string;
  status: string; // "all" | "UNALLOCATED" | "PARTIAL" | "ALLOCATED"
  page: number;
  pageSize: number;
  totalCount: number;
  rows: PaymentListItemDto[];
  summary: {
    totalAmount: number;
    allocatedTotal: number;
    unallocatedTotal: number;
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

function statusLabel(s: PaymentStatus) {
  switch (s) {
    case "ALLOCATED":
      return "割当済";
    case "PARTIAL":
      return "一部割当";
    case "UNALLOCATED":
      return "未割当";
  }
}

function statusPillClass(s: PaymentStatus) {
  switch (s) {
    case "ALLOCATED":
      return "bg-emerald-500/10 text-emerald-300 border-emerald-500/30";
    case "PARTIAL":
      return "bg-sky-500/10 text-sky-300 border-sky-500/30";
    case "UNALLOCATED":
      return "bg-amber-500/10 text-amber-300 border-amber-500/30";
  }
}

function normalizeStatus(input: string): PaymentStatus | "all" {
  const s = (input ?? "all").trim().toUpperCase();
  if (s === "UNALLOCATED" || s === "PARTIAL" || s === "ALLOCATED") return s;
  return "all";
}

function toDisplayPaymentId(id: number) {
  // PAY-000123 みたいにしたいなら padStart を増やしてOK
  return `PAY-${String(id).padStart(3, "0")}`;
}

function buildHref(base: {
  year: number;
  month: number | "all";
  status: PaymentStatus | "all";
  keyword: string;
  page: number;
}) {
  const sp = new URLSearchParams();
  sp.set("year", String(base.year));
  sp.set("month", String(base.month));
  sp.set("status", String(base.status).toLowerCase());
  if (base.keyword.trim()) sp.set("q", base.keyword.trim());
  sp.set("page", String(base.page));
  return `/payments?${sp.toString()}`;
}

/**
 * ✅ DB反映：API fetch
 * - Next.js Server Component なので fetch 直でOK
 * - ルートが別ドメインなら NEXT_PUBLIC_API_BASE_URL を使う
 */
async function getPayments(params: {
  year: number;
  month: number | "all";
  keyword: string;
  status: PaymentStatus | "all";
  page: number;
  pageSize: number;
}): Promise<PaymentListResult> {
  const apiBase = process.env.NEXT_PUBLIC_API_BASE_URL ?? ""; // 例: "https://localhost:5001"
  const sp = new URLSearchParams();

  sp.set("year", String(params.year));
  if (params.month !== "all") sp.set("month", String(params.month));
  if (params.keyword.trim()) sp.set("q", params.keyword.trim());

  // C#側は "all" or "UNALLOCATED"... を想定してるので大文字に
  if (params.status !== "all") sp.set("status", params.status);
  else sp.set("status", "all");

  sp.set("page", String(params.page));
  sp.set("pageSize", String(params.pageSize));

  const url = `${apiBase}/api/payments?${sp.toString()}`;

  const res = await fetch(url, {
    method: "GET",
    // 開発中はキャッシュでハマるので no-store 推奨
    cache: "no-store",
  });

  if (!res.ok) {
    const text = await res.text().catch(() => "");
    throw new Error(`Failed to fetch payments: ${res.status} ${res.statusText}\n${text}`);
  }

  const dto = (await res.json()) as PaymentListResultDto;

  const rows: PaymentRow[] = dto.rows.map((p) => {
    const st = normalizeStatus(p.status);
    const status: PaymentStatus = st === "all" ? "UNALLOCATED" : st; // dtoはallにならない想定

    return {
      id: toDisplayPaymentId(p.id),
      paymentId: p.id,
      paidAt: p.paymentDate,
      payerName: p.payerName ?? "",
      amount: p.amount,
      allocatedAmount: p.allocatedAmount,
      invoiceIds: p.invoiceIds ?? [],
      status,
    };
  });

  const month =
    dto.month === "all" ? "all" : (Number(dto.month) || "all");

  const status = normalizeStatus(dto.status);

  return {
    year: dto.year,
    month,
    keyword: dto.keyword ?? "",
    status,
    page: dto.page,
    pageSize: dto.pageSize,
    totalCount: dto.totalCount,
    rows,
    summary: {
      totalAmount: dto.summary?.totalAmount ?? 0,
      allocatedTotal: dto.summary?.allocatedTotal ?? 0,
      unallocatedTotal: dto.summary?.unallocatedTotal ?? 0,
    },
  };
}

export default async function PaymentsPage({
  searchParams,
}: {
  searchParams: Promise<{ [key: string]: string | string[] | undefined }>;
}) {
  const params = await searchParams;

  const now = new Date();
  const currentYear = now.getFullYear();

  const year = Number(params.year ?? currentYear) || currentYear;

  const monthParam = params.month ?? "all";
  const month = monthParam === "all" ? "all" : (Number(monthParam) || "all");

  const keyword = String(params.q ?? "");

  const statusParam = String(params.status ?? "all").toUpperCase();
  const status =
    statusParam === "ALLOCATED" || statusParam === "PARTIAL" || statusParam === "UNALLOCATED"
      ? (statusParam as PaymentStatus)
      : "all";

  const page = Math.max(1, Number(params.page ?? 1) || 1);
  const pageSize = 10;

  const result = await getPayments({ year, month, keyword, status, page, pageSize });
  const totalPages = Math.max(1, Math.ceil(result.totalCount / result.pageSize));

  return (
    <div className="min-h-screen bg-slate-950 text-slate-50">
      <div className="pointer-events-none fixed inset-0 bg-[radial-gradient(circle_at_top,_#10b981_0,_transparent_55%),radial-gradient(circle_at_bottom,_#6366f1_0,_transparent_55%)] opacity-60" />

      <header className="relative z-10 border-b border-slate-800 bg-slate-950/80 backdrop-blur">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
          <div>
            <h1 className="text-lg font-semibold">入金一覧</h1>
            <p className="mt-1 text-xs text-slate-400">
              入金明細を一覧で確認し、請求書への割当状況（未割当/一部/完了）を把握できます。
            </p>
          </div>

          <div className="flex items-center gap-3">
            <Link href="/dashboards/admin" className="text-xs text-slate-300 hover:text-slate-100">
              ← 管理者トップへ
            </Link>

            <Link
              href="/payments/new"
              className="inline-flex h-9 shrink-0 items-center justify-center whitespace-nowrap rounded-lg bg-emerald-600 px-4 text-xs font-medium leading-none text-slate-50 hover:bg-emerald-500"
            >
              + 入金登録
            </Link>

            <CurrentUserBadge />
            <LogoutButton />
          </div>
        </div>
      </header>

      <main className="relative z-10 mx-auto max-w-6xl px-6 py-8 space-y-6">
        {/* 検索・絞り込み（GET） */}
        <section className="rounded-2xl border border-slate-800 bg-slate-900/80 p-5 shadow-md">
          <form className="flex flex-wrap items-end gap-3" action="/payments">
            <div className="flex flex-col gap-1">
              <label className="text-[11px] text-slate-400">年</label>
              <select
                name="year"
                defaultValue={String(result.year)}
                className="h-9 rounded-lg border border-slate-700 bg-slate-950/40 px-3 text-xs text-slate-100"
              >
                {[currentYear - 1, currentYear, currentYear + 1].map((y) => (
                  <option key={y} value={String(y)}>{y}年</option>
                ))}
              </select>
            </div>

            <div className="flex flex-col gap-1">
              <label className="text-[11px] text-slate-400">月</label>
              <select
                name="month"
                defaultValue={String(result.month)}
                className="h-9 rounded-lg border border-slate-700 bg-slate-950/40 px-3 text-xs text-slate-100"
              >
                <option value="all">すべて</option>
                {Array.from({ length: 12 }, (_, i) => i + 1).map((m) => (
                  <option key={m} value={String(m)}>{m}月</option>
                ))}
              </select>
            </div>

            <div className="flex flex-col gap-1">
              <label className="text-[11px] text-slate-400">割当ステータス</label>
              <select
                name="status"
                defaultValue={String(result.status).toLowerCase()}
                className="h-9 rounded-lg border border-slate-700 bg-slate-950/40 px-3 text-xs text-slate-100"
              >
                <option value="all">すべて</option>
                <option value="unallocated">未割当</option>
                <option value="partial">一部割当</option>
                <option value="allocated">割当済</option>
              </select>
            </div>

            <div className="flex flex-col gap-1">
              <label className="text-[11px] text-slate-400">検索（入金ID / 名義 / 請求書番号）</label>
              <input
                name="q"
                defaultValue={result.keyword}
                placeholder="例: 123 / クライアントC / INV-004"
                className="h-9 w-80 max-w-[70vw] rounded-lg border border-slate-700 bg-slate-950/40 px-3 text-xs text-slate-100 placeholder:text-slate-500"
              />
            </div>

            <input type="hidden" name="page" value="1" />

            <button className="h-9 rounded-lg border border-slate-600 bg-slate-900 px-4 text-xs font-medium text-slate-200 hover:bg-slate-800">
              検索
            </button>

            <Link
              href="/payments"
              className="inline-flex h-9 items-center justify-center rounded-lg border border-slate-600 bg-slate-900 px-4 text-xs font-medium text-slate-200 hover:bg-slate-800"
            >
              リセット
            </Link>
          </form>

          {/* サマリー */}
          <div className="mt-4 grid gap-3 md:grid-cols-3">
            <div className="rounded-xl border border-slate-800 bg-slate-950/30 p-4">
              <p className="text-[11px] text-slate-400">入金合計</p>
              <p className="mt-2 text-xl font-semibold text-emerald-300">
                {formatCurrency(result.summary.totalAmount)}
              </p>
            </div>
            <div className="rounded-xl border border-slate-800 bg-slate-950/30 p-4">
              <p className="text-[11px] text-slate-400">割当済み合計</p>
              <p className="mt-2 text-xl font-semibold">
                {formatCurrency(result.summary.allocatedTotal)}
              </p>
            </div>
            <div className="rounded-xl border border-slate-800 bg-slate-950/30 p-4">
              <p className="text-[11px] text-slate-400">未割当合計</p>
              <p className="mt-2 text-xl font-semibold text-amber-300">
                {formatCurrency(result.summary.unallocatedTotal)}
              </p>
            </div>
          </div>
        </section>

        {/* 一覧 */}
        <section className="rounded-2xl border border-slate-800 bg-slate-900/80 p-5 shadow-md">
          <div className="mb-3 flex items-center justify-between">
            <h2 className="text-sm font-semibold text-slate-100">入金一覧（{result.totalCount}件）</h2>
            <p className="text-[11px] text-slate-400">入金詳細→割当へ遷移できます</p>
          </div>

          {/* PC/タブレット：テーブル */}
          <div className="hidden md:block overflow-hidden rounded-xl border border-slate-800 bg-slate-950/40">
            <table className="min-w-full text-xs">
              <thead className="bg-slate-900/90">
                <tr className="text-left text-[11px] text-slate-400">
                  <th className="px-4 py-2 font-medium">入金ID</th>
                  <th className="px-4 py-2 font-medium">入金日</th>
                  <th className="px-4 py-2 font-medium">入金名義</th>
                  <th className="px-4 py-2 font-medium">割当状態</th>
                  <th className="px-4 py-2 font-medium text-right">入金額</th>
                  <th className="px-4 py-2 font-medium text-right">割当済</th>
                  <th className="px-4 py-2 font-medium text-right">未割当</th>
                  <th className="px-4 py-2 font-medium">請求書</th>
                  <th className="px-4 py-2 font-medium text-right">操作</th>
                </tr>
              </thead>
              <tbody>
                {result.rows.map((p) => {
                  const unallocated = Math.max(0, p.amount - p.allocatedAmount);
                  return (
                    <tr key={p.paymentId} className="border-t border-slate-800/80 hover:bg-slate-800/60">
                      <td className="px-4 py-2 text-slate-100">{p.id}</td>
                      <td className="px-4 py-2 text-slate-200">{formatDate(p.paidAt)}</td>
                      <td className="px-4 py-2 text-slate-200">{p.payerName}</td>
                      <td className="px-4 py-2">
                        <span className={`inline-flex items-center rounded-full border px-2 py-0.5 text-[11px] ${statusPillClass(p.status)}`}>
                          {statusLabel(p.status)}
                        </span>
                      </td>
                      <td className="px-4 py-2 text-right text-emerald-200">{formatCurrency(p.amount)}</td>
                      <td className="px-4 py-2 text-right">{formatCurrency(p.allocatedAmount)}</td>
                      <td className="px-4 py-2 text-right text-amber-200">{formatCurrency(unallocated)}</td>
                      <td className="px-4 py-2">
                        {p.invoiceIds.length === 0 ? (
                          <span className="text-[11px] text-slate-500">—</span>
                        ) : (
                          p.invoiceIds.map((invNo) => (
                            <Link
                              key={invNo}
                              href={`/invoices/${encodeURIComponent(invNo)}`}
                              className="mr-2 text-sky-300 hover:text-sky-200"
                            >
                              {invNo}
                            </Link>
                          ))
                        )}
                      </td>
                      <td className="px-4 py-2 text-right">
                        <Link
                          href={`/payments/${p.paymentId}`}
                          className="inline-flex h-8 items-center justify-center rounded-lg border border-slate-600 bg-slate-900 px-3 text-[11px] font-medium text-slate-200 hover:bg-slate-800"
                        >
                          詳細
                        </Link>
                      </td>
                    </tr>
                  );
                })}

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

          {/* スマホ：カード */}
          <div className="md:hidden space-y-3">
            {result.rows.map((p) => {
              const unallocated = Math.max(0, p.amount - p.allocatedAmount);
              return (
                <div key={p.paymentId} className="rounded-2xl border border-slate-800 bg-slate-950/30 p-4">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <p className="text-sm font-semibold text-slate-100">{p.id}</p>
                      <p className="mt-1 text-[11px] text-slate-400">
                        {formatDate(p.paidAt)} / {p.payerName}
                      </p>
                    </div>
                    <span className={`shrink-0 inline-flex items-center rounded-full border px-2 py-0.5 text-[11px] ${statusPillClass(p.status)}`}>
                      {statusLabel(p.status)}
                    </span>
                  </div>

                  <div className="mt-3 grid grid-cols-2 gap-2 text-[11px]">
                    <div className="rounded-xl border border-slate-800 bg-slate-950/40 p-2">
                      <p className="text-slate-400">入金額</p>
                      <p className="mt-1 text-xs font-semibold text-emerald-200">{formatCurrency(p.amount)}</p>
                    </div>
                    <div className="rounded-xl border border-slate-800 bg-slate-950/40 p-2">
                      <p className="text-slate-400">割当済</p>
                      <p className="mt-1 text-xs font-semibold">{formatCurrency(p.allocatedAmount)}</p>
                    </div>
                    <div className="rounded-xl border border-slate-800 bg-slate-950/40 p-2 col-span-2">
                      <p className="text-slate-400">未割当</p>
                      <p className="mt-1 text-xs font-semibold text-amber-200">{formatCurrency(unallocated)}</p>
                    </div>
                  </div>

                  <div className="mt-3">
                    <p className="text-[11px] text-slate-400">請求書</p>
                    <div className="mt-1 flex flex-wrap gap-2">
                      {p.invoiceIds.length === 0 ? (
                        <span className="text-[11px] text-slate-500">—</span>
                      ) : (
                        p.invoiceIds.map((invNo) => (
                          <Link
                            key={invNo}
                            href={`/invoices/${encodeURIComponent(invNo)}`}
                            className="rounded-full border border-slate-700 bg-slate-900 px-2 py-1 text-[11px] text-sky-300"
                          >
                            {invNo}
                          </Link>
                        ))
                      )}
                    </div>
                  </div>

                  <div className="mt-4 flex justify-end">
                    <Link
                      href={`/payments/${p.paymentId}`}
                      className="inline-flex h-9 items-center justify-center rounded-lg border border-slate-600 bg-slate-900 px-4 text-xs font-medium text-slate-200 hover:bg-slate-800"
                    >
                      詳細へ
                    </Link>
                  </div>
                </div>
              );
            })}

            {result.rows.length === 0 && (
              <div className="rounded-2xl border border-slate-800 bg-slate-950/30 p-6 text-center text-[11px] text-slate-400">
                条件に一致するデータがありません。
              </div>
            )}
          </div>

          {/* ページネーション */}
          <div className="mt-4 flex items-center justify-between">
            <p className="text-[11px] text-slate-400">
              {result.page} / {totalPages} ページ
            </p>
            <div className="flex gap-2">
              <Link
                aria-disabled={result.page <= 1}
                href={buildHref({
                  year: result.year,
                  month: result.month,
                  status: result.status,
                  keyword: result.keyword,
                  page: Math.max(1, result.page - 1),
                })}
                className={`rounded-lg border px-3 py-1 text-xs ${
                  result.page <= 1
                    ? "border-slate-800 bg-slate-900/30 text-slate-500 pointer-events-none"
                    : "border-slate-700 bg-slate-900 text-slate-200 hover:bg-slate-800"
                }`}
              >
                ← 前へ
              </Link>

              <Link
                aria-disabled={result.page >= totalPages}
                href={buildHref({
                  year: result.year,
                  month: result.month,
                  status: result.status,
                  keyword: result.keyword,
                  page: Math.min(totalPages, result.page + 1),
                })}
                className={`rounded-lg border px-3 py-1 text-xs ${
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
