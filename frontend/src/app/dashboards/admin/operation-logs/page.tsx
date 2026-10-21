import Link from "next/link";
import AdminHeader from "@/components/admin/AdminHeader";
import AdminSidebar from "@/components/admin/AdminSidebar";
import { apiGetServer } from "@/lib/api.server";
import {
  formatOperationActionLabel,
  formatOperationTarget,
} from "@/lib/operationLogFormat";

type ApiAdminOperationLogDto = {
  id: number;
  at: string;
  actorUserId: number;
  action: string;
  entity: string;
  entityId?: string | null;
  summary: string;
};

type ApiAdminOperationLogListResultDto = {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  items: ApiAdminOperationLogDto[];
};

const PAGE_SIZE = 10;
const OPERATION_LOGS_PATH = "/dashboards/admin/operation-logs";

async function getOperationLogs(
  page: number,
  pageSize = PAGE_SIZE
): Promise<ApiAdminOperationLogListResultDto> {
  return apiGetServer<ApiAdminOperationLogListResultDto>(
    `/api/admin/operation-logs?page=${page}&pageSize=${pageSize}`
  );
}

function parsePage(value: string | string[] | undefined): number {
  const raw = Array.isArray(value) ? value[0] : value;
  const page = Number(raw ?? 1);

  if (!Number.isFinite(page) || page < 1) return 1;
  return Math.floor(page);
}

function buildPageNumbers(currentPage: number, totalPages: number): number[] {
  const maxVisible = 7;

  if (totalPages <= maxVisible) {
    return Array.from({ length: totalPages }, (_, i) => i + 1);
  }

  const start = Math.max(1, currentPage - 3);
  const end = Math.min(totalPages, start + maxVisible - 1);
  const adjustedStart = Math.max(1, end - maxVisible + 1);

  return Array.from(
    { length: end - adjustedStart + 1 },
    (_, i) => adjustedStart + i
  );
}

