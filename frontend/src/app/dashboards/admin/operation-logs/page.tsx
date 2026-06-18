import Link from "next/link";
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

  if (Number.isNaN(page) || page < 1) return 1;
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
    <div className="min-h-screen bg-slate-950 text-slate-50">
      <div className="pointer-events-none fixed inset-0 bg-[radial-gradient(circle_at_top,_#0ea5e9_0,_transparent_55%),radial-gradient(circle_at_bottom,_#6366f1_0,_transparent_55%)] opacity-60" />

      <header className="relative z-10 border-b border-slate-800 bg-slate-950/80 backdrop-blur">
        <div className="mx-auto flex max-w-6xl items-center justify-between gap-4 px-6 py-4">
          <div className="min-w-0">
            <h1 className="truncate text-lg font-semibold text-slate-50">
              操作ログ一覧
            </h1>
            <p className="mt-1 text-xs text-slate-400">
              管理者による請求書・入金・催促履歴などの操作を確認できます。
            </p>
          </div>

          <Link
            href="/dashboards/admin"
            className="shrink-0 rounded-full border border-slate-700 bg-slate-900/60 px-4 py-2 text-xs text-slate-300 hover:border-sky-400 hover:text-sky-300"
          >
            ダッシュボードへ戻る
          </Link>
        </div>
      </header>

      <main className="relative z-10 mx-auto max-w-6xl px-6 py-8">
        <section className="rounded-2xl border border-slate-800 bg-slate-900/80 p-5 shadow-md">
          <div className="mb-4 flex items-center justify-between gap-3">
            <div>
              <h2 className="text-sm font-semibold text-slate-100">
                監査ログ
              </h2>
              <p className="mt-1 text-[11px] text-slate-400">
                全 {totalCount} 件の操作ログを {PAGE_SIZE} 件ずつ表示しています。
              </p>
            </div>

            <div className="rounded-full border border-slate-700 bg-slate-950/50 px-3 py-1 text-[11px] text-slate-300">
              {currentPage} / {totalPages} ページ
            </div>
          </div>

          <div className="overflow-hidden rounded-xl border border-slate-800 bg-slate-950/40">
            <table className="hidden min-w-full text-xs sm:table">
              <thead className="bg-slate-900/90">
                <tr className="text-left text-[11px] text-slate-400">
                  <th className="px-4 py-3 font-medium">日時</th>
                  <th className="px-4 py-3 font-medium">操作</th>
                  <th className="px-4 py-3 font-medium">対象</th>
                  <th className="px-4 py-3 font-medium">内容</th>
                  <th className="px-4 py-3 font-medium text-right">actor</th>
                </tr>
              </thead>

              <tbody>
                {logs.map((x) => (
                  <tr
                    key={x.id}
                    className="border-t border-slate-800/80 hover:bg-slate-800/70"
                  >
                    <td className="whitespace-nowrap px-4 py-3 text-slate-200">
                      {new Date(x.at).toLocaleString("ja-JP")}
                    </td>

                    <td className="whitespace-nowrap px-4 py-3 text-slate-100">
                      {formatOperationActionLabel(x.action)}
                    </td>

                    <td className="whitespace-nowrap px-4 py-3 text-slate-200">
                      {formatOperationTarget(x.entity, x.entityId)}
                    </td>

                    <td className="px-4 py-3 text-slate-200">
                      {x.summary || "-"}
                    </td>

                    <td className="px-4 py-3 text-right text-slate-400">
                      {x.actorUserId}
                    </td>
                  </tr>
                ))}

                {logs.length === 0 && (
                  <tr>
                    <td
                      colSpan={5}
                      className="px-4 py-8 text-center text-slate-400"
                    >
                      操作ログはありません。
                    </td>
                  </tr>
                )}
              </tbody>
            </table>

            <div className="divide-y divide-slate-800/80 sm:hidden">
              {logs.map((x) => (
                <div key={x.id} className="px-4 py-3">
                  <div className="flex items-start justify-between gap-3">
                    <div className="min-w-0">
                      <p className="text-xs text-slate-400">
                        {new Date(x.at).toLocaleString("ja-JP")}
                      </p>
                      <p className="mt-1 text-sm font-semibold text-slate-100">
                        {formatOperationActionLabel(x.action)}
                      </p>
                      <p className="mt-1 truncate text-xs text-slate-300">
                        {formatOperationTarget(x.entity, x.entityId)}
                      </p>
                    </div>

                    <p className="shrink-0 text-[11px] text-slate-500">
                      actor: {x.actorUserId}
                    </p>
                  </div>

                  <p className="mt-2 text-xs text-slate-200">
                    {x.summary || "-"}
                  </p>
                </div>
              ))}

              {logs.length === 0 && (
                <div className="px-4 py-8 text-center text-xs text-slate-400">
                  操作ログはありません。
                </div>
              )}
            </div>
          </div>

          <div className="mt-4 flex items-center justify-between gap-3">
            {hasPrev ? (
              <Link
                href={`/admin/operation-logs?page=${currentPage - 1}`}
                className="rounded-full border border-slate-700 bg-slate-950/50 px-4 py-2 text-xs text-slate-300 hover:border-sky-400 hover:text-sky-300"
              >
                ← 前へ
              </Link>
            ) : (
              <span className="rounded-full border border-slate-800 bg-slate-950/30 px-4 py-2 text-xs text-slate-600">
                ← 前へ
              </span>
            )}

            <div className="hidden items-center gap-2 sm:flex">
              {pageNumbers[0] > 1 && (
                <>
                  <Link
                    href="/admin/operation-logs?page=1"
                    className="rounded-full border border-slate-700 px-3 py-1 text-xs text-slate-400 hover:border-sky-400 hover:text-sky-300"
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
                    href={`/admin/operation-logs?page=${page}`}
                    className={
                      active
                        ? "rounded-full bg-sky-500/20 px-3 py-1 text-xs font-medium text-sky-200"
                        : "rounded-full border border-slate-700 px-3 py-1 text-xs text-slate-400 hover:border-sky-400 hover:text-sky-300"
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
                    href={`/admin/operation-logs?page=${totalPages}`}
                    className="rounded-full border border-slate-700 px-3 py-1 text-xs text-slate-400 hover:border-sky-400 hover:text-sky-300"
                  >
                    {totalPages}
                  </Link>
                </>
              )}
            </div>

            <div className="text-xs text-slate-400 sm:hidden">
              {currentPage} / {totalPages}
            </div>

            {hasNext ? (
              <Link
                href={`/admin/operation-logs?page=${currentPage + 1}`}
                className="rounded-full border border-slate-700 bg-slate-950/50 px-4 py-2 text-xs text-slate-300 hover:border-sky-400 hover:text-sky-300"
              >
                次へ →
              </Link>
            ) : (
              <span className="rounded-full border border-slate-800 bg-slate-950/30 px-4 py-2 text-xs text-slate-600">
                次へ →
              </span>
            )}
          </div>
        </section>
      </main>
    </div>
  );
}