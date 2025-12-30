import Link from "next/link";
import CurrentUserBadge from "@/components/CurrentUserBadge";
import LogoutButton from "@/components/LogoutButton";
import PaymentDetailClient from "./PaymentDetailClient";

export default async function PaymentDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;

  return (
    <div className="min-h-screen bg-slate-950 text-slate-50">
      <div className="pointer-events-none fixed inset-0 bg-[radial-gradient(circle_at_top,_#10b981_0,_transparent_55%),radial-gradient(circle_at_bottom,_#6366f1_0,_transparent_55%)] opacity-60" />

<header className="relative z-10 border-b border-slate-800 bg-slate-950/80 backdrop-blur">
  <div className="mx-auto max-w-6xl px-4 sm:px-6 py-4">
    <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
      <div className="min-w-0">
        <div className="flex flex-wrap items-center gap-2 text-[11px] text-slate-400">
          <Link href="/dashboards/admin" className="hover:text-sky-300">ダッシュボード</Link>
          <span>/</span>
          <Link href="/payments" className="hover:text-sky-300">入金一覧</Link>
          <span>/</span>
          <span>入金詳細</span>
        </div>

        <h1 className="mt-1 text-lg font-semibold">入金詳細（割当）</h1>
        <p className="mt-1 text-xs text-slate-400">入金を請求書へ割り当てます。</p>
      </div>

      <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:gap-3">
        <Link href="/payments" className="text-xs text-slate-300 hover:text-slate-100">
          ← 入金一覧へ
        </Link>

        <div className="flex flex-wrap items-center gap-2 sm:flex-nowrap sm:justify-end">
          <CurrentUserBadge />
          <LogoutButton />
        </div>
      </div>
    </div>
  </div>
</header>


      <main className="relative z-10 mx-auto max-w-6xl px-6 py-8">
        <PaymentDetailClient paymentId={decodeURIComponent(id)} />
      </main>
    </div>
  );
}