export default async function AdminOperationLogsPage({
  searchParams,
}: {
  searchParams: Promise<{ [key: string]: string | string[] | undefined }>;
}) {
  const params = await searchParams;

  const requestedPage = parsePage(params.page);
  const result = await getOperationLogs(requestedPage, PAGE_SIZE);

  const logs = result.items ?? [];
  const currentPage = result.page;
  const totalPages = Math.max(1, result.totalPages);
  const totalCount = result.totalCount;

  const hasPrev = currentPage > 1;
  const hasNext = currentPage < totalPages;
  const pageNumbers = buildPageNumbers(currentPage, totalPages);

  return (
    <div className="relative flex min-h-screen flex-col bg-slate-950 text-slate-50">
      <div
        aria-hidden="true"
        className="pointer-events-none fixed inset-0 bg-[radial-gradient(circle_at_top,_#0ea5e9_0,_transparent_55%),radial-gradient(circle_at_bottom,_#6366f1_0,_transparent_55%)] opacity-40"
      />

      {/* 共通ヘッダーはサイドバーより上に配置。管理者トップと同じ構成。 */}
      <AdminHeader variant="management" />

      <div className="relative z-10 mx-auto flex w-full max-w-[1440px] flex-1 flex-col gap-5 px-4 py-6 sm:px-6 lg:flex-row lg:items-start lg:gap-6 lg:px-8">
        <AdminSidebar activeSection="operation-logs" />

        <main className="w-full min-w-0 flex-1 space-y-5">
          {/* 画面固有のサブヘッダーは角丸カード */}
          <header className="rounded-2xl border border-slate-800 bg-slate-900/60 px-4 py-4 shadow-sm sm:px-5 sm:py-5">
            <div className="flex flex-wrap items-start justify-between gap-3">
              <div className="min-w-0">
                <nav aria-label="パンくずリスト" className="flex flex-wrap items-center gap-2 text-[11px] text-slate-400">
                  <Link href="/dashboards/admin" className="hover:text-sky-300">
                    ダッシュボード
                  </Link>
                  <span aria-hidden="true">/</span>
                  <span aria-current="page">操作ログ一覧</span>
                </nav>
                <h1 className="mt-1 text-lg font-semibold text-slate-50">
                  操作ログ一覧
                </h1>
                <p className="mt-1 text-xs leading-relaxed text-slate-400">
                  管理者による請求書・入金・催促履歴などの操作を確認できます。
                </p>
              </div>

              <Link
                href="/dashboards/admin"
                className="inline-flex shrink-0 items-center justify-center rounded-lg border border-slate-700 bg-slate-950/40 px-3 py-2 text-xs text-slate-200 hover:border-sky-400 hover:text-sky-300"
              >
                ダッシュボードへ戻る
              </Link>
            </div>
          </header>

          <section className="min-w-0 rounded-2xl border border-slate-800 bg-slate-900/80 p-4 shadow-md sm:p-5">
            <div className="mb-4 flex flex-wrap items-start justify-between gap-3">
              <div className="min-w-0">
                <h2 className="text-sm font-semibold text-slate-100">監査ログ</h2>
                <p className="mt-1 text-[11px] text-slate-400">
                  全 {totalCount} 件の操作ログを {PAGE_SIZE} 件ずつ表示しています。
                </p>
              </div>
              <div className="shrink-0 rounded-full border border-slate-700 bg-slate-950/50 px-3 py-1 text-[11px] text-slate-300">
                {currentPage} / {totalPages} ページ
              </div>
            </div>

            {logs.length === 0 ? (
              <div className="rounded-xl border border-slate-800 bg-slate-950/40 px-4 py-8 text-center text-xs text-slate-400">
                操作ログはありません。
              </div>
            ) : (
              <>
                {/* デスクトップはテーブル。幅の厳しい端末はカード表示。 */}
                <div className="hidden min-w-0 max-w-full overflow-x-auto rounded-xl border border-slate-800 bg-slate-950/40 xl:block">
                  <table className="w-full min-w-[800px] text-xs">
                    <thead className="bg-slate-900/90">
                      <tr className="text-left text-[11px] text-slate-400">
                        <th className="whitespace-nowrap px-3 py-3 font-medium">日時</th>
                        <th className="whitespace-nowrap px-3 py-3 font-medium">操作</th>
                        <th className="px-3 py-3 font-medium">対象</th>
                        <th className="px-3 py-3 font-medium">内容</th>
                        <th className="whitespace-nowrap px-3 py-3 text-right font-medium">実行者ID</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-800/80">
                      {logs.map((x) => (
                        <tr key={x.id} className="hover:bg-slate-800/70">
                          <td className="whitespace-nowrap px-3 py-3 text-slate-200">
                            {new Date(x.at).toLocaleString("ja-JP")}
                          </td>
                          <td className="px-3 py-3 text-slate-100">
                            {formatOperationActionLabel(x.action)}
                          </td>
                          <td className="break-words px-3 py-3 text-slate-200">
                            {formatOperationTarget(x.entity, x.entityId)}
                          </td>
                          <td className="break-words px-3 py-3 text-slate-200">
                            {x.summary || "-"}
                          </td>
                          <td className="whitespace-nowrap px-3 py-3 text-right tabular-nums text-slate-400">
                            {x.actorUserId}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>

                <div className="grid min-w-0 gap-3 xl:hidden">
                  {logs.map((x) => (
                    <article
                      key={x.id}
                      className="min-w-0 rounded-xl border border-slate-800 bg-slate-950/40 p-3 sm:p-4"
                    >
                      <div className="flex flex-wrap items-start justify-between gap-2">
                        <div className="min-w-0">
                          <p className="text-[11px] text-slate-400">
                            {new Date(x.at).toLocaleString("ja-JP")}
                          </p>
                          <p className="mt-1 text-sm font-semibold text-slate-100">
                            {formatOperationActionLabel(x.action)}
                          </p>
                        </div>
                        <span className="rounded-full border border-slate-700 bg-slate-900/60 px-2 py-1 text-[11px] text-slate-400">
                          実行者ID: {x.actorUserId}
                        </span>
                      </div>
                      <p className="mt-2 break-words text-xs text-slate-300 [overflow-wrap:anywhere]">
                        {formatOperationTarget(x.entity, x.entityId)}
                      </p>
                      <p className="mt-2 break-words text-xs leading-relaxed text-slate-200 [overflow-wrap:anywhere]">
                        {x.summary || "-"}
                      </p>
                    </article>
                  ))}
                </div>
              </>
            )}

            <nav aria-label="操作ログのページ切替" className="mt-4 flex flex-wrap items-center justify-between gap-2">
              {hasPrev ? (
                <Link
                  href={`${OPERATION_LOGS_PATH}?page=${currentPage - 1}`}
                  className="rounded-lg border border-slate-700 bg-slate-950/50 px-3 py-2 text-xs text-slate-300 hover:border-sky-400 hover:text-sky-300"
                >
                  ← 前へ
                </Link>
              ) : (
                <span className="rounded-lg border border-slate-800 bg-slate-950/30 px-3 py-2 text-xs text-slate-600">
                  ← 前へ
                </span>
              )}

              <div className="hidden flex-wrap items-center justify-center gap-2 md:flex">
                {pageNumbers[0] > 1 && (
                  <>
                    <Link
                      href={`${OPERATION_LOGS_PATH}?page=1`}
                      className="rounded-lg border border-slate-700 px-3 py-1.5 text-xs text-slate-400 hover:border-sky-400 hover:text-sky-300"
                    >
                      1
                    </Link>
                    <span className="text-xs text-slate-500">...</span>
                  </>
                )}

                {pageNumbers.map((page) => {
                  const active = page === currentPage;

                  return (
                    <Link
                      key={page}
                      href={`${OPERATION_LOGS_PATH}?page=${page}`}
                      aria-current={active ? "page" : undefined}
                      className={
                        active
                          ? "rounded-lg bg-sky-500/20 px-3 py-1.5 text-xs font-medium text-sky-200"
                          : "rounded-lg border border-slate-700 px-3 py-1.5 text-xs text-slate-400 hover:border-sky-400 hover:text-sky-300"
                      }
                    >
                      {page}
                    </Link>
                  );
                })}

                {pageNumbers[pageNumbers.length - 1] < totalPages && (
                  <>
                    <span className="text-xs text-slate-500">...</span>
                    <Link
                      href={`${OPERATION_LOGS_PATH}?page=${totalPages}`}
                      className="rounded-lg border border-slate-700 px-3 py-1.5 text-xs text-slate-400 hover:border-sky-400 hover:text-sky-300"
                    >
                      {totalPages}
                    </Link>
                  </>
                )}
              </div>

              <span className="text-xs text-slate-400 md:hidden">
                {currentPage} / {totalPages}
              </span>

              {hasNext ? (
                <Link
                  href={`${OPERATION_LOGS_PATH}?page=${currentPage + 1}`}
                  className="rounded-lg border border-slate-700 bg-slate-950/50 px-3 py-2 text-xs text-slate-300 hover:border-sky-400 hover:text-sky-300"
                >
                  次へ →
                </Link>
              ) : (
                <span className="rounded-lg border border-slate-800 bg-slate-950/30 px-3 py-2 text-xs text-slate-600">
                  次へ →
                </span>
              )}
            </nav>
          </section>
        </main>
      </div>
    </div>
  );
}
