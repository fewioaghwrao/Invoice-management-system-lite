"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

export default function SalesTabsClient() {
  const pathname = usePathname();

  const tabs = [
    { href: "/sales", label: "請求書ベース" },
    { href: "/sales/by-member", label: "顧客別集計" },
  ];

  return (
    <div className="flex gap-2 border-b border-slate-800">
      {tabs.map((t) => {
        const active = pathname === t.href;
        return (
          <Link
            key={t.href}
            href={t.href}
            className={`px-4 py-2 text-sm border-b-2 ${
              active
                ? "border-sky-400 text-sky-300"
                : "border-transparent text-slate-400 hover:text-slate-200"
            }`}
          >
            {t.label}
          </Link>
        );
      })}
    </div>
  );
}
