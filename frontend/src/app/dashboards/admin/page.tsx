// src/app/dashboards/admin/page.tsx
import Link from "next/link";
import CurrentUserBadge from "@/components/CurrentUserBadge";
import LogoutButton from "@/components/LogoutButton";
import MonthlySalesChartClient from "@/components/MonthlySalesChartClient";
import { apiGetServer } from "@/lib/api.server";

type UnpaidInvoice = {
  invoiceId: number;
  invoiceNumber: string;
  clientName: string;
  amount: number;
  isOverdue: boolean;
};

type MonthlySale = {
  label: string; // "1月" など
  amount: number;
};

type AdminSummary = {
  year: number;
  availableYears: number[];
  totalSales: number;
  unpaidAmount: number;
  invoiceCount: number;
  paymentCount: number;
  unpaidInvoices: UnpaidInvoice[];
  monthlySales: MonthlySale[];
  recoveryRate: number; // 0..100
};

// --- API DTO（バックエンドの返却に合わせる） ---
type ApiMonthlySalesDto = {
  month: number; // 1..12
  invoiceTotal: number;
};

type ApiUnpaidInvoiceDto = {
  invoiceId: number;
  invoiceNumber: string;
  clientName: string;
  dueDate: string; // ISO
  invoiceTotal: number;
  paidTotal: number;
  remainingTotal: number;
  isOverdue: boolean;
};

type ApiAdminSummaryDto = {
  year: number;
  invoiceTotal: number;
  paidTotal: number;
  remainingTotal: number;
  recoveryRate: number; // 0..100
  invoiceCount: number;
  paymentCount: number;
  monthlySales: ApiMonthlySalesDto[];
  unpaidTop5: ApiUnpaidInvoiceDto[];
};

type ApiAdminOperationLogDto = {
  id: number;
  at: string;          // ISO
  actorUserId: number;
  action: string;
  entity: string;
  entityId?: string | null;
  summary: string;
};

type WorstCustomer = {
  memberId: number;
  memberName: string;
  invoiceTotal: number;
  paidTotal: number;
  remainingTotal: number;
  recoveryRate: number;
};

type WorstTop5Result = {
  year: number;
  month: string; // "all" or "1".."12"
  keyword: string;
  count: number;
  rows: WorstCustomer[];
};

// ★API→画面用変換
function mapAdminSummary(dto: ApiAdminSummaryDto): AdminSummary {
  const year = dto.year;

  const availableYears = [year - 2, year - 1, year, year + 1, year + 2];

  const monthlySales: MonthlySale[] = (dto.monthlySales ?? []).map((m) => ({
    label: `${m.month}月`,
    amount: Number(m.invoiceTotal ?? 0),
  }));

const unpaidInvoices: UnpaidInvoice[] = (dto.unpaidTop5 ?? []).map((x) => ({
  invoiceId: x.invoiceId,
  invoiceNumber: x.invoiceNumber,
  clientName: x.clientName,
  amount: Number(x.remainingTotal ?? 0),
  isOverdue: Boolean(x.isOverdue),
}));

  return {
    year,
    availableYears,
    totalSales: Number(dto.invoiceTotal ?? 0),
    unpaidAmount: Number(dto.remainingTotal ?? 0),
    recoveryRate: Number(dto.recoveryRate ?? 0),
    invoiceCount: Number(dto.invoiceCount ?? 0),
    paymentCount: Number(dto.paymentCount ?? 0),
    unpaidInvoices,
    monthlySales,
  };
}

async function getRecentOperationLogs(limit = 5): Promise<ApiAdminOperationLogDto[]> {
  return apiGetServer<ApiAdminOperationLogDto[]>(
    `/api/admin/operation-logs/recent?limit=${limit}`
  );
}

// 画面表示用（任意：Actionコードを日本語に寄せる）
function formatActionLabel(code: string): string {
  const x = (code ?? "").toUpperCase();
  return x === "PAYMENT_ALLOCATION_ADDED" ? "割当追加"
    : x === "PAYMENT_ALLOCATION_DELETED" ? "割当削除"
    : x === "PAYMENT_ALLOCATIONS_REPLACED" ? "割当保存（置換）"
    : x === "PAYMENT_ALLOCATIONS_CLEARED" ? "割当クリア"
    : code;
}

