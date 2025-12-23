"use client";

import { FormEvent, useRef, useState } from "react";
import { useRouter } from "next/navigation";

type Role = "Admin" | "Member";

type LoginResponse = {
  id: number;
  email: string;
  name: string;
  role: Role;
  token: string;
};

const DEMO_ACCOUNTS = {
  admin: {
    email: "admin@example.com",
    password: "Admin1234!",
  },
  member: {
    email: "member@example.com",
    password: "Member1234!",
  },
} as const;

// 表示制御（Herokuなどで NEXT_PUBLIC_SHOW_DEMO_LOGIN=true にする）
const SHOW_DEMO_LOGIN = process.env.NEXT_PUBLIC_SHOW_DEMO_LOGIN === "true";

export default function LoginPage() {
  const router = useRouter();

  const formRef = useRef<HTMLFormElement | null>(null);

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError("");
    setIsSubmitting(true);

    try {
      const res = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/auth/login`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email, password }),
      });

      const data = await res.json().catch(() => ({} as any));

      if (!res.ok) {
        if ((data as any)?.message) {
          setError((data as any).message);
          return;
        }

        if (res.status === 401) {
          setError("メールアドレスまたはパスワードが正しくありません。");
        } else if (res.status === 403) {
          setError("このアカウントは利用できません。");
        } else if (res.status === 400) {
          setError("不正なリクエストです。");
        } else {
          setError("ログインに失敗しました。時間をおいて再度お試しください。");
        }
        return;
      }

      const loginData = data as LoginResponse;

      if (typeof window !== "undefined") {
        localStorage.setItem("currentUser", JSON.stringify(loginData));
        document.cookie = "isLoggedIn=true; path=/;";
        document.cookie = `role=${loginData.role}; path=/;`;
        document.cookie = `email=${encodeURIComponent(loginData.email)}; path=/; samesite=lax;`;
        document.cookie = `token=${loginData.token}; path=/; samesite=lax;`;
      }

      router.push(loginData.role === "Admin" ? "/dashboards/admin" : "/dashboards/member");
    } catch (err) {
      console.error(err);
      setError("通信エラーが発生しました。ネットワーク環境を確認してから再度お試しください。");
    } finally {
      setIsSubmitting(false);
    }
  };

  const demoLogin = (role: "admin" | "member") => {
    if (isSubmitting) return;

    const demo = DEMO_ACCOUNTS[role];

    setError("");
    setEmail(demo.email);
    setPassword(demo.password);

    // state反映後に submit（refで対象フォームを確実に指定）
    setTimeout(() => {
      formRef.current?.requestSubmit();
    }, 0);
  };

  return (
    <div className="min-h-screen bg-slate-950 text-slate-50">
      <div className="pointer-events-none fixed inset-0 bg-[radial-gradient(circle_at_top,_#0ea5e9_0,_transparent_55%),radial-gradient(circle_at_bottom,_#6366f1_0,_transparent_55%)] opacity-70" />

      <div className="relative z-10 flex min-h-screen items-center justify-center px-4 py-8">
        <div className="w-full max-w-3xl grid gap-10 rounded-3xl border border-slate-800 bg-slate-900/80 p-8 shadow-2xl backdrop-blur">
          <div className="hidden flex-col justify-between md:flex">
            <div>
              <span className="inline-flex items-center rounded-full bg-slate-800 px-3 py-1 text-xs font-medium text-sky-300">
                Invoice Dashboard Lite
              </span>
              <h1 className="mt-4 text-2xl font-semibold tracking-tight">共通ログイン</h1>
              <p className="mt-2 text-sm text-slate-300 leading-relaxed">
                請求書・入金ステータスダッシュボード（Lite版）へログインします。
                管理者ロールと一般会員ロールで、ダッシュボードの表示内容が切り替わります。
              </p>
            </div>
          </div>

          <div className="rounded-2xl bg-slate-900/80 p-6 shadow-lg">
            <div className="mb-6 md:hidden">
              <span className="inline-flex items-center rounded-full bg-slate-800 px-3 py-1 text-xs font-medium text-sky-300">
                Invoice Dashboard Lite
              </span>
              <h1 className="mt-3 text-xl font-semibold tracking-tight">ログイン</h1>
              <p className="mt-1 text-xs text-slate-300">
                メールアドレスとパスワードを入力してサインインしてください。
              </p>
            </div>

            <form ref={formRef} onSubmit={handleSubmit} className="space-y-4">
              <div className="space-y-1">
                <label htmlFor="email" className="text-xs font-medium text-slate-200">
                  メールアドレス
                </label>
                <input
                  id="email"
                  type="email"
                  autoComplete="email"
                  className="w-full rounded-xl border border-slate-700 bg-slate-900/60 px-3 py-2.5 text-sm outline-none ring-0 transition focus:border-sky-500 focus:ring-2 focus:ring-sky-500/60"
                  placeholder="example@example.com"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  required
                />
              </div>

              <div className="space-y-1">
                <label htmlFor="password" className="text-xs font-medium text-slate-200">
                  パスワード
                </label>
                <div className="relative">
                  <input
                    id="password"
                    type={showPassword ? "text" : "password"}
                    autoComplete="current-password"
                    className="w-full rounded-xl border border-slate-700 bg-slate-900/60 px-3 py-2.5 pr-20 text-sm outline-none ring-0 transition focus:border-sky-500 focus:ring-2 focus:ring-sky-500/60"
                    placeholder="••••••••"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    required
                  />
                  <button
                    type="button"
                    onClick={() => setShowPassword((prev) => !prev)}
                    className="absolute inset-y-0 right-2 my-1 inline-flex items-center rounded-lg px-2 text-xs font-medium text-slate-200 hover:bg-slate-800/80"
                  >
                    {showPassword ? "非表示" : "表示"}
                  </button>
                </div>
              </div>

              {error && (
                <div className="rounded-xl border border-red-500/40 bg-red-500/10 px-3 py-2 text-xs text-red-200">
                  {error}
                </div>
              )}

              <button
                type="submit"
                disabled={isSubmitting}
                className="mt-1 inline-flex w-full items-center justify-center rounded-xl bg-gradient-to-r from-sky-500 to-indigo-500 px-4 py-2.5 text-sm font-semibold text-white shadow-lg shadow-sky-500/30 transition hover:brightness-110 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {isSubmitting ? "ログイン中..." : "ログイン"}
              </button>
            </form>

            {SHOW_DEMO_LOGIN && (
              <div className="mt-4 space-y-2">
                <p className="text-center text-[11px] text-slate-400">
                  デモログイン（パスワードは画面に表示しません）
                </p>

                <button
                  type="button"
                  disabled={isSubmitting}
                  onClick={() => demoLogin("admin")}
                  className="inline-flex w-full items-center justify-center rounded-xl border border-slate-700 bg-slate-900/60 px-4 py-2.5 text-xs font-semibold text-slate-100 transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  管理者デモでログイン
                </button>

                <button
                  type="button"
                  disabled={isSubmitting}
                  onClick={() => demoLogin("member")}
                  className="inline-flex w-full items-center justify-center rounded-xl border border-slate-700 bg-slate-900/60 px-4 py-2.5 text-xs font-semibold text-slate-100 transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  一般会員デモでログイン
                </button>
              </div>
            )}

            <div className="mt-4 flex flex-col gap-2 text-xs text-slate-300">
              <p className="text-center">
                アカウントをお持ちでない方は、こちらから新規登録できます。
              </p>
              <button
                type="button"
                onClick={() => router.push("/auth/register")}
                className="inline-flex w-full items-center justify-center rounded-xl border border-slate-700 bg-slate-900/60 px-4 py-2.5 text-xs font-semibold text-slate-100 transition hover:bg-slate-800"
              >
                新規会員登録
              </button>
            </div>

            <div className="mt-3 text-center">
              <button
                type="button"
                onClick={() => router.push("/auth/forgot-password")}
                className="text-xs text-sky-300 hover:text-sky-400 transition"
              >
                パスワードをお忘れの方はこちら
              </button>
            </div>

            {/* ★モバイルの「デモアカウント（PW表示）」は削除（露出NG） */}
          </div>
        </div>
      </div>
    </div>
  );
}



