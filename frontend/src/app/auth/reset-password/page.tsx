"use client";

import { useState, FormEvent } from "react";
import { useSearchParams, useRouter } from "next/navigation";

export default function ResetPasswordPage() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const [showPassword, setShowPassword] = useState(false);

  const token = searchParams.get("token");
  const [password, setPassword] = useState("");
  const [done, setDone] = useState(false);
  const [error, setError] = useState("");

  if (!token) {
    return (
      <div className="min-h-screen flex items-center justify-center text-red-300">
        不正なリクエストです（トークンがありません）
      </div>
    );
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError("");

    try {
      const res = await fetch(
        `${process.env.NEXT_PUBLIC_API_BASE_URL}/auth/reset-password`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ token, newPassword: password }),
        }
      );

      if (!res.ok) {
        setError("パスワードの再設定に失敗しました。");
        return;
      }

      setDone(true);
      setTimeout(() => router.push("/login"), 2500);
    } catch {
      setError("通信エラーが発生しました。");
    }
  };

  return (
    <div className="min-h-screen bg-slate-950 text-slate-50">
      <div className="pointer-events-none fixed inset-0 bg-[radial-gradient(circle_at_top,#0ea5e9_0,transparent_55%),radial-gradient(circle_at_bottom,#6366f1_0,transparent_55%)] opacity-70" />

      <div className="relative flex items-center justify-center min-h-screen px-4 py-8">
        <div className="w-full max-w-md rounded-3xl border border-slate-800 bg-slate-900/80 p-6 shadow-xl backdrop-blur">
          <h1 className="text-xl font-semibold">新しいパスワードを設定</h1>

          {done ? (
            <p className="mt-6 text-sky-300 text-sm">
              パスワードを更新しました。ログイン画面へ移動します…
            </p>
          ) : (
            <form onSubmit={handleSubmit} className="mt-6 space-y-4">
<div>
  <label className="text-xs">新しいパスワード</label>

  <div className="relative">
    <input
      type={showPassword ? "text" : "password"}
      className="w-full mt-1 rounded-lg bg-slate-900/60 border border-slate-700 px-3 py-2 pr-20 text-sm focus:ring-2 focus:ring-sky-500"
      value={password}
      onChange={(e) => setPassword(e.target.value)}
      required
    />

    <button
      type="button"
      onClick={() => setShowPassword((prev) => !prev)}
      className="absolute inset-y-0 right-2 my-1 inline-flex items-center rounded-lg px-2 text-xs text-slate-200 hover:bg-slate-800/80"
    >
      {showPassword ? "非表示" : "表示"}
    </button>
  </div>
</div>

              {error && (
                <div className="text-xs text-red-300 bg-red-500/10 border border-red-500/40 px-3 py-2 rounded-lg">
                  {error}
                </div>
              )}

              <button
                type="submit"
                className="w-full rounded-xl bg-gradient-to-r from-sky-500 to-indigo-500 py-2 text-sm font-semibold shadow-lg hover:brightness-110"
              >
                パスワードを更新
              </button>
            </form>
          )}
        </div>
      </div>
    </div>
  );
}
