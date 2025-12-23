"use client";

import { useState, FormEvent } from "react";

export default function ForgotPasswordPage() {
  const [email, setEmail] = useState("");
  const [sent, setSent] = useState(false);
  const [error, setError] = useState("");

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError("");

    try {
      const res = await fetch(
        `${process.env.NEXT_PUBLIC_API_BASE_URL}/auth/forgot-password`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ email }),
        }
      );

      if (!res.ok) {
        setError("メール送信に失敗しました。メールアドレスをご確認ください。");
        return;
      }

      setSent(true);
    } catch {
      setError("通信エラーが発生しました。");
    }
  };

  return (
    <div className="min-h-screen bg-slate-950 text-slate-50">
      <div className="pointer-events-none fixed inset-0 bg-[radial-gradient(circle_at_top,#0ea5e9_0,transparent_55%),radial-gradient(circle_at_bottom,#6366f1_0,transparent_55%)] opacity-70" />

      <div className="relative z-10 flex items-center justify-center min-h-screen px-4 py-8">
        <div className="w-full max-w-md rounded-3xl border border-slate-800 bg-slate-900/80 p-6 shadow-xl backdrop-blur">
          <h1 className="text-xl font-semibold">パスワード再設定</h1>
          <p className="text-xs text-slate-300 mt-1">
            登録したメールアドレス宛に再設定リンクを送信します。
          </p>

          {sent ? (
            <p className="mt-6 text-sky-300 text-sm">
              再設定リンクをメールに送信しました。
              <br />
              メールをご確認ください。
            </p>
          ) : (
            <form onSubmit={handleSubmit} className="mt-6 space-y-4">
              <div>
                <label className="text-xs">メールアドレス</label>
                <input
                  type="email"
                  className="w-full mt-1 rounded-lg bg-slate-900/60 border border-slate-700 px-3 py-2 text-sm focus:ring-2 focus:ring-sky-500"
                  placeholder="example@example.com"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  required
                />
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
                再設定メールを送信
              </button>
            </form>
          )}
        </div>
      </div>
    </div>
  );
}
