"use client";

import { FormEvent, useState } from "react";

export default function RegisterPage() {
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [postalCode, setPostalCode] = useState("");
  const [address, setAddress] = useState("");
  const [phone, setPhone] = useState("");

  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const apiBase = process.env.NEXT_PUBLIC_API_BASE_URL;

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setSuccess(null);
    setLoading(true);

    try {
      const res = await fetch(`${apiBase}/auth/register`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          name,
          email,
          password,
          postalCode: postalCode || null,
          address: address || null,
          phone: phone || null,
        }),
      });

      const data = await res.json().catch(() => ({}));

      if (!res.ok) {
        setError(data.message ?? "登録に失敗しました。");
        return;
      }

      setSuccess("登録が完了しました。ログイン画面からサインインしてください。");

      // フォームリセット
      setName("");
      setEmail("");
      setPostalCode("");
      setAddress("");
      setPhone("");
      setPassword("");
    } catch (err) {
      console.error(err);
      setError("通信エラーが発生しました。時間をおいて再度お試しください。");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="min-h-screen bg-slate-950 text-slate-50">
      {/* 背景グラデーション */}
      <div className="pointer-events-none fixed inset-0 bg-[radial-gradient(circle_at_top,_#0ea5e9_0,_transparent_55%),radial-gradient(circle_at_bottom,_#6366f1_0,_transparent_55%)] opacity-70" />
      <div className="relative z-10 flex min-h-screen items-center justify-center px-4 py-8">
        <div className="w-full max-w-3xl grid gap-10 rounded-3xl border border-slate-800 bg-slate-900/80 p-8 shadow-2xl backdrop-blur">
          {/* 左側（説明） */}
          <div className="hidden flex-col justify-between md:flex">
            <div>
              <span className="inline-flex items-center rounded-full bg-slate-800 px-3 py-1 text-xs font-medium text-sky-300">
                Invoice Dashboard Lite
              </span>
              <h1 className="mt-4 text-2xl font-semibold tracking-tight">
                新規会員登録
              </h1>
              <p className="mt-2 text-sm text-slate-300 leading-relaxed">
                請求書・入金ステータスダッシュボードのデモ用アカウントを登録します。
                氏名とメールアドレス、パスワードだけでも登録可能です。
              </p>
            </div>

            <ul className="mt-8 space-y-3 text-sm text-slate-300">
              <li className="flex gap-2">
                <span className="mt-1 h-1.5 w-1.5 rounded-full bg-sky-400" />
                <span>登録後は、請求書一覧・入金状況を閲覧できるようになります。</span>
              </li>
              <li className="flex gap-2">
                <span className="mt-1 h-1.5 w-1.5 rounded-full bg-sky-400" />
                <span>メールアドレスはログインIDとして使用します。</span>
              </li>
              <li className="flex gap-2">
                <span className="mt-1 h-1.5 w-1.5 rounded-full bg-sky-400" />
                <span>ダミー情報でも問題ありません（ポートフォリオ用）。</span>
              </li>
            </ul>
          </div>

          {/* 右側（フォーム） */}
          <div className="rounded-2xl bg-slate-900/80 p-6 shadow-lg">
            <div className="mb-6 md:hidden">
              <h1 className="text-xl font-semibold tracking-tight">
                新規会員登録
              </h1>
              <p className="mt-1 text-xs text-slate-300">
                必要事項を入力してアカウントを作成してください。
              </p>
            </div>

            {error && (
              <div className="mb-4 rounded-xl border border-red-500/40 bg-red-500/10 px-3 py-2 text-xs text-red-200">
                {error}
              </div>
            )}

            {success && (
              <div className="mb-4 rounded-xl border border-emerald-500/40 bg-emerald-500/10 px-3 py-2 text-xs text-emerald-200">
                {success}
              </div>
            )}

            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="space-y-1">
                <label className="text-xs font-medium text-slate-200">
                  氏名 <span className="text-red-400">*</span>
                </label>
                <input
                  className="w-full rounded-xl border border-slate-700 bg-slate-900/60 px-3 py-2 text-sm outline-none ring-0 transition focus:border-sky-500 focus:ring-2 focus:ring-sky-500/60"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  required
                />
              </div>

              <div className="space-y-1">
                <label className="text-xs font-medium text-slate-200">
                  メールアドレス <span className="text-red-400">*</span>
                </label>
                <input
                  type="email"
                  className="w-full rounded-xl border border-slate-700 bg-slate-900/60 px-3 py-2 text-sm outline-none ring-0 transition focus:border-sky-500 focus:ring-2 focus:ring-sky-500/60"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  required
                />
              </div>

              <div className="space-y-1">
                <label className="text-xs font-medium text-slate-200">
                  パスワード <span className="text-red-400">*</span>
                </label>
                <div className="relative">
                  <input
                    type={showPassword ? "text" : "password"}
                    className="w-full rounded-xl border border-slate-700 bg-slate-900/60 px-3 py-2 pr-20 text-sm outline-none ring-0 transition focus:border-sky-500 focus:ring-2 focus:ring-sky-500/60"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    required
                    minLength={8}
                    placeholder="8文字以上を推奨"
                  />
                  <button
                    type="button"
                    onClick={() => setShowPassword((prev) => !prev)}
                    className="absolute inset-y-0 right-2 my-1 inline-flex items-center rounded-lg px-2 text-xs font-medium text-slate-200 hover:bg-slate-800/80"
                  >
                    {showPassword ? "非表示" : "表示"}
                  </button>
                </div>
                <p className="text-[10px] text-slate-400">
                  英数字・記号を組み合わせた8文字以上のパスワードを推奨します。
                </p>
              </div>

              <div className="grid gap-3 md:grid-cols-2">
                <div className="space-y-1">
                  <label className="text-xs font-medium text-slate-200">
                    郵便番号
                  </label>
                  <input
                    className="w-full rounded-xl border border-slate-700 bg-slate-900/60 px-3 py-2 text-sm outline-none ring-0 transition focus:border-sky-500 focus:ring-2 focus:ring-sky-500/60"
                    value={postalCode}
                    onChange={(e) => setPostalCode(e.target.value)}
                    placeholder="例）1000001"
                  />
                </div>
                <div className="space-y-1">
                  <label className="text-xs font-medium text-slate-200">
                    電話番号
                  </label>
                  <input
                    className="w-full rounded-xl border border-slate-700 bg-slate-900/60 px-3 py-2 text-sm outline-none ring-0 transition focus:border-sky-500 focus:ring-2 focus:ring-sky-500/60"
                    value={phone}
                    onChange={(e) => setPhone(e.target.value)}
                    placeholder="例）090-1234-5678"
                  />
                </div>
              </div>

              <div className="space-y-1">
                <label className="text-xs font-medium text-slate-200">
                  住所
                </label>
                <input
                  className="w-full rounded-xl border border-slate-700 bg-slate-900/60 px-3 py-2 text-sm outline-none ring-0 transition focus:border-sky-500 focus:ring-2 focus:ring-sky-500/60"
                  value={address}
                  onChange={(e) => setAddress(e.target.value)}
                  placeholder="例）東京都千代田区1-1-1"
                />
              </div>

              <button
                type="submit"
                disabled={loading}
                className="mt-2 inline-flex w-full items-center justify-center rounded-xl bg-gradient-to-r from-sky-500 to-indigo-500 px-4 py-2.5 text-sm font-semibold text-white shadow-lg shadow-sky-500/30 transition hover:brightness-110 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {loading ? "登録中..." : "アカウントを作成する"}
              </button>
            </form>

            <p className="mt-4 text-center text-xs text-slate-400">
              すでにアカウントをお持ちですか？{" "}
              <a
                href="/auth/login"
                className="font-medium text-sky-300 underline-offset-4 hover:underline"
              >
                ログインはこちら
              </a>
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
