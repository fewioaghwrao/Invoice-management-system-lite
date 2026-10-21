// src/app/members/[id]/page.tsx
import Link from "next/link";
import { apiGetServer } from "@/lib/api.server";
import MemberFormClient from "../shared/MemberFormClient";

type MemberDetailDto = {
  id: number;
  name: string;
  email: string;
  postalCode?: string | null;
  address?: string | null;
  phone?: string | null;
  role: number;
  isActive: boolean;
  createdAt: string;
};

export default async function MemberDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const member = await apiGetServer<MemberDetailDto>(`/api/members/${id}`);

  return (
    <div className="w-full min-w-0 text-slate-50">
      {/* 管理者共通ヘッダーとメニューは members/layout.tsx で表示 */}
      <header className="min-w-0 rounded-2xl border border-slate-800 bg-slate-900/60 shadow-sm">
        <div className="flex flex-col gap-3 px-4 py-4 sm:flex-row sm:items-center sm:justify-between sm:px-5 sm:py-5">
          <div className="min-w-0">
            <nav aria-label="パンくずリスト" className="flex flex-wrap items-center gap-2 text-xs text-slate-400">
              <Link href="/dashboards/admin" className="hover:text-sky-300">ダッシュボード</Link>
              <span aria-hidden="true">/</span>
              <Link href="/members" className="hover:text-sky-300">会員一覧</Link>
              <span aria-hidden="true">/</span>
              <span aria-current="page">会員詳細</span>
            </nav>
            <h1 className="mt-2 text-lg font-semibold text-slate-50 sm:text-xl">会員詳細・編集</h1>
            <p className="mt-1 text-sm leading-relaxed text-slate-400">
              会員情報の確認・編集と、アカウントの有効状態を確認できます。
            </p>
          </div>
          <Link
            href="/members"
            className="inline-flex min-h-10 shrink-0 items-center justify-center self-start rounded-lg border border-slate-700 bg-slate-900/60 px-4 py-2 text-sm font-medium text-slate-200 hover:bg-slate-800 sm:self-auto"
          >
            ← 一覧へ
          </Link>
        </div>
      </header>

      <main className="min-w-0 pt-5 sm:pt-6">
        <MemberFormClient mode="edit" initialValue={member} />
      </main>
    </div>
  );
}
