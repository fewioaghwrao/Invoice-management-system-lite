import type { ReactNode } from "react";
import AdminHeader from "@/components/admin/AdminHeader";
import SalesSidebarClient from "@/components/admin/SalesSidebarClient";

/** 売上一覧・顧客別集計で共通の管理者ヘッダーとナビゲーション。 */
export default function SalesLayout({ children }: { children: ReactNode }) {
  return (
    <div className="relative flex min-h-screen flex-col bg-slate-950 text-slate-50">
      <div
        aria-hidden="true"
        className="pointer-events-none fixed inset-0 bg-[radial-gradient(circle_at_top,_#0ea5e9_0,_transparent_55%),radial-gradient(circle_at_bottom,_#6366f1_0,_transparent_55%)] opacity-40"
      />
      <AdminHeader variant="management" />
      <div className="relative z-10 mx-auto flex w-full max-w-[1440px] flex-1 flex-col gap-5 px-4 py-6 sm:px-6 lg:flex-row lg:items-start lg:gap-6 lg:px-8">
        <SalesSidebarClient />
        <div className="w-full min-w-0 flex-1">{children}</div>
      </div>
    </div>
  );
}
