// src/app/dashboards/page.tsx
import Link from "next/link";

export default async function DashboardsPage() {
  return (
    <div className="min-h-screen bg-slate-950 text-slate-50">
      {/* 背景グラデーション */}
      <div className="pointer-events-none fixed inset-0 bg-[radial-gradient(circle_at_top,_#0ea5e9_0,_transparent_55%),radial-gradient(circle_at_bottom,_#6366f1_0,_transparent_55%)] opacity-60" />

      {/* 上部ヘッダーエリア */}
      <header className="relative z-10 border-b border-slate-800 bg-slate-950/80 backdrop-blur">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
          <div>
            <h1 className="text-lg font-semibold text-slate-50">
              請求・入金ステータスダッシュボード
            </h1>
            <p className="mt-1 text-xs text-slate-400">
              管理者用メインメニュー。請求書と会員情報の管理画面へアクセスできます。
            </p>
          </div>

          {/* 将来的に「管理者名」「ログアウト」などを置く想定スペース */}
          <div className="hidden items-center gap-3 text-[11px] text-slate-300 md:flex">
            <span className="rounded-full bg-slate-900/80 px-3 py-1 border border-slate-700">
              ロール: 管理者
            </span>
          </div>
        </div>
      </header>

      {/* コンテンツエリア */}
      <main className="relative z-10 mx-auto max-w-6xl px-6 py-8 space-y-8">
        {/* サマリーカード群 */}
        <section className="grid gap-4 md:grid-cols-3">
          {/* 管理コンソール 概要 */}
          <div className="rounded-2xl border border-sky-500/40 bg-slate-900/80 p-4 shadow-lg shadow-sky-500/20">
            <p className="text-[11px] font-medium text-sky-300">
              本日のサマリー
            </p>
            <p className="mt-2 text-lg font-semibold text-slate-50">
              管理コンソール
            </p>
            <p className="mt-2 text-[11px] leading-relaxed text-slate-300">
              請求書・会員の状態を一元管理するためのコンソールです。
              将来的に「未入金件数」や「今月の請求額」などの集計と連動させられます。
            </p>
            <div className="mt-3 text-[11px] text-slate-400">
              ※ Lite版ではメニュー中心の構成です。
            </div>
          </div>

          {/* 請求書 概要 */}
          <div className="flex flex-col justify-between rounded-2xl border border-slate-800 bg-slate-900/80 p-4 shadow-md">
            <div>
              <p className="text-[11px] font-medium text-slate-300">
                請求書
              </p>
              <p className="mt-2 text-sm text-slate-100">
                請求書一覧から、入金ステータスや支払期限を確認できます。
                ステータス別の色分けバッジやページネーションを備えています。
              </p>
            </div>
            <div className="mt-3 text-[11px] text-slate-500">
              ※ API 集計と組み合わせることで実運用に近い画面構成になります。
            </div>
          </div>

          {/* 会員 概要 */}
          <div className="flex flex-col justify-between rounded-2xl border border-slate-800 bg-slate-900/80 p-4 shadow-md">
            <div>
              <p className="text-[11px] font-medium text-slate-300">
                会員
              </p>
              <p className="mt-2 text-sm text-slate-100">
                会員一覧から、顧客情報・ロール・有効 / 無効状態を管理します。
                将来的に退会処理やロール変更機能と連携させる想定です。
              </p>
            </div>
            <div className="mt-3 text-[11px] text-slate-500">
              ※ ここも将来的にグラフや数値カードに差し替え可能です。
            </div>
          </div>
        </section>

        {/* メインカード：請求書一覧 / 会員一覧 */}
        <section className="grid gap-6 md:grid-cols-2">
          {/* 請求書一覧カード */}
          <Link
            href="/invoices"
            className="group relative overflow-hidden rounded-2xl border border-slate-800 bg-slate-900/90 p-6 shadow-md transition hover:-translate-y-1 hover:border-sky-500/60 hover:shadow-sky-500/20"
          >
            {/* 背景のうっすらした装飾 */}
            <div className="pointer-events-none absolute -right-10 -top-10 h-32 w-32 rounded-full bg-sky-500/10 blur-3xl" />

            <div className="relative flex items-start justify-between gap-4">
              <div>
                <h2 className="flex items-center gap-2 text-lg font-semibold text-slate-50">
                  <span className="inline-flex h-8 w-8 items-center justify-center rounded-full bg-sky-500/10 text-sky-300 text-lg">
                    📄
                  </span>
                  請求書一覧
                </h2>
                <p className="mt-2 text-sm text-slate-300">
                  請求番号・会員名・ステータス・請求日などで検索し、
                  入金状況や支払期限を一覧で確認できます。
                </p>
              </div>
            </div>

            <div className="relative mt-4 flex items-center justify-between">
              <div className="flex flex-col text-[11px] text-slate-400">
                <span>・ページネーション（5件 / ページ）</span>
                <span>・ステータス別のカラーラベル</span>
                <span>・請求日／支払期限の確認</span>
              </div>
              <span className="inline-flex items-center gap-1 rounded-full bg-sky-500/10 px-3 py-1 text-xs font-medium text-sky-300 group-hover:bg-sky-500/20">
                一覧を開く
                <span aria-hidden>→</span>
              </span>
            </div>
          </Link>

          {/* 会員一覧カード */}
          <Link
            href="/members"
            className="group relative overflow-hidden rounded-2xl border border-slate-800 bg-slate-900/90 p-6 shadow-md transition hover:-translate-y-1 hover:border-emerald-500/60 hover:shadow-emerald-500/20"
          >
            <div className="pointer-events-none absolute -left-10 -bottom-10 h-32 w-32 rounded-full bg-emerald-500/10 blur-3xl" />

            <div className="relative flex items-start justify-between gap-4">
              <div>
                <h2 className="flex items-center gap-2 text-lg font-semibold text-slate-50">
                  <span className="inline-flex h-8 w-8 items-center justify-center rounded-full bg-emerald-500/10 text-emerald-300 text-lg">
                    👥
                  </span>
                  会員一覧
                </h2>
                <p className="mt-2 text-sm text-slate-300">
                  顧客（会員）の基本情報・ロール・有効 / 無効状態を確認し、
                  必要に応じて退会（無効化）操作を行います。
                </p>
              </div>
            </div>

            <div className="relative mt-4 flex items-center justify-between">
              <div className="flex flex-col text-[11px] text-slate-400">
                <span>・キーワード検索（名前 / メール）</span>
                <span>・ロール絞り込み（管理者 / 一般 / 退会）</span>
                <span>・ステータスバッジ表示</span>
              </div>
              <span className="inline-flex items-center gap-1 rounded-full bg-emerald-500/10 px-3 py-1 text-xs font-medium text-emerald-300 group-hover:bg-emerald-500/20">
                一覧を開く
                <span aria-hidden>→</span>
              </span>
            </div>
          </Link>
        </section>
      </main>
    </div>
  );
}

