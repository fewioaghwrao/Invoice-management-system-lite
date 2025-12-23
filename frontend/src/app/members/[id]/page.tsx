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
  role: number;        // ← enumは JSON では number で来るのでOK
  isActive: boolean;
  createdAt: string;  // ISO文字列
};

export default async function MemberDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;

  // ★ API はあなたのバックエンド仕様に合わせて調整
  const member = await apiGetServer<MemberDetailDto>(`/api/members/${id}`);

  return (
    <div className="min-h-screen bg-slate-950 text-slate-50">
      <header className="border-b border-slate-800 bg-slate-950/80 backdrop-blur">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
          <div>
            <div className="flex items-center gap-2 text-[11px] text-slate-400">
              <Link href="/dashboards/admin" className="hover:text-sky-300">
                ダッシュボード
              </Link>
              <span>/</span>
              <Link href="/members" className="hover:text-sky-300">
                会員一覧
              </Link>
              <span>/</span>
              <span>詳細</span>
            </div>
            <h1 className="mt-1 text-lg font-semibold">会員詳細（編集）</h1>
            <p className="mt-1 text-xs text-slate-400">
              管理者が顧客情報を編集します（メール認証なし）。
            </p>
          </div>
          <Link
            href="/members"
            className="inline-flex items-center gap-1 rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-1.5 text-xs font-medium text-slate-200 hover:bg-slate-800"
          >
            ← 一覧へ
          </Link>
        </div>
      </header>

      <main className="mx-auto max-w-6xl px-6 py-8">
        <MemberFormClient mode="edit" initialValue={member} />
      </main>
    </div>
  );
}
