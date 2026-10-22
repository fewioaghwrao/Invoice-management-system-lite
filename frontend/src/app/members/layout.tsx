import type { ReactNode } from "react";
import AdminHeader from "@/components/admin/AdminHeader";
import AdminSidebar from "@/components/admin/AdminSidebar";

/**
 * 会員一覧 / 会員詳細の共通レイアウト。
 * 画面上部のヘッダーと左サイドバーは他の管理者画面と共通化します。
 */
export default function MembersLayout({ children }: { children: ReactNode }) {
  return (
    <div className="relative flex min-h-screen flex-col bg-slate-950 text-slate-50">
      <div
        aria-hidden="true"
        className="pointer-events-none fixed inset-0 bg-[radial-gradient(circle_at_top,_#0ea5e9_0,_transparent_55%),radial-gradient(circle_at_bottom,_#6366f1_0,_transparent_55%)] opacity-40"
      />

      <AdminHeader variant="management" />

      <div className="relative z-10 mx-auto flex w-full max-w-[1440px] flex-1 flex-col gap-5 px-4 py-6 sm:px-6 lg:flex-row lg:items-start lg:gap-6 lg:px-8">
        <AdminSidebar activeSection="members" />
        <div className="w-full min-w-0 flex-1">{children}</div>
      </div>
    </div>
  );
}
