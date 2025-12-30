"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

type NavItem = {
  href: string;
  title: string;
  sub?: string;
  match?: "exact" | "prefix";
};

const NAV: NavItem[] = [
  {
    href: "/account/invoices",
    title: "請求書一覧",
    sub: "（請求書）",
    match: "prefix",
  },
  {
    href: "/account/unpaid",
    title: "入金確認",
    sub: "（未入金）", // ← “未払い”より業務用語として自然
    match: "exact",
  },
  {
    href: "/account/profile",
    title: "プロフィール",
    sub: "（登録情報）",
    match: "exact",
  },
];

function isActive(pathname: string, item: NavItem) {
  if (item.match === "prefix") return pathname.startsWith(item.href);
  return pathname === item.href;
}

function className(active: boolean) {
  const base =
    "shrink-0 rounded-xl border px-4 py-3 text-xs font-medium transition outline-none";
  const inactive =
    "border-slate-700 bg-slate-950/40 text-slate-200 hover:bg-slate-800 focus-visible:ring-2 focus-visible:ring-slate-500/60";
  const activeCls =
    "border-slate-500 bg-slate-800 text-slate-50 shadow-sm";
  return `${base} ${active ? activeCls : inactive}`;
}

export default function AccountNavClient() {
  const pathname = usePathname();

  return (
    <div className="flex lg:flex-col gap-2 overflow-x-auto lg:overflow-visible pb-1 [-webkit-overflow-scrolling:touch] snap-x">
      {NAV.map((item) => {
        const active = isActive(pathname, item);

        return (
          <Link
            key={item.href}
            href={item.href}
            className={`${className(active)} snap-start`}
            aria-current={active ? "page" : undefined}
          >
            {item.title}
            {item.sub ? (
<span className="ml-2 hidden sm:inline text-[11px] text-slate-400">{item.sub}</span>
            ) : null}
          </Link>
        );
      })}
    </div>
  );
}
