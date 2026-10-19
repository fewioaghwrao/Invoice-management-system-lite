import Link from "next/link";

/**
 * 管理者画面の共通メニュー。
 * Server Component として利用可能（usePathname への依存なし）。
 * 各ページ / layout から activeSection を明示して選択状態を統一します。
 */
type AdminSection =
  | "dashboard"
  | "invoices"
  | "payments"
  | "members"
  | "sales"
  | "sales-by-member"
  | "operation-logs";

type NavItem = {
  section: AdminSection;
  href: string;
  label: string;
  icon: string;
};

const ADMIN_NAV: NavItem[] = [
  { section: "dashboard", href: "/dashboards/admin", label: "ダッシュボード", icon: "▦" },
  { section: "invoices", href: "/invoices", label: "請求書一覧", icon: "📄" },
  { section: "payments", href: "/payments", label: "入金一覧", icon: "💰" },
  { section: "members", href: "/members", label: "会員一覧", icon: "👥" },
  { section: "sales", href: "/sales", label: "売上一覧", icon: "📈" },
  { section: "sales-by-member", href: "/sales/by-member", label: "顧客別集計", icon: "📊" },
  {
    section: "operation-logs",
    href: "/dashboards/admin/operation-logs",
    label: "操作ログ",
    icon: "🕘",
  },
];

function NavLinks({ activeSection }: { activeSection: AdminSection }) {
  return (
    <>
      {ADMIN_NAV.map((item) => {
        const active = activeSection === item.section;
        return (
          <Link
            key={item.href}
            href={item.href}
            aria-current={active ? "page" : undefined}
            className={[
              "flex min-h-11 items-center gap-3 rounded-xl border px-3 py-2.5 text-sm font-medium transition",
              "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sky-500",
              active
                ? "border-sky-500/40 bg-sky-500/10 text-sky-100"
                : "border-slate-800 bg-slate-900/60 text-slate-300 hover:border-slate-600 hover:bg-slate-800 hover:text-slate-50",
            ].join(" ")}
          >
            <span className="w-5 shrink-0 text-center" aria-hidden="true">
              {item.icon}
            </span>
            <span>{item.label}</span>
          </Link>
        );
      })}
    </>
  );
}

export default function AdminSidebar({ activeSection }: { activeSection: AdminSection }) {
  return (
    <aside className="w-full shrink-0 lg:sticky lg:top-6 lg:w-52" aria-label="管理者メニュー">
      {/* PCは常時表示：details 要素の open 状態には依存しない */}
      <nav className="hidden flex-col gap-2 lg:flex" aria-label="管理者画面の移動">
        <NavLinks activeSection={activeSection} />
      </nav>

      {/* タブレット・スマートフォンでは開閉メニュー */}
      <details className="rounded-xl border border-slate-800 bg-slate-900/90 lg:hidden">
        <summary className="cursor-pointer select-none px-4 py-3 text-sm font-semibold text-slate-100 marker:text-sky-400">
          管理メニューを開く
        </summary>
        <nav
          className="grid gap-2 border-t border-slate-800 p-3 sm:grid-cols-2"
          aria-label="管理者画面の移動（モバイル）"
        >
          <NavLinks activeSection={activeSection} />
        </nav>
      </details>
    </aside>
  );
}
