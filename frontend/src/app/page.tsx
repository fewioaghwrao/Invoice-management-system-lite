// src/app/page.tsx
import { cookies } from "next/headers";
import { redirect } from "next/navigation";

export default async function HomePage() {
  const c = await cookies(); // ★ここがポイント（Promise対応）
  const isLoggedIn = c.get("isLoggedIn")?.value === "true";
  const role = c.get("role")?.value;

  if (!isLoggedIn) {
    redirect("/auth/login");
  }

  if (role === "Admin") {
    redirect("/dashboards/admin");
  }

  redirect("/dashboards/member");
}

