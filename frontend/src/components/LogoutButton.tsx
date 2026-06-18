// src/components/LogoutButton.tsx
"use client";

import { useState, useEffect } from "react";
import { createPortal } from "react-dom";
import { useRouter } from "next/navigation";

export default function LogoutButton() {
  const router = useRouter();
  const [showConfirm, setShowConfirm] = useState(false);
  const [loading, setLoading] = useState(false);
  const [mounted, setMounted] = useState(false);

  useEffect(() => setMounted(true), []);

  // モーダル表示中は背面スクロールを止める
  useEffect(() => {
    if (showConfirm) {
      document.body.style.overflow = "hidden";
      return () => {
        document.body.style.overflow = "";
      };
    }
  }, [showConfirm]);

  const handleLogout = async () => {
    setLoading(true);
    if (typeof window !== "undefined") {
      localStorage.removeItem("currentUser");
      document.cookie =
        "isLoggedIn=; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT";
      document.cookie =
        "role=; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT";
    }
    await fetch("/api/auth/logout", { method: "POST" });
    router.push("/auth/login");
  };

  const modal = (
    <div
      className="fixed inset-0 z-[1000] flex items-center justify-center bg-black/60 backdrop-blur-sm"
      onClick={() => !loading && setShowConfirm(false)}
    >
      <div
        className="bg-slate-800 border border-slate-700 rounded-lg shadow-2xl p-6 w-80 max-w-[90%]"
        onClick={(e) => e.stopPropagation()}
      >
        <h2 className="text-lg font-semibold mb-2 text-slate-100">
          ログアウト確認
        </h2>
        <p className="text-sm text-slate-400 mb-6">本当にログアウトしますか？</p>
        <div className="flex justify-end gap-2">
          <button
            onClick={() => setShowConfirm(false)}
            disabled={loading}
            className="text-sm px-3 py-1 rounded-md border border-slate-600 text-slate-200 hover:bg-slate-700 transition disabled:opacity-50"
          >
            キャンセル
          </button>
          <button
            onClick={handleLogout}
            disabled={loading}
            className="text-sm px-3 py-1 rounded-md bg-red-600 text-white hover:bg-red-500 transition disabled:opacity-50"
          >
            {loading ? "処理中..." : "ログアウト"}
          </button>
        </div>
      </div>
    </div>
  );

  return (
    <>
      <button
        onClick={() => setShowConfirm(true)}
        className="text-sm text-red-400 hover:text-red-300 border border-red-500/40 px-3 py-1 rounded-md hover:bg-red-500/10 transition"
      >
        ログアウト
      </button>

      {mounted && showConfirm && createPortal(modal, document.body)}
    </>
  );
}