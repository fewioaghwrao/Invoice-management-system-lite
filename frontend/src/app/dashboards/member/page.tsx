// src/app/dashboards/member/page.tsx
import CurrentUserBadge from "@/components/CurrentUserBadge";
import LogoutButton from "@/components/LogoutButton";
import Link from "next/link";

type QuickLink = {
  href: string;
  title: string;
  desc: string;
  icon: string;
  badge: { label: string; tone: "done" | "planned" | "info" };
};

function badgeClass(tone: QuickLink["badge"]["tone"]) {
  switch (tone) {
    case "done":
      return "border-emerald-500/30 bg-emerald-500/10 text-emerald-200";
    case "planned":
      return "border-amber-500/30 bg-amber-500/10 text-amber-200";
    default:
      return "border-slate-500/30 bg-slate-500/10 text-slate-200";
  }
}

function cardAccent(href: string) {
  if (href.startsWith("/account/invoices")) return "hover:border-sky-500/60"; // 請求書
  if (href === "/account/unpaid") return "hover:border-amber-500/60"; // 未入金（注意喚起）
  if (href === "/account/profile") return "hover:border-emerald-500/60"; // 設定
  return "hover:border-slate-500/60";
}

export default async function MemberDashboardPage() {
  const links: QuickLink[] = [
    {
      href: "/account/invoices",
      title: "自分の請求書一覧",
      desc: "請求書の金額・支払期限・入金状況を確認できます。",
      icon: "📄",
      badge: { label: "実装済み", tone: "done" },
    },
    {
      href: "/account/unpaid",
      title: "入金確認（未入金）",
      desc: "未入金の請求書を確認し、対応が必要なものを把握できます。",
      icon: "⏳",
      badge: { label: "実装済み", tone: "done" },
    },
    {
      href: "/account/profile",
      title: "登録情報の確認",
      desc: "氏名・メール・住所などの登録情報を確認/変更できます。",
      icon: "👤",
      badge: { label: "実装済み", tone: "done" }, // もし未実装なら planned に
    },
  ];

  return (
    <div className="min-h-screen bg-gradient-to-b from-zinc-950 via-zinc-900 to-zinc-950">
      <header className="border-b border-slate-800 bg-slate-950/40 backdrop-blur">
        <div className="mx-auto max-w-5xl px-4 sm:px-6 py-4 sm:py-5 flex items-center justify-between gap-3">
          <div className="min-w-0">
            <h1 className="truncate text-base sm:text-lg font-semibold text-slate-100">
              会員用ダッシュボード
            </h1>
            <p className="mt-1 hidden sm:block text-xs text-slate-400">
              自分の請求書・入金状況を確認するメイン画面です。
            </p>
          </div>

          <div className="flex items-center gap-2 sm:gap-3">
            <CurrentUserBadge />
            <LogoutButton />
          </div>
        </div>
      </header>

      <main className="mx-auto max-w-5xl px-4 sm:px-6 py-6 sm:py-8 space-y-6">
        {/* ポートフォリオ最適化：意図を短く明示 */}
        <section className="rounded-2xl border border-slate-800 bg-slate-900/60 p-4 sm:p-5 shadow-sm">
          <h2 className="text-sm font-semibold text-slate-100">
            できること（会員向け）
          </h2>
          <p className="mt-2 text-xs text-slate-400 leading-relaxed">
            請求書の一覧・支払期限・入金ステータスを確認し、未入金の把握ができます。
            <span className="hidden sm:inline">
              {" "}
              Lite版はコア機能の完成度を優先し、運用系（自動リマインド等）は拡張予定として整理しています。
            </span>
          </p>
        </section>

        {/* クイックリンク（Link入れ子禁止） */}
        <section className="grid gap-4 sm:gap-6 md:grid-cols-2">
          {links.map((x) => (
            <Link
              key={x.href}
              href={x.href}
              className={[
                "group rounded-2xl border border-slate-800 bg-slate-900/60 p-5 sm:p-6 shadow-sm transition",
                "hover:-translate-y-0.5 hover:bg-slate-900/80 hover:shadow-lg",
                cardAccent(x.href),
                "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-500/60",
              ].join(" ")}
              aria-label={`${x.title}へ`}
            >
              <div className="flex items-start justify-between gap-3">
                <div className="min-w-0">
                  <h3 className="text-sm sm:text-base font-semibold text-slate-100 flex items-center gap-2">
                    <span className="inline-flex h-8 w-8 items-center justify-center rounded-full border border-slate-700 bg-slate-950/40 text-base">
                      {x.icon}
                    </span>
                    <span className="truncate">{x.title}</span>
                  </h3>
                  <p className="mt-2 text-xs sm:text-sm text-slate-300/90">
                    {x.desc}
                  </p>
                </div>

                <span
                  className={[
                    "shrink-0 inline-flex items-center rounded-full border px-2.5 py-1 text-[11px] font-medium",
                    badgeClass(x.badge.tone),
                  ].join(" ")}
                >
                  {x.badge.label}
                </span>
              </div>

              <div className="mt-4 flex items-center justify-between">
                <p className="text-[11px] text-slate-500">
                  ※ {x.badge.tone === "planned" ? "段階的に拡張予定" : "動作確認済み"}
                </p>
                <span className="text-xs text-slate-400 group-hover:text-slate-200">
                  開く →
                </span>
              </div>
            </Link>
          ))}
        </section>
      </main>

      <footer className="border-t border-slate-800 bg-slate-950/30">
        <div className="mx-auto max-w-5xl px-4 sm:px-6 py-4 text-[11px] text-slate-500">
          Invoice &amp; Payment Status Dashboard (Lite) / Member Dashboard
        </div>
      </footer>
    </div>
  );
}
