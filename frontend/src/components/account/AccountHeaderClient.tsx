"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import CurrentUserBadge from "@/components/CurrentUserBadge";
import LogoutButton from "@/components/LogoutButton";

type HeaderMeta = { title: string; desc: string };

function getHeaderMeta(pathname: string): HeaderMeta {
  if (pathname.startsWith("/account/invoices")) {
    return {
      title: "請求書一覧",
      desc: "ご自身の請求書を確認できます。",
    };
  }
  if (pathname === "/account/unpaid") {
    return {
      title: "入金確認",
      desc: "未入金の請求や状況を確認できます。",
    };
  }
  if (pathname === "/account/profile") {
    return {
      title: "プロフィール",
      desc: "登録情報の確認・変更を行えます。",
    };
  }
  return {
    title: "アカウント設定",
    desc: "登録情報の確認・変更を行えます。",
  };
}

export default function AccountHeaderClient() {
  const pathname = usePathname();
  const meta = getHeaderMeta(pathname);

  return (
    <header className="border-b border-slate-800 bg-slate-950/40 backdrop-blur">
      <div className="mx-auto max-w-5xl px-4 sm:px-6 py-4 sm:py-5 flex items-center justify-between gap-3">
        <div className="min-w-0">
          <h1 className="truncate text-base sm:text-lg font-semibold text-slate-100">
            {meta.title}
          </h1>
          <p className="mt-1 hidden sm:block text-xs text-slate-400">
            {meta.desc}
          </p>
        </div>

        <div className="flex items-center gap-2 sm:gap-3">
          {/* メイン画面へ戻る（レスポンシブ） */}
          <Link
            href="/dashboards/member"
            className="inline-flex h-9 items-center rounded-lg border border-slate-700 bg-slate-900/50 px-2 sm:px-3 text-xs font-medium text-slate-200 hover:bg-slate-800 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-500/60"
          >
            <span className="sm:hidden">← 戻る</span>
            <span className="hidden sm:inline">← メイン画面へ</span>
          </Link>

          <CurrentUserBadge />
          <LogoutButton />
        </div>
      </div>
    </header>
  );
}
