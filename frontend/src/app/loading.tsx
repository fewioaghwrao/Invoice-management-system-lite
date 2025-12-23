// src/app/loading.tsx
export default function Loading() {
  return (
    <div className="min-h-screen bg-slate-950 text-slate-50">
      {/* 背景グラデーション（既存の世界観に合わせる） */}
      <div className="pointer-events-none fixed inset-0 bg-[radial-gradient(circle_at_top,_#0ea5e9_0,_transparent_55%),radial-gradient(circle_at_bottom,_#6366f1_0,_transparent_55%)] opacity-60" />

      <div className="relative z-10 mx-auto max-w-6xl px-4 sm:px-6 py-10">
        {/* ヘッダー風 */}
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <div className="h-4 w-40 rounded bg-slate-800/60 animate-pulse" />
            <div className="mt-2 h-3 w-96 max-w-full rounded bg-slate-800/40 animate-pulse" />
          </div>
          <div className="flex gap-2">
            <div className="h-8 w-28 rounded-lg bg-slate-800/50 animate-pulse" />
            <div className="h-8 w-20 rounded-lg bg-slate-800/50 animate-pulse" />
          </div>
        </div>

        {/* 本体：カード群（一覧・詳細・フォームどれでも破綻しない） */}
        <div className="mt-8 grid gap-4 lg:grid-cols-3">
          {/* 左カラム風 */}
          <div className="space-y-4">
            {Array.from({ length: 3 }).map((_, i) => (
              <div
                key={i}
                className="rounded-2xl border border-slate-800 bg-slate-900/70 p-5 shadow-md"
              >
                <div className="h-3 w-24 rounded bg-slate-800/60 animate-pulse" />
                <div className="mt-3 h-4 w-56 rounded bg-slate-800/50 animate-pulse" />
                <div className="mt-2 h-3 w-40 rounded bg-slate-800/40 animate-pulse" />
              </div>
            ))}
          </div>

          {/* 右カラム風（大きめ） */}
          <div className="lg:col-span-2 space-y-4">
            <div className="rounded-2xl border border-slate-800 bg-slate-900/70 p-5 shadow-md">
              <div className="flex items-center justify-between">
                <div className="h-4 w-40 rounded bg-slate-800/60 animate-pulse" />
                <div className="h-8 w-24 rounded-lg bg-slate-800/50 animate-pulse" />
              </div>

              <div className="mt-4 space-y-3">
                {Array.from({ length: 6 }).map((_, r) => (
                  <div
                    key={r}
                    className="rounded-xl border border-slate-800 bg-slate-950/40 p-4"
                  >
                    <div className="h-3 w-64 rounded bg-slate-800/50 animate-pulse" />
                    <div className="mt-2 h-3 w-44 rounded bg-slate-800/40 animate-pulse" />
                  </div>
                ))}
              </div>
            </div>

            <div className="rounded-2xl border border-slate-800 bg-slate-900/70 p-5 shadow-md">
              <div className="h-4 w-32 rounded bg-slate-800/60 animate-pulse" />
              <div className="mt-4 grid gap-3 sm:grid-cols-2">
                {Array.from({ length: 4 }).map((_, i) => (
                  <div
                    key={i}
                    className="rounded-xl border border-slate-800 bg-slate-950/40 p-4"
                  >
                    <div className="h-3 w-28 rounded bg-slate-800/50 animate-pulse" />
                    <div className="mt-2 h-4 w-full rounded bg-slate-800/40 animate-pulse" />
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>

        {/* 画面下部：進行中メッセージ */}
        <div className="mt-8 text-center text-xs text-slate-400">
          読み込み中…
        </div>
      </div>
    </div>
  );
}
