import Link from "next/link";
import PaymentNewClient from "./PaymentNewClient";

export default function PaymentNewPage() {
  return (
    <div className="w-full min-w-0 text-slate-50">
      <header className="mb-5 rounded-2xl border border-slate-800 bg-slate-900/70 p-4 shadow-sm sm:mb-6 sm:p-5">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <div className="min-w-0">
            <nav aria-label="パンくずリスト" className="flex flex-wrap items-center gap-2 text-xs text-slate-400">
              <Link href="/dashboards/admin" className="hover:text-sky-300">ダッシュボード</Link>
              <span aria-hidden="true">/</span>
              <Link href="/payments" className="hover:text-sky-300">入金一覧</Link>
              <span aria-hidden="true">/</span>
              <span>新規登録</span>
            </nav>
            <h1 className="mt-1 text-lg font-semibold">入金登録（手動）</h1>
            <p className="mt-1 text-xs leading-relaxed text-slate-400">
              入金日・金額・入金名義を入力して登録します。登録後は請求書への割当画面へ進みます。
            </p>
          </div>
          <Link
            href="/payments"
            className="inline-flex min-h-9 shrink-0 items-center justify-center rounded-lg border border-slate-700 bg-slate-900/60 px-3 text-xs font-medium text-slate-200 hover:bg-slate-800"
          >
            ← 入金一覧へ
          </Link>
        </div>
      </header>
      <main className="w-full min-w-0">
        <PaymentNewClient />
      </main>
    </div>
  );
}
