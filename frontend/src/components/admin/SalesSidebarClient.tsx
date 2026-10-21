"use client";

import { usePathname } from "next/navigation";
import AdminSidebar from "@/components/admin/AdminSidebar";

/** 既存のAdminSidebarを再利用し、現在の売上メニューだけを選択します。 */
export default function SalesSidebarClient() {
  const pathname = usePathname() ?? "";
  const activeSection = pathname === "/sales/by-member" || pathname.startsWith("/sales/by-member/")
    ? "sales-by-member"
    : "sales";

  return <AdminSidebar activeSection={activeSection} />;
}
