// src/app/dashboards/page.tsx
import { cookies } from "next/headers";
import { redirect } from "next/navigation";

async function requireAdmin() {
  const cookieStore = cookies();

  const res = await fetch(
    `${process.env.NEXT_PUBLIC_API_BASE_URL}/auth/me`,
    {
      headers: {
        Cookie: cookieStore.toString(),
      },
      cache: "no-store",
      credentials: "include",
    }
  );

  // ★ ここが重要：ログイン画面には行かせない
  if (!res.ok) {
    redirect("/forbidden");
  }

  const me = await res.json();

  if (me.role !== "ADMIN") {
    redirect("/forbidden");
  }
}

export default async function DashboardsPage() {
  await requireAdmin();

  return (
    <div className="min-h-screen bg-slate-950 text-slate-50">
      {/* 管理者ダッシュボード */}
    </div>
  );
}

