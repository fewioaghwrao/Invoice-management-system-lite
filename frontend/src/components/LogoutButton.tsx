// src/components/LogoutButton.tsx
"use client";

import { useRouter } from "next/navigation";

export default function LogoutButton() {
  const router = useRouter();

  const handleLogout = async () => {
    if (typeof window !== "undefined") {
      localStorage.removeItem("currentUser");

      document.cookie =
        "isLoggedIn=; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT";
      document.cookie =
        "role=; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT";
    }

    // ★ cookie削除はサーバ側(route.ts)で一元化
    await fetch("/api/auth/logout", { method: "POST" });
    router.push("/auth/login");
  };

  return (
    <button
      onClick={handleLogout}
      className="text-sm text-red-600 hover:text-red-700 border border-red-200 px-3 py-1 rounded-md hover:bg-red-50 transition"
    >
      ログアウト
    </button>
  );
}

