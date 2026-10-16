// src/app/account/layout.tsx
import AccountHeaderClient from "@/components/account/AccountHeaderClient";
import AccountNavClient from "@/components/account/AccountNavClient";

export default function AccountLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <div className="flex min-h-screen flex-col bg-gradient-to-b from-zinc-950 via-zinc-900 to-zinc-950 text-slate-100">
      <AccountHeaderClient />

      <main className="mx-auto w-full max-w-[1440px] flex-1 px-4 py-5 sm:px-6 sm:py-8 xl:px-8">
        <div className="grid min-w-0 gap-5 lg:grid-cols-[200px_minmax(0,1fr)] lg:gap-6 xl:grid-cols-[216px_minmax(0,1fr)]">
          {/* スマホ・タブレットは折りたたみ式メニュー */}
          <details className="group rounded-2xl border border-slate-800 bg-slate-900/70 lg:hidden">
            <summary className="flex min-h-11 cursor-pointer list-none items-center justify-between px-4 py-3 text-sm font-semibold text-slate-100 [&::-webkit-details-marker]:hidden">
              <span>メニューを開く</span>
              <span aria-hidden="true" className="text-slate-400 transition-transform group-open:rotate-180">
                ⌄
              </span>
            </summary>
            <div className="border-t border-slate-800 p-2">
              <AccountNavClient />
            </div>
          </details>

          {/* PCは共通の左メニュー */}
          <aside className="hidden self-start lg:sticky lg:top-6 lg:block">
            <div className="rounded-2xl border border-slate-800 bg-slate-900/70 p-2 shadow-sm">
              <AccountNavClient />
            </div>
          </aside>

          <div className="min-w-0">{children}</div>
        </div>
      </main>

      <footer className="mt-auto border-t border-slate-800 bg-slate-950/30">
        <div className="mx-auto max-w-[1440px] px-4 py-4 text-xs text-slate-500 sm:px-6 xl:px-8">
          Invoice Management System
        </div>
      </footer>
    </div>
  );
}
