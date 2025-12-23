import Link from "next/link";

export default function NotFound() {
  return (
    <div className="min-h-screen bg-slate-950 text-slate-50">
      <div className="pointer-events-none fixed inset-0 bg-[radial-gradient(circle_at_top,_#0ea5e9_0,_transparent_55%),radial-gradient(circle_at_bottom,_#6366f1_0,_transparent_55%)] opacity-60" />

      <main className="relative z-10 mx-auto flex max-w-2xl flex-col items-center px-4 py-16 text-center">
        <div className="rounded-2xl border border-slate-800 bg-slate-900/80 p-6 shadow-md">
          <p className="text-[11px] text-slate-400">404</p>
          <h1 className="mt-2 text-xl font-semibold">ページが見つかりません</h1>
          <p className="mt-2 text-xs text-slate-400 leading-relaxed">
            URLが間違っているか、ページが移動した可能性があります。
          </p>

          <div className="mt-5 grid gap-2 sm:grid-cols-2">
            <Link
              href="/dashboards/admin"
              className="inline-flex items-center justify-center rounded-lg bg-sky-600 px-4 py-2 text-xs font-medium text-slate-50 shadow-sm shadow-sky-500/30 hover:bg-sky-500"
            >
              管理トップへ
            </Link>
            <Link
              href="/invoices"
              className="inline-flex items-center justify-center rounded-lg border border-slate-700 bg-slate-900 px-4 py-2 text-xs font-medium text-slate-200 hover:bg-slate-800"
            >
              請求書一覧へ
            </Link>
          </div>
        </div>
      </main>
    </div>
  );
}
