import type { ReactNode } from "react";
import AdminHeader from "@/components/admin/AdminHeader";
import AdminSidebar from "@/components/admin/AdminSidebar";

/**
 * 請求書単位の督促画面に適用する共通管理者レイアウト。
 * /collections/[invoiceId] は請求書から遷移するため、サイドバーでは「請求書一覧」を選択します。
 */
export default function CollectionsLayout({ children }: { children: ReactNode }) {
  return (
    <div className="relative flex min-h-screen flex-col bg-slate-950 text-slate-50">
      <div
        aria-hidden="true"
        className="pointer-events-none fixed inset-0 bg-[radial-gradient(circle_at_top,_#0ea5e9_0,_transparent_55%),radial-gradient(circle_at_bottom,_#6366f1_0,_transparent_55%)] opacity-40"
      />

      <AdminHeader variant="management" />

      <div className="relative z-10 mx-auto flex w-full max-w-[1440px] flex-1 flex-col gap-5 px-4 py-6 sm:px-6 lg:flex-row lg:items-start lg:gap-6 lg:px-8">
        <AdminSidebar activeSection="invoices" />
        <div className="w-full min-w-0 flex-1">{children}</div>
      </div>
    </div>
  );
}
