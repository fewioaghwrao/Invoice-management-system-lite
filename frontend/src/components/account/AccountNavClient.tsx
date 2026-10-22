"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

type NavItem = {
  href: string;
  label: string;
  icon: string;
};

const NAV_ITEMS: NavItem[] = [
  { href: "/dashboards/member", label: "マイページ", icon: "⌂" },
  { href: "/account/invoices", label: "請求書一覧", icon: "📄" },
  { href: "/account/unpaid", label: "未入金の確認", icon: "⏳" },
  { href: "/account/profile", label: "登録情報", icon: "👤" },
];

function isActive(pathname: string, href: string): boolean {
  return pathname === href || pathname.startsWith(`${href}/`);
}

export default function AccountNavClient() {
  const pathname = usePathname();

  return (
    <nav aria-label="会員用メニュー" className="flex flex-col gap-1.5">
      {NAV_ITEMS.map((item) => {
        const active = isActive(pathname, item.href);

        return (
          <Link
            key={item.href}
            href={item.href}
            aria-current={active ? "page" : undefined}
            className={[
              "flex min-h-11 items-center gap-2.5 rounded-xl border px-3 py-2.5 text-sm font-medium transition-colors",
              "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sky-400",
              active
                ? "border-sky-500/50 bg-slate-700/70 text-white"
                : "border-transparent text-slate-300 hover:border-slate-700 hover:bg-slate-800/80 hover:text-white",
            ].join(" ")}
          >
            <span aria-hidden="true" className="w-5 shrink-0 text-center">
              {item.icon}
            </span>
            <span>{item.label}</span>
          </Link>
        );
      })}
    </nav>
  );
}
