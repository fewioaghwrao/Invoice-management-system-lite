"use client";

import Link from "next/link";
import { useEffect } from "react";

export default function GlobalError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  // ここで Sentry 等に送る想定（ポートフォリオで説明できる）
  useEffect(() => {
    // console.error は最低限（本番は監視に送る）
    console.error("[GlobalError]", error);
  }, [error]);

  return (
    <div className="min-h-screen bg-slate-950 text-slate-50">
      {/* 背景 */}
      <div className="pointer-events-none fixed inset-0 bg-[radial-gradient(circle_at_top,_#0ea5e9_0,_transparent_55%),radial-gradient(circle_at_bottom,_#6366f1_0,_transparent_55%)] opacity-60" />

      <main className="relative z-10 mx-auto flex max-w-2xl flex-col items-center px-4 py-16 text-center">
        <div className="rounded-2xl border border-slate-800 bg-slate-900/80 p-6 shadow-md">
          <p className="text-[11px] text-slate-400">Error</p>
          <h1 className="mt-2 text-xl font-semibold">予期しないエラーが発生しました</h1>
          <p className="mt-2 text-xs text-slate-400 leading-relaxed">
            画面の読み込み中に問題が発生しました。<br />
            まずは再試行し、改善しない場合はトップへ戻ってください。
          </p>

          {/* 実務っぽい：デバッグ情報（本番は非表示にする想定） */}
          <div className="mt-4 rounded-xl border border-slate-800 bg-slate-950/40 p-4 text-left">
            <p className="text-[11px] text-slate-400">診断情報（開発用）</p>
            <p className="mt-2 text-xs text-slate-200 break-all">
              {error?.message || "Unknown error"}
            </p>
            {error?.digest && (
              <p className="mt-2 text-[11px] text-slate-400 break-all">
                digest: {error.digest}
              </p>
            )}
          </div>

          <div className="mt-5 grid gap-2 sm:grid-cols-2">
            <button
              onClick={() => reset()}
              className="inline-flex items-center justify-center rounded-lg bg-sky-600 px-4 py-2 text-xs font-medium text-slate-50 shadow-sm shadow-sky-500/30 hover:bg-sky-500"
            >
              再試行（reset）
            </button>

            <Link
              href="/dashboards/admin"
              className="inline-flex items-center justify-center rounded-lg border border-slate-700 bg-slate-900 px-4 py-2 text-xs font-medium text-slate-200 hover:bg-slate-800"
            >
              管理トップへ
            </Link>

            <Link
              href="/invoices"
              className="inline-flex items-center justify-center rounded-lg border border-slate-700 bg-slate-900 px-4 py-2 text-xs font-medium text-slate-200 hover:bg-slate-800 sm:col-span-2"
            >
              請求書一覧へ
            </Link>
          </div>

          <p className="mt-4 text-[11px] text-slate-500">
            ※本番環境では詳細メッセージは表示せず、監視基盤へ送信する想定です。
          </p>
        </div>
      </main>
    </div>
  );
}