function formatTarget(entity: string, entityId?: string | null): string {
  const e = (entity ?? "").toUpperCase();
  const label = e === "PAYMENT" ? "入金"
    : e === "INVOICE" ? "請求書"
    : e === "MEMBER" ? "会員"
    : entity;
  return entityId ? `${label} #${entityId}` : label;
}

// ★ year を引数で受け取る（実API）
async function getAdminSummary(year: number): Promise<AdminSummary> {
  const dto = await apiGetServer<ApiAdminSummaryDto>(`/api/admin/summary?year=${year}`);
  return mapAdminSummary(dto);
}

async function getWorstTop5(year: number): Promise<WorstTop5Result> {
  return apiGetServer(`/api/sales/by-member/worst-top5?year=${year}`);
}

function formatCurrency(value: number): string {
  return value.toLocaleString("ja-JP", {
    style: "currency",
    currency: "JPY",
    maximumFractionDigits: 0,
  });
}

export default async function AdminDashboardPage({
  searchParams,
}: {
  searchParams: Promise<{ [key: string]: string | string[] | undefined }>;
}) {
  const params = await searchParams;

  const now = new Date();
  const currentYear = now.getFullYear();

  const yearParam = params.year;
  const selectedYear = yearParam ? Number(yearParam) || currentYear : currentYear;

  const summary = await getAdminSummary(selectedYear);
  const worstTop5 = await getWorstTop5(selectedYear);

  const recentLogs = await getRecentOperationLogs(5);


  const maxMonthly = Math.max(...summary.monthlySales.map((m) => m.amount || 0), 1);

  return (
    <div className="min-h-screen bg-slate-950 text-slate-50">
      <div className="pointer-events-none fixed inset-0 bg-[radial-gradient(circle_at_top,_#0ea5e9_0,_transparent_55%),radial-gradient(circle_at_bottom,_#6366f1_0,_transparent_55%)] opacity-60" />

      <header className="relative z-10 border-b border-slate-800 bg-slate-950/80 backdrop-blur">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
<div className="min-w-0">
  <h1 className="text-lg font-semibold text-slate-50 truncate">
    請求・入金ステータスダッシュボード（管理者）
  </h1>
  <p className="mt-1 text-xs text-slate-400 line-clamp-2 sm:line-clamp-none">
    売上・未入金・請求書／入金の状況を一目で確認できる管理コンソールです。
  </p>
</div>

{/* 右側：スマホは縦、sm以上は横 */}
<div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:gap-3">
  {/* 年度切替：スマホは左右ボタン + 中央年 */}
  <div className="flex items-center justify-between rounded-xl border border-slate-700 bg-slate-900/60 px-3 py-2 sm:justify-start sm:gap-2 sm:px-0 sm:py-0 sm:border-0 sm:bg-transparent">
    <Link
      href={`/dashboards/admin?year=${selectedYear - 1}`}
      className="text-xs text-slate-300 hover:text-sky-300"
      aria-label="前年へ"
    >
      ← {selectedYear - 1}
    </Link>

    <span className="mx-2 inline-flex items-center rounded-full border border-slate-700 bg-slate-900/60 px-3 py-1 text-xs font-medium text-slate-100 sm:mx-0">
      {selectedYear} 年
    </span>

    <Link
      href={`/dashboards/admin?year=${selectedYear + 1}`}
      className="text-xs text-slate-300 hover:text-sky-300"
      aria-label="翌年へ"
    >
      {selectedYear + 1} →
    </Link>
  </div>

  {/* バッジ類：スマホは折り返し */}
  <div className="flex flex-wrap items-center gap-2 sm:flex-nowrap sm:gap-3">
    <CurrentUserBadge />
    <LogoutButton />
  </div>
</div>
        </div>
      </header>

      <main className="relative z-10 mx-auto max-w-6xl px-6 py-8 space-y-8">
        <section className="grid gap-4 md:grid-cols-5">
          <div className="rounded-2xl border border-sky-500/40 bg-slate-900/80 p-4 shadow-lg shadow-sky-500/20">
            <p className="text-[11px] font-medium text-slate-300">
              売上合計（請求金額）
            </p>
            <p className="mt-3 text-2xl font-semibold text-slate-50">
              {formatCurrency(summary.totalSales)}
            </p>
            <p className="mt-2 text-[11px] text-slate-400">
              発行済み請求書の合計金額（売掛）の総額です。
            </p>
          </div>

          <div className="rounded-2xl border border-amber-400/40 bg-slate-900/80 p-4 shadow-md shadow-amber-500/15">
            <p className="text-[11px] font-medium text-slate-300">未入金額</p>
            <p className="mt-3 text-2xl font-semibold text-amber-300">
              {formatCurrency(summary.unpaidAmount)}
            </p>
            <p className="mt-2 text-[11px] text-slate-400">
              期日超過や未入金の請求書の合計金額です。
            </p>
          </div>

          <div className="rounded-2xl border border-slate-700 bg-slate-900/80 p-4 shadow-md">
            <p className="text-[11px] font-medium text-slate-300">請求書数</p>
            <p className="mt-3 text-2xl font-semibold text-slate-50">
              {summary.invoiceCount}
              <span className="ml-1 text-xs text-slate-400">件</span>
            </p>
            <p className="mt-2 text-[11px] text-slate-400">
              集計対象期間内に発行された請求書の件数です。
            </p>
          </div>

          <div className="rounded-2xl border border-emerald-500/40 bg-slate-900/80 p-4 shadow-md shadow-emerald-500/20">
            <p className="text-[11px] font-medium text-slate-300">入金数</p>
            <p className="mt-3 text-2xl font-semibold text-emerald-300">
              {summary.paymentCount}
              <span className="ml-1 text-xs text-slate-400">件</span>
            </p>
            <p className="mt-2 text-[11px] text-slate-400">
              入金として登録された明細の件数です。
            </p>
          </div>
          {/* 回収率 */}
<div className="rounded-2xl border border-indigo-500/40 bg-slate-900/80 p-4 shadow-md shadow-indigo-500/20">
  <p className="text-[11px] font-medium text-slate-300">回収率</p>
  <p className="mt-3 text-2xl font-semibold text-indigo-200">
    {summary.recoveryRate.toFixed(1)}%
  </p>
  <p className="mt-2 text-[11px] text-slate-400">
    入金済み合計 ÷ 請求合計（当年）で算出しています。
  </p>
</div>
        </section>

        <section className="grid gap-6 md:grid-cols-2">
          <div className="rounded-2xl border border-slate-800 bg-slate-900/80 p-5 shadow-md">
            <div className="flex items-center justify-between mb-3">
              <h2 className="text-sm font-semibold text-slate-100">
                未入金一覧（Top5）
              </h2>
              <Link
                href="/invoices?status=unpaid"
                className="text-[11px] text-sky-300 hover:text-sky-200"
              >
                すべて表示 →
              </Link>
            </div>

            <div className="overflow-hidden rounded-xl border border-slate-800 bg-slate-950/40">
              <table className="hidden sm:table min-w-full text-xs">
                <thead className="bg-slate-900/90">
                  <tr className="text-left text-[11px] text-slate-400">
                    <th className="px-4 py-2 font-medium">請求番号</th>
                    <th className="px-4 py-2 font-medium">顧客</th>
                    <th className="px-4 py-2 font-medium text-right">金額</th>
                  </tr>
                </thead>
                <tbody>
                  {summary.unpaidInvoices.map((inv) => (
                    <tr
                      key={inv.invoiceId}
                      className="border-t border-slate-800/80 hover:bg-slate-800/70 cursor-pointer"
                    >
                      <td className="px-4 py-2 align-middle text-slate-100">
                        <Link href={`/invoices/${inv.invoiceId}`}>{inv.invoiceNumber}</Link>
                      </td>
                      <td className="px-4 py-2 align-middle text-slate-200">
                        {inv.clientName}
                      </td>
                      <td className="px-4 py-2 align-middle text-right text-slate-100">
                        {formatCurrency(inv.amount)}
                      </td>
                    </tr>
                  ))}

                  {summary.unpaidInvoices.length === 0 && (
                    <tr>
                      <td
                        colSpan={3}
                        className="px-4 py-6 text-center text-slate-400 text-[11px]"
                      >
                        現在、未入金の請求書はありません。
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
                {/* スマホだけカード */}
  <div className="sm:hidden divide-y divide-slate-800/80">
    {summary.unpaidInvoices.map((inv) => (
      <Link
        key={inv.invoiceId}
        href={`/invoices/${inv.invoiceId}`}
        className="block px-4 py-3 hover:bg-slate-800/40"
      >
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0">
            <p className="text-sm font-semibold text-slate-100 truncate">
              {inv.invoiceNumber}
            </p>
            <p className="mt-1 text-xs text-slate-300 truncate">
              {inv.clientName}
            </p>
          </div>

          <div className="shrink-0 text-right">
            <p className="text-sm font-semibold text-slate-100">
              {formatCurrency(inv.amount)}
            </p>

            {inv.isOverdue && (
              <span className="mt-1 inline-flex items-center rounded-full border border-amber-400/40 bg-amber-500/10 px-2 py-0.5 text-[10px] font-medium text-amber-200">
                期限超過
              </span>
            )}
          </div>
        </div>
      </Link>
    ))}

    {summary.unpaidInvoices.length === 0 && (
      <div className="px-4 py-6 text-center text-slate-400 text-[11px]">
        現在、未入金の請求書はありません。
      </div>
    )}
  </div>
            </div>
          </div>

          <div className="rounded-2xl border border-slate-800 bg-slate-900/80 p-5 shadow-md">
            <MonthlySalesChartClient
              year={summary.year}
              availableYears={summary.availableYears}
              monthlySales={summary.monthlySales}
            />
          </div>
        </section>

        <section className="rounded-2xl border border-slate-800 bg-slate-900/80 p-5 shadow-md">
          <div className="flex items-center justify-between mb-3">
            <h2 className="text-sm font-semibold text-slate-100">
              回収率ワースト顧客 TOP5
            </h2>
            <Link
              href={`/sales/by-member?year=${selectedYear}`}
              className="text-[11px] text-sky-300 hover:text-sky-200"
            >
              顧客別集計を見る →
            </Link>
          </div>

          <p className="mb-3 text-[11px] text-slate-400">
            ※ 未回収（残額）がある顧客のみ。回収率が低い順に表示します。
          </p>

          <div className="overflow-hidden rounded-xl border border-slate-800 bg-slate-950/40">
            <table className="min-w-full text-xs">
              <thead className="bg-slate-900/90">
                <tr className="text-left text-[11px] text-slate-400">
                  <th className="px-4 py-2 font-medium">顧客</th>
                  <th className="px-4 py-2 font-medium text-right">回収率</th>
                  <th className="px-4 py-2 font-medium text-right">未回収</th>
                  <th className="px-4 py-2 font-medium text-right">請求合計</th>
                </tr>
              </thead>

              <tbody>
                {worstTop5.rows.map((r) => (
                  <tr
                    key={r.memberId}
                    className="border-t border-slate-800/80 hover:bg-slate-800/70"
                  >
                    <td className="px-4 py-2 text-slate-100">
                      <Link
                        href={`/sales?year=${selectedYear}&memberId=${r.memberId}`}
                        className="hover:text-sky-300"
                      >
                        {r.memberName}
                      </Link>
                    </td>

                    <td className="px-4 py-2 text-right text-sky-200">
                      {Number(r.recoveryRate).toFixed(1)}%
                    </td>

                    <td className="px-4 py-2 text-right text-amber-300">
                      {formatCurrency(r.remainingTotal)}
                    </td>

                    <td className="px-4 py-2 text-right text-slate-100">
                      {formatCurrency(r.invoiceTotal)}
                    </td>
                  </tr>
                ))}

                {worstTop5.rows.length === 0 && (
                  <tr>
                    <td
                      colSpan={4}
                      className="px-4 py-6 text-center text-slate-400 text-[11px]"
                    >
                      未回収がある顧客がありません（回収率ワースト対象なし）
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </section>

<section className="rounded-2xl border border-slate-800 bg-slate-900/80 p-5 shadow-md">
  <div className="flex items-center justify-between mb-3">
    <h2 className="text-sm font-semibold text-slate-100">
      最近の操作（直近5件）
    </h2>
    <p className="text-[11px] text-slate-400">
      管理者の操作履歴を簡易表示しています。
    </p>
  </div>

  <div className="overflow-hidden rounded-xl border border-slate-800 bg-slate-950/40">
  <table className="hidden sm:table min-w-full text-xs">
      <thead className="bg-slate-900/90">
        <tr className="text-left text-[11px] text-slate-400">
          <th className="px-4 py-2 font-medium">日時</th>
          <th className="px-4 py-2 font-medium">操作</th>
          <th className="px-4 py-2 font-medium">対象</th>
          <th className="px-4 py-2 font-medium">内容</th>
          <th className="px-4 py-2 font-medium text-right">actor</th>
        </tr>
      </thead>

      <tbody>
        {recentLogs.map((x) => (
          <tr key={x.id} className="border-t border-slate-800/80 hover:bg-slate-800/70">
            <td className="px-4 py-2 text-slate-200">
              {new Date(x.at).toLocaleString("ja-JP")}
            </td>
            <td className="px-4 py-2 text-slate-100">
              {formatActionLabel(x.action)}
            </td>
            <td className="px-4 py-2 text-slate-200">
              {formatTarget(x.entity, x.entityId)}
            </td>
            <td className="px-4 py-2 text-slate-200">
              {x.summary || "-"}
            </td>
            <td className="px-4 py-2 text-right text-slate-400">
              {x.actorUserId}
            </td>
          </tr>
        ))}

        {recentLogs.length === 0 && (
          <tr>
            <td colSpan={5} className="px-4 py-6 text-center text-slate-400 text-[11px]">
              最近の操作ログはありません。
            </td>
          </tr>
        )}
      </tbody>
    </table>
      <div className="sm:hidden divide-y divide-slate-800/80">
    {recentLogs.map((x) => (
      <div key={x.id} className="px-4 py-3">
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0">
            <p className="text-xs text-slate-400">
              {new Date(x.at).toLocaleString("ja-JP")}
            </p>
            <p className="mt-1 text-sm font-semibold text-slate-100">
              {formatActionLabel(x.action)}
            </p>
            <p className="mt-1 text-xs text-slate-300 truncate">
              {formatTarget(x.entity, x.entityId)}
            </p>
          </div>
          <p className="shrink-0 text-[11px] text-slate-500">actor: {x.actorUserId}</p>
        </div>

        <p className="mt-2 text-xs text-slate-200">
          {x.summary || "-"}
        </p>
      </div>
    ))}

    {recentLogs.length === 0 && (
      <div className="px-4 py-6 text-center text-slate-400 text-[11px]">
        最近の操作ログはありません。
      </div>
    )}
  </div>
  </div>
</section>

{/* 下段：請求書一覧 / 会員一覧 / 売上一覧 へのナビカード */}
<section className="grid gap-6 md:grid-cols-3">
  {/* 請求書一覧 */}
  <Link
    href="/invoices"
    className="group relative overflow-hidden rounded-2xl border border-slate-800 bg-slate-900/90 p-6 shadow-md transition hover:-translate-y-1 hover:border-sky-500/60 hover:shadow-sky-500/20"
  >
    <div className="flex items-start justify-between gap-4">
      <div>
        <h2 className="flex items-center gap-2 text-lg font-semibold text-slate-50">
          <span className="inline-flex h-8 w-8 items-center justify-center rounded-full bg-sky-500/10 text-sky-300 text-lg">
            📄
          </span>
          請求書一覧
        </h2>
        <p className="mt-2 text-sm text-slate-300">
          請求番号・顧客名・ステータス・請求日などで検索し、
          入金状況や支払期限を一覧で確認できます。
        </p>
      </div>
    </div>

    <div className="mt-4 flex items-center justify-between">
      <div className="flex flex-col text-[11px] text-slate-400">
        <span>・ページネーション（5件 / ページ）</span>
        <span>・ステータス別のカラーラベル</span>
        <span>・請求書詳細から入金登録・催促へ遷移</span>
      </div>
      <span className="inline-flex items-center gap-1 rounded-full bg-sky-500/10 px-3 py-1 text-xs font-medium text-sky-300 group-hover:bg-sky-500/20">
        一覧を開く
        <span aria-hidden>→</span>
      </span>
    </div>
  </Link>

  {/* 会員一覧 */}
  <Link
    href="/members"
    className="group relative overflow-hidden rounded-2xl border border-slate-800 bg-slate-900/90 p-6 shadow-md transition hover:-translate-y-1 hover:border-emerald-500/60 hover:shadow-emerald-500/20"
  >
    <div className="flex items-start justify-between gap-4">
      <div>
        <h2 className="flex items-center gap-2 text-lg font-semibold text-slate-50">
          <span className="inline-flex h-8 w-8 items-center justify-center rounded-full bg-emerald-500/10 text-emerald-300 text-lg">
            👥
          </span>
          会員一覧
        </h2>
        <p className="mt-2 text-sm text-slate-300">
          顧客（会員）の基本情報・ロール・有効 / 無効状態を確認し、
          必要に応じて無効化や権限変更を行います。
        </p>
      </div>
    </div>

    <div className="mt-4 flex items-center justify-between">
      <div className="flex flex-col text-[11px] text-slate-400">
        <span>・キーワード検索（名前 / メール）</span>
        <span>・ロール絞り込み（管理者 / 一般）</span>
        <span>・ステータスバッジ表示</span>
      </div>
      <span className="inline-flex items-center gap-1 rounded-full bg-emerald-500/10 px-3 py-1 text-xs font-medium text-emerald-300 group-hover:bg-emerald-500/20">
        一覧を開く
        <span aria-hidden>→</span>
      </span>
    </div>
  </Link>

  {/* ★追加：売上一覧 */}
  <Link
    href="/sales"
    className="group relative overflow-hidden rounded-2xl border border-slate-800 bg-slate-900/90 p-6 shadow-md transition hover:-translate-y-1 hover:border-indigo-500/60 hover:shadow-indigo-500/20"
  >
    <div className="flex items-start justify-between gap-4">
      <div>
        <h2 className="flex items-center gap-2 text-lg font-semibold text-slate-50">
          <span className="inline-flex h-8 w-8 items-center justify-center rounded-full bg-indigo-500/10 text-indigo-300 text-lg">
            📈
          </span>
          売上一覧
        </h2>
        <p className="mt-2 text-sm text-slate-300">
          請求ベースの売上を一覧で確認し、入金済み・残額をあわせて把握します。
          年/月/ステータスで絞り込み可能です。
        </p>
      </div>
    </div>

    <div className="mt-4 flex items-center justify-between">
      <div className="flex flex-col text-[11px] text-slate-400">
        <span>・年 / 月 / ステータス絞り込み</span>
        <span>・請求 / 入金済 / 残額を並列表示</span>
        <span>・請求書詳細へ遷移</span>
      </div>
      <span className="inline-flex items-center gap-1 rounded-full bg-indigo-500/10 px-3 py-1 text-xs font-medium text-indigo-300 group-hover:bg-indigo-500/20">
        一覧を開く
        <span aria-hidden>→</span>
      </span>
    </div>
  </Link>

  {/* ★追加：入金一覧 */}
<Link
  href="/payments"
  className="group relative overflow-hidden rounded-2xl border border-slate-800 bg-slate-900/90 p-6 shadow-md transition hover:-translate-y-1 hover:border-emerald-500/60 hover:shadow-emerald-500/20"
>
  <div className="flex items-start justify-between gap-4">
    <div>
      <h2 className="flex items-center gap-2 text-lg font-semibold text-slate-50">
        <span className="inline-flex h-8 w-8 items-center justify-center rounded-full bg-emerald-500/10 text-emerald-300 text-lg">
          💰
        </span>
        入金一覧
      </h2>
      <p className="mt-2 text-sm text-slate-300">
        登録された入金明細を一覧で確認し、
        請求書との紐づきや入金状況を把握します。
      </p>
    </div>
  </div>

  <div className="mt-4 flex items-center justify-between">
    <div className="flex flex-col text-[11px] text-slate-400">
      <span>・入金日 / 金額 / 名義</span>
      <span>・割当済み請求書の確認</span>
      <span>・部分入金 / 複数割当対応</span>
    </div>
    <span className="inline-flex items-center gap-1 rounded-full bg-emerald-500/10 px-3 py-1 text-xs font-medium text-emerald-300 group-hover:bg-emerald-500/20">
      一覧を開く →
    </span>
  </div>
</Link>

</section>

      </main>
    </div>
  );
}
