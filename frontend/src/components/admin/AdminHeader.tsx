import Link from "next/link";
import CurrentUserBadge from "@/components/CurrentUserBadge";
import LogoutButton from "@/components/LogoutButton";

type AdminHeaderProps = {
  /** ダッシュボードのみ年度切替を表示 */
  selectedYear?: number;
  /** 管理者トップ以外では業務共通ヘッダーとして表示 */
  variant?: "dashboard" | "management";
};

/**
 * 管理者トップと請求書管理の共通ヘッダー。
 * サイドバーの外・上に配置し、ページ固有の見出しは本文側に残す。
 */
export default function AdminHeader({
  selectedYear,
  variant = "management",
}: AdminHeaderProps) {
  const isDashboard = variant === "dashboard";

  return (
    <header className="relative z-10 border-b border-slate-800 bg-slate-950/80 backdrop-blur">
      <div className="mx-auto flex w-full max-w-[1440px] flex-col gap-4 px-4 py-4 sm:px-6 lg:flex-row lg:items-center lg:justify-between lg:px-8">
        <div className="min-w-0">
          {isDashboard ? (
            <h1 className="text-lg font-semibold leading-snug text-slate-50 sm:text-xl">
              請求・入金ステータスダッシュボード（管理者）
            </h1>
          ) : (
            <Link
              href="/dashboards/admin"
              className="inline-block text-lg font-semibold leading-snug text-slate-50 hover:text-sky-200 sm:text-xl"
            >
              請求・入金管理システム（管理者）
            </Link>
          )}
          <p className="mt-1 text-xs text-slate-400 sm:text-sm">
            {isDashboard
              ? "売上・未入金・請求書と入金の状況を確認できます。"
              : "請求書・入金・会員・売上を管理できます。"}
          </p>
        </div>

        <div className="flex flex-col gap-3 sm:flex-row sm:flex-wrap sm:items-center sm:justify-between lg:justify-end">
          {selectedYear !== undefined && (
            <div className="flex items-center justify-between gap-2 rounded-xl border border-slate-700 bg-slate-900/60 px-3 py-2 sm:justify-start">
              <Link
                href={`/dashboards/admin?year=${selectedYear - 1}`}
                className="rounded px-1 text-xs text-slate-300 hover:text-sky-300 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sky-500"
                aria-label={`${selectedYear - 1}年へ`}
              >
                ← {selectedYear - 1}
              </Link>
              <span className="rounded-full border border-slate-600 bg-slate-800 px-3 py-1 text-xs font-semibold text-slate-100">
                {selectedYear} 年
              </span>
              <Link
                href={`/dashboards/admin?year=${selectedYear + 1}`}
                className="rounded px-1 text-xs text-slate-300 hover:text-sky-300 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sky-500"
                aria-label={`${selectedYear + 1}年へ`}
              >
                {selectedYear + 1} →
              </Link>
            </div>
          )}
          <div className="flex flex-wrap items-center gap-2">
            <CurrentUserBadge />
            <LogoutButton />
          </div>
        </div>
      </div>
    </header>
  );
}
