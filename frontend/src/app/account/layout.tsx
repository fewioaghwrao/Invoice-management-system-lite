// src/app/account/layout.tsx
import AccountHeaderClient from "@/components/account/AccountHeaderClient";
import AccountNavClient from "@/components/account/AccountNavClient";

export default function AccountLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <div className="min-h-screen bg-gradient-to-b from-zinc-950 via-zinc-900 to-zinc-950">
      {/* 共通ヘッダー（Client側で現在地に応じて文言を切替） */}
      <AccountHeaderClient />

      {/* 2カラム（左メニュー + 右コンテンツ） */}
      <main className="mx-auto max-w-5xl px-4 sm:px-6 py-6 sm:py-8">
        <div className="grid gap-6 lg:grid-cols-[260px_1fr]">
          {/* 左メニュー */}
          <nav className="lg:sticky lg:top-6" aria-label="Account navigation">
            <div className="rounded-2xl border border-slate-800 bg-slate-900/70 p-3 shadow-sm">
              <AccountNavClient />
            </div>
          </nav>

          {/* 右コンテンツ */}
          <section className="min-w-0">{children}</section>
        </div>
      </main>

      {/* 共通フッター */}
      <footer className="border-t border-slate-800 bg-slate-950/30">
        <div className="mx-auto max-w-5xl px-4 sm:px-6 py-4 text-[11px] text-slate-500">
          Invoice &amp; Payment Status Dashboard (Lite) / Account
        </div>
      </footer>
    </div>
  );
}
