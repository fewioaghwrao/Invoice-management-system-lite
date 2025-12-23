"use client";

import { useEffect, useState } from "react";
import { useSearchParams, useRouter } from "next/navigation";

type Status = "idle" | "loading" | "success" | "error" | "no_token";

export default function VerifyEmailPage() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const [status, setStatus] = useState<Status>("idle");
  const [message, setMessage] = useState<string | null>(null);

  const apiBase = process.env.NEXT_PUBLIC_API_BASE_URL;

  useEffect(() => {
    const token = searchParams.get("token");

    if (!token) {
      setStatus("no_token");
      setMessage("トークンが指定されていません。メール内のリンクからアクセスしてください。");
      return;
    }

    const verify = async () => {
      setStatus("loading");
      setMessage(null);

      try {
        const res = await fetch(`${apiBase}/auth/verify-email`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ token }),
        });

        const data = await res.json().catch(() => ({}));

        if (!res.ok) {
          setStatus("error");
          setMessage(
            data.message ??
              "トークンが無効、または有効期限が切れています。お手数ですが再度登録をお試しください。"
          );
          return;
        }

        setStatus("success");
        setMessage("メールアドレスの確認が完了しました。ログイン画面からサインインできます。");
      } catch (error) {
        console.error(error);
        setStatus("error");
        setMessage("通信エラーが発生しました。時間をおいて再度アクセスしてください。");
      }
    };

    verify();
  }, [searchParams, apiBase]);

  const isLoading = status === "loading";

  return (
    <div className="min-h-screen bg-slate-950 text-slate-50">
      {/* 背景グラデーション */}
      <div className="pointer-events-none fixed inset-0 bg-[radial-gradient(circle_at_top,_#0ea5e9_0,_transparent_55%),radial-gradient(circle_at_bottom,_#6366f1_0,_transparent_55%)] opacity-70" />
      <div className="relative z-10 flex min-h-screen items-center justify-center px-4 py-8">
        <div className="w-full max-w-md rounded-3xl border border-slate-800 bg-slate-900/80 p-6 shadow-2xl backdrop-blur">
          <h1 className="text-xl font-semibold tracking-tight">
            メールアドレス確認
          </h1>
          <p className="mt-1 text-xs text-slate-300">
            登録時に送信されたメール内のリンクからアクセスしたことを確認しています。
          </p>

          <div className="mt-6">
            {isLoading && (
              <div className="rounded-xl border border-sky-500/40 bg-sky-500/10 px-3 py-3 text-xs text-sky-100">
                メールアドレスを確認しています…
              </div>
            )}

            {status === "success" && (
              <div className="rounded-xl border border-emerald-500/40 bg-emerald-500/10 px-3 py-3 text-xs text-emerald-100">
                {message}
              </div>
            )}

            {(status === "error" || status === "no_token") && (
              <div className="rounded-xl border border-red-500/40 bg-red-500/10 px-3 py-3 text-xs text-red-100">
                {message}
              </div>
            )}
          </div>

          <div className="mt-6 flex flex-col gap-2 text-xs">
            <button
              type="button"
              onClick={() => router.push("/login")}
              className="inline-flex w-full items-center justify-center rounded-xl bg-gradient-to-r from-sky-500 to-indigo-500 px-4 py-2.5 font-semibold text-white shadow-lg shadow-sky-500/30 transition hover:brightness-110"
            >
              ログイン画面へ戻る
            </button>

            <button
              type="button"
              onClick={() => router.push("/auth/register")}
              className="inline-flex w-full items-center justify-center rounded-xl border border-slate-700 bg-slate-900/60 px-4 py-2.5 font-semibold text-slate-100 transition hover:bg-slate-800"
            >
              新規登録ページへ
            </button>
          </div>

          <p className="mt-4 text-center text-[10px] text-slate-400">
            この画面をブックマークして直接アクセスした場合、トークンが無効となる場合があります。
            正常に動作しない場合は、改めて会員登録を行ってください。
          </p>
        </div>
      </div>
    </div>
  );
}
