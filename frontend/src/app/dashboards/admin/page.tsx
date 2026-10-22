// src/app/dashboards/admin/page.tsx
import Link from "next/link";
import AdminSidebar from "@/components/admin/AdminSidebar";
import AdminHeader from "@/components/admin/AdminHeader";
import MonthlySalesChartClient from "@/components/MonthlySalesChartClient";
import { apiGetServer } from "@/lib/api.server";
import {
  formatOperationActionLabel,
  formatOperationTarget,
} from "@/lib/operationLogFormat";

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
  const yearParam = Array.isArray(params.year) ? params.year[0] : params.year;
  const selectedYear = yearParam ? Number(yearParam) || currentYear : currentYear;

  // 既存の API と集計内容は変更しない
  const summary = await getAdminSummary(selectedYear);
  const worstTop5 = await getWorstTop5(selectedYear);
  const recentLogs = await getRecentOperationLogs(5);

  const kpis = [
    {
      label: "売上合計（請求金額）",
      value: formatCurrency(summary.totalSales),
      hint: "発行済み請求書の合計",
      accent: "border-sky-500/40",
      valueColor: "text-slate-50",
    },
    {
      label: "未入金額",
      value: formatCurrency(summary.unpaidAmount),
      hint: "未回収残額の合計",
      accent: "border-amber-400/40",
      valueColor: "text-amber-300",
    },
    {
      label: "請求書数",
      value: `${summary.invoiceCount} 件`,
      hint: "対象年の請求書",
      accent: "border-slate-700",
      valueColor: "text-slate-50",
    },
    {
      label: "入金数",
      value: `${summary.paymentCount} 件`,
      hint: "登録された入金明細",
      accent: "border-emerald-500/40",
      valueColor: "text-emerald-300",
    },
    {
      label: "回収率",
      value: `${summary.recoveryRate.toFixed(1)}%`,
      hint: "入金済み合計 ÷ 請求合計",
      accent: "border-indigo-500/40",
      valueColor: "text-indigo-200",
    },
  ];

  const panel = "rounded-2xl border border-slate-800 bg-slate-900/80 p-4 shadow-sm sm:p-5";

  return (
    <div className="relative flex min-h-screen flex-col bg-slate-950 text-slate-50">
      <div className="pointer-events-none fixed inset-0 bg-[radial-gradient(circle_at_top,_#0ea5e9_0,_transparent_55%),radial-gradient(circle_at_bottom,_#6366f1_0,_transparent_55%)] opacity-40" />

      <AdminHeader variant="dashboard" selectedYear={selectedYear} />

      <div className="relative z-10 mx-auto flex w-full max-w-[1440px] flex-1 flex-col gap-5 px-4 py-6 sm:px-6 lg:flex-row lg:items-start lg:gap-6 lg:px-8">
        <AdminSidebar activeSection="dashboard" />

        {/* ダッシュボード本体 */}
        <main className="min-w-0 flex-1 space-y-5 sm:space-y-6" id="overview">
          <section aria-label="主要指標" className="grid grid-cols-2 gap-3 sm:grid-cols-3 xl:grid-cols-5">
            {kpis.map((kpi) => (
              <div
                key={kpi.label}
                className={`min-w-0 rounded-2xl border bg-slate-900/85 p-3 shadow-sm sm:p-4 ${kpi.accent}`}
              >
                <p className="text-xs font-medium text-slate-300">{kpi.label}</p>
                <p className={`mt-2 break-words text-lg font-semibold tabular-nums sm:text-xl ${kpi.valueColor}`}>
                  {kpi.value}
                </p>
                <p className="mt-2 text-[11px] leading-relaxed text-slate-400">{kpi.hint}</p>
              </div>
            ))}
          </section>

          <section className="grid gap-5 xl:grid-cols-2" aria-label="請求・売上の概要">
            <div className={panel}>
              <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
                <h2 className="text-sm font-semibold text-slate-100">未入金一覧（Top5）</h2>
                <Link href="/invoices?status=unpaid" className="text-xs text-sky-300 hover:text-sky-200">
                  すべて表示 →
                </Link>
              </div>
              <div className="overflow-x-auto rounded-xl border border-slate-800 bg-slate-950/40">
                <table className="hidden min-w-[480px] w-full text-xs sm:table">
                  <thead className="bg-slate-900/90 text-left text-slate-400">
                    <tr>
                      <th className="px-3 py-3 font-medium">請求番号</th>
                      <th className="px-3 py-3 font-medium">顧客</th>
                      <th className="px-3 py-3 text-right font-medium">未入金額</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-800/80">
                    {summary.unpaidInvoices.map((inv) => (
                      <tr key={inv.invoiceId} className="hover:bg-slate-800/60">
                        <td className="px-3 py-3 align-middle">
                          <Link href={`/invoices/${inv.invoiceId}`} className="break-all text-sky-200 hover:underline">
                            {inv.invoiceNumber}
                          </Link>
                          {inv.isOverdue && (
                            <span className="mt-1 block text-[11px] text-amber-300">期限超過</span>
                          )}
                        </td>
                        <td className="px-3 py-3 align-middle text-slate-200">{inv.clientName}</td>
                        <td className="whitespace-nowrap px-3 py-3 text-right align-middle font-medium tabular-nums text-slate-100">
                          {formatCurrency(inv.amount)}
                        </td>
                      </tr>
                    ))}
                    {summary.unpaidInvoices.length === 0 && (
                      <tr><td colSpan={3} className="px-3 py-6 text-center text-slate-400">未入金の請求書はありません。</td></tr>
                    )}
                  </tbody>
                </table>
                <div className="divide-y divide-slate-800/80 sm:hidden">
                  {summary.unpaidInvoices.map((inv) => (
                    <Link key={inv.invoiceId} href={`/invoices/${inv.invoiceId}`} className="block px-3 py-3 hover:bg-slate-800/60">
                      <div className="flex items-start justify-between gap-3">
                        <div className="min-w-0">
                          <p className="break-all text-sm font-medium text-slate-100">{inv.invoiceNumber}</p>
                          <p className="mt-1 text-xs text-slate-300">{inv.clientName}</p>
                        </div>
                        <div className="shrink-0 text-right">
                          <p className="text-sm font-semibold tabular-nums">{formatCurrency(inv.amount)}</p>
                          {inv.isOverdue && <p className="mt-1 text-xs text-amber-300">期限超過</p>}
                        </div>
                      </div>
                    </Link>
                  ))}
                  {summary.unpaidInvoices.length === 0 && (
                    <p className="px-3 py-6 text-center text-xs text-slate-400">未入金の請求書はありません。</p>
                  )}
                </div>
              </div>
            </div>

            <div className={panel}>
              <MonthlySalesChartClient
                year={summary.year}
                availableYears={summary.availableYears}
                monthlySales={summary.monthlySales}
              />
            </div>
          </section>

          <section className={panel} aria-labelledby="worst-customers-title">
            <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
              <h2 id="worst-customers-title" className="text-sm font-semibold text-slate-100">
                回収率ワースト顧客 TOP5
              </h2>
              <Link href={`/sales/by-member?year=${selectedYear}`} className="text-xs text-sky-300 hover:text-sky-200">
                顧客別集計を見る →
              </Link>
            </div>
            <p className="mb-3 text-xs text-slate-400">未回収残額がある顧客を、回収率の低い順に表示しています。</p>
            <div className="overflow-x-auto rounded-xl border border-slate-800 bg-slate-950/40">
              <table className="w-full min-w-[480px] text-xs">
                <thead className="bg-slate-900/90 text-left text-slate-400">
                  <tr>
                    <th className="px-3 py-3 font-medium">顧客</th>
                    <th className="px-3 py-3 text-right font-medium">回収率</th>
                    <th className="px-3 py-3 text-right font-medium">未回収</th>
                    <th className="px-3 py-3 text-right font-medium">請求合計</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-800/80">
                  {worstTop5.rows.map((r) => (
                    <tr key={r.memberId} className="hover:bg-slate-800/60">
                      <td className="px-3 py-3 text-slate-100">
                        <Link href={`/sales?year=${selectedYear}&memberId=${r.memberId}`} className="hover:text-sky-300 hover:underline">
                          {r.memberName}
                        </Link>
                      </td>
                      <td className="whitespace-nowrap px-3 py-3 text-right tabular-nums text-sky-200">{Number(r.recoveryRate).toFixed(1)}%</td>
                      <td className="whitespace-nowrap px-3 py-3 text-right tabular-nums text-amber-300">{formatCurrency(r.remainingTotal)}</td>
                      <td className="whitespace-nowrap px-3 py-3 text-right tabular-nums text-slate-100">{formatCurrency(r.invoiceTotal)}</td>
                    </tr>
                  ))}
                  {worstTop5.rows.length === 0 && (
                    <tr><td colSpan={4} className="px-3 py-6 text-center text-slate-400">未回収残額のある顧客はありません。</td></tr>
                  )}
                </tbody>
              </table>
            </div>
          </section>

          <section className={panel} aria-labelledby="recent-logs-title">
            <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
              <h2 id="recent-logs-title" className="text-sm font-semibold text-slate-100">最近の操作（直近5件）</h2>
              <Link href="/dashboards/admin/operation-logs" className="text-xs text-sky-300 hover:text-sky-200">
                すべて表示 →
              </Link>
            </div>
            <div className="overflow-x-auto rounded-xl border border-slate-800 bg-slate-950/40">
              <table className="hidden w-full min-w-[700px] text-xs md:table">
                <thead className="bg-slate-900/90 text-left text-slate-400">
                  <tr>
                    <th className="px-3 py-3 font-medium">日時</th>
                    <th className="px-3 py-3 font-medium">操作</th>
                    <th className="px-3 py-3 font-medium">対象</th>
                    <th className="px-3 py-3 font-medium">内容</th>
                    <th className="px-3 py-3 text-right font-medium">実行者ID</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-800/80">
                  {recentLogs.map((x) => (
                    <tr key={x.id} className="hover:bg-slate-800/60">
                      <td className="whitespace-nowrap px-3 py-3 text-slate-300">{new Date(x.at).toLocaleString("ja-JP")}</td>
                      <td className="whitespace-nowrap px-3 py-3 text-slate-100">{formatOperationActionLabel(x.action)}</td>
                      <td className="px-3 py-3 text-slate-200">{formatOperationTarget(x.entity, x.entityId)}</td>
                      <td className="px-3 py-3 text-slate-200">{x.summary || "-"}</td>
                      <td className="px-3 py-3 text-right tabular-nums text-slate-400">{x.actorUserId}</td>
                    </tr>
                  ))}
                  {recentLogs.length === 0 && (
                    <tr><td colSpan={5} className="px-3 py-6 text-center text-slate-400">最近の操作ログはありません。</td></tr>
                  )}
                </tbody>
              </table>
              <div className="divide-y divide-slate-800/80 md:hidden">
                {recentLogs.map((x) => (
                  <div key={x.id} className="px-3 py-3">
                    <div className="flex flex-wrap items-center justify-between gap-2">
                      <span className="text-xs text-slate-400">{new Date(x.at).toLocaleString("ja-JP")}</span>
                      <span className="text-xs text-slate-400">実行者ID: {x.actorUserId}</span>
                    </div>
                    <p className="mt-2 text-sm font-medium text-slate-100">{formatOperationActionLabel(x.action)}</p>
                    <p className="mt-1 break-all text-xs text-slate-300">{formatOperationTarget(x.entity, x.entityId)}</p>
                    <p className="mt-2 text-xs text-slate-200">{x.summary || "-"}</p>
                  </div>
                ))}
                {recentLogs.length === 0 && (
                  <p className="px-3 py-6 text-center text-xs text-slate-400">最近の操作ログはありません。</p>
                )}
              </div>
            </div>
          </section>
        </main>
      </div>
    </div>
  );
}
