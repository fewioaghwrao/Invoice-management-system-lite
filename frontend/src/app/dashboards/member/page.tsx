// src/app/dashboards/member/page.tsx
import CurrentUserBadge from "@/components/CurrentUserBadge";
import LogoutButton from "@/components/LogoutButton";
import Link from "next/link";

type QuickLink = {
  href: string;
  title: string;
  desc: string;
  icon: string;
};

function cardAccent(href: string) {
  if (href.startsWith("/account/invoices")) return "hover:border-sky-500/60";
  if (href === "/account/unpaid") return "hover:border-amber-500/60";
  if (href === "/account/profile") return "hover:border-emerald-500/60";
  return "hover:border-slate-500/60";
}

const links: QuickLink[] = [
  {
    href: "/account/invoices",
    title: "請求書一覧",
    desc: "請求書の金額・支払期限・入金状況を確認できます。",
    icon: "📄",
  },
  {
    href: "/account/unpaid",
    title: "未入金の確認",
    desc: "未入金の請求書と支払期限を確認できます。",
    icon: "⏳",
  },
  {
    href: "/account/profile",
    title: "登録情報",
    desc: "氏名・メールアドレス・住所などを確認・変更できます。",
    icon: "👤",
  },
];

export default function MemberDashboardPage() {
  return (
    <div className="flex min-h-screen flex-col bg-gradient-to-b from-zinc-950 via-zinc-900 to-zinc-950">
      {/* スマートフォンはタイトル行とユーザー操作行を分ける */}
      <header className="border-b border-slate-800 bg-slate-950/40 backdrop-blur">
        <div className="mx-auto flex w-full max-w-5xl flex-col gap-3 px-4 py-4 sm:flex-row sm:items-center sm:justify-between sm:gap-4 sm:px-6 sm:py-5">
          <div className="min-w-0">
            <h1 className="text-lg font-semibold text-slate-100">
              マイページ
            </h1>
            <p className="mt-1 text-sm text-slate-400 sm:text-xs">
              請求書やお支払い状況を確認できます。
            </p>
          </div>

          <div className="flex w-full flex-wrap items-center justify-between gap-2 sm:w-auto sm:justify-end sm:gap-3">
            <CurrentUserBadge />
            <LogoutButton />
          </div>
        </div>
      </header>

      {/* メインコンテンツ：余白部分を伸ばし、フッターを画面下部へ */}
      <main className="mx-auto w-full max-w-5xl flex-1 space-y-4 px-4 py-5 sm:space-y-5 sm:px-6 sm:py-8">
        <h2 className="text-base font-semibold text-slate-100">
          ご利用メニュー
        </h2>

        {/* スマホ1列、タブレット2列、PC3列 */}
        <nav
          className="grid grid-cols-1 gap-3 sm:grid-cols-2 sm:gap-4 lg:grid-cols-3"
          aria-label="会員向けメニュー"
        >
          {links.map((x) => (
            <Link
              key={x.href}
              href={x.href}
              className={[
                "group flex h-full min-h-36 flex-col rounded-2xl border border-slate-800 bg-slate-900/60 p-4 shadow-sm transition sm:p-5",
                "hover:-translate-y-0.5 hover:bg-slate-900/80 hover:shadow-lg",
                cardAccent(x.href),
                "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sky-500",
              ].join(" ")}
              aria-label={`${x.title}へ`}
            >
              <h3 className="flex items-center gap-2 text-base font-semibold text-slate-100">
                <span
                  className="inline-flex h-9 w-9 shrink-0 items-center justify-center rounded-full border border-slate-700 bg-slate-950/40 text-lg"
                  aria-hidden="true"
                >
                  {x.icon}
                </span>
                <span>{x.title}</span>
              </h3>

              <p className="mt-3 text-sm leading-relaxed text-slate-300/90">
                {x.desc}
              </p>

              <div className="mt-auto flex justify-end pt-4">
                <span
                  className="rounded-full border border-slate-700 bg-slate-950/40 px-3 py-1.5 text-xs text-slate-300 group-hover:text-slate-100"
                  aria-hidden="true"
                >
                  開く →
                </span>
              </div>
            </Link>
          ))}
        </nav>
      </main>

      <footer className="border-t border-slate-800 bg-slate-950/30">
        <div className="mx-auto w-full max-w-5xl px-4 py-4 text-xs text-slate-500 sm:px-6">
          Invoice Management System
        </div>
      </footer>
    </div>
  );
}
