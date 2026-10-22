// src/app/members/page.tsx
import Link from "next/link";
import { apiGetServer } from "@/lib/api.server";
import type { MemberListItemDto } from "@/types/member";
import { DeactivateMemberButton } from "./DeactivateMemberButton";

export const dynamic = "force-dynamic";

const PAGE_SIZE = 5;

// URL の searchParams で受け取る型
type SearchParams = {
  keyword?: string;
  role?: string; // "1" | "2" | "9" | ""
  isActive?: string; // "true" | "false" | ""
  page?: string;
};

// クエリストリングを組み立てるユーティリティ
function buildQueryString(params: Record<string, string | undefined>) {
  const usp = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== "") usp.set(key, value);
  });
  const qs = usp.toString();
  return qs ? `?${qs}` : "";
}

// バックエンドから会員一覧を取得
async function fetchMembers(resolvedSearch: SearchParams) {
  const page = resolvedSearch.page ? Number(resolvedSearch.page) || 1 : 1;

  const isActive =
    resolvedSearch.isActive === "true"
      ? true
      : resolvedSearch.isActive === "false"
      ? false
      : undefined;

  const members = await apiGetServer<MemberListItemDto[]>("/api/members", {
    Keyword: resolvedSearch.keyword || undefined,
    RoleId: resolvedSearch.role ? Number(resolvedSearch.role) : undefined,
    IsActive: isActive,
    Page: page,
    PageSize: PAGE_SIZE,
  });

  const hasNextPage = members.length === PAGE_SIZE;
  return { members, page, hasNextPage };
}


function roleLabel(role: number) {
  if (role === 1) return "管理者";
  if (role === 2) return "一般会員";
  if (role === 9) return "退会";
  return `不明(${role})`;
}

export default async function MembersPage({
  searchParams,
}: {
  // ★ Next.js 16 では Promise で渡ってくる
  searchParams: Promise<SearchParams>;
}) {
  const resolved = await searchParams;
  const { members, page, hasNextPage } = await fetchMembers(resolved);

  const hasPrevPage = page > 1;

  const currentFrom = members.length > 0 ? (page - 1) * PAGE_SIZE + 1 : 0;
  const currentTo =
    members.length > 0 ? (page - 1) * PAGE_SIZE + members.length : 0;

  return (
    <div className="w-full min-w-0 text-slate-50">
      {/* 業務画面固有の見出し。全幅共通ヘッダーは members/layout.tsx が描画 */}
      <header className="min-w-0 rounded-2xl border border-slate-800 bg-slate-900/60 shadow-sm">
        <div className="w-full min-w-0 px-4 py-4 sm:px-5 sm:py-5">
          <nav aria-label="パンくずリスト" className="flex flex-wrap items-center gap-2 text-xs text-slate-400">
            <Link href="/dashboards/admin" className="hover:text-sky-300">ダッシュボード</Link>
            <span aria-hidden="true">/</span>
            <span aria-current="page">会員一覧</span>
          </nav>
          <h1 className="mt-2 text-lg font-semibold text-slate-50 sm:text-xl">会員一覧</h1>
          <p className="mt-1 text-sm leading-relaxed text-slate-400">
            会員の基本情報・ロール・有効状態を検索し、詳細を確認できます。
          </p>
        </div>
      </header>

      <main className="min-w-0 space-y-5 pt-5 sm:space-y-6 sm:pt-6">
        {/* 検索フォーム */}
        <form
          className="grid gap-4 min-w-0 rounded-2xl border border-slate-800 bg-slate-900/80 p-4 shadow-md shadow-emerald-900/20 sm:p-5 md:grid-cols-4 xl:grid-cols-6"
          method="get"
        >
          {/* キーワード */}
          <div className="flex flex-col gap-1 md:col-span-2 xl:col-span-2">
            <label className="text-[11px] font-medium text-slate-300">
              キーワード（名前 / メール）
            </label>
            <input
              type="text"
              name="keyword"
              defaultValue={resolved.keyword ?? ""}
              className="rounded-lg border border-slate-700 bg-slate-950/60 px-3 py-2 text-xs text-slate-100 outline-none ring-0 placeholder:text-slate-500 focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500"
              placeholder="例：山田 / example@example.com"
            />
          </div>

          {/* ロール */}
          <div className="flex flex-col gap-1">
            <label className="text-[11px] font-medium text-slate-300">
              ロール
            </label>
            <select
              name="role"
              defaultValue={resolved.role ?? ""}
              className="rounded-lg border border-slate-700 bg-slate-950/60 px-3 py-2 text-xs text-slate-100 outline-none ring-0 focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500"
            >
              <option value="">すべて</option>
              <option value="1">管理者</option>
              <option value="2">一般会員</option>
              <option value="9">退会</option>
            </select>
          </div>

          {/* 有効 / 無効 */}
          <div className="flex flex-col gap-1">
            <label className="text-[11px] font-medium text-slate-300">
              ステータス
            </label>
            <select
              name="isActive"
              defaultValue={resolved.isActive ?? ""}
              className="rounded-lg border border-slate-700 bg-slate-950/60 px-3 py-2 text-xs text-slate-100 outline-none ring-0 focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500"
            >
              <option value="">すべて</option>
              <option value="true">有効</option>
              <option value="false">無効（退会含む）</option>
            </select>
          </div>

          {/* ボタン */}
          <div className="flex flex-col gap-2 md:col-span-2 md:flex-row md:items-end xl:col-span-2">
            <button
              type="submit"
              className="inline-flex w-full items-center justify-center rounded-lg bg-emerald-600 px-4 py-2 text-xs font-medium text-slate-50 shadow-sm shadow-emerald-500/30 transition hover:bg-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-400 focus:ring-offset-1 focus:ring-offset-slate-950"
            >
              検索
            </button>

            <Link
              href="/members"
              className="inline-flex w-full items-center justify-center rounded-lg border border-slate-600 bg-slate-900 px-4 py-2 text-xs font-medium text-slate-200 hover:bg-slate-800 whitespace-nowrap"
            >
              リセット
            </Link>
          </div>
        </form>

        {/* =========================
            ✅ B：カード版（スマホ）
            ========================= */}
        <section className="md:hidden space-y-3">
          {members.length === 0 ? (
            <div className="rounded-2xl border border-slate-800 bg-slate-900/80 p-5 text-xs text-slate-400">
              該当する会員はいません。
            </div>
          ) : (
            members.map((m) => (
              <div
                key={m.id}
                className="rounded-2xl border border-slate-800 bg-slate-900/80 p-4 shadow-md"
              >
                <div className="flex min-w-0 items-start justify-between gap-3">
                  <div className="min-w-0">
                    <p className="text-[11px] text-slate-400">ID: {m.id}</p>
                    <p className="mt-1 text-sm font-semibold text-slate-50">
                      {m.name}
                    </p>
                    <p className="mt-1 break-all text-xs text-slate-200">
                      {m.email}
                    </p>
                  </div>

                  <div className="flex flex-col items-end gap-2">
                    {/* ロール */}
                    <span
                      className={[
                        "inline-flex items-center rounded-full px-2.5 py-1 text-[10px] font-medium",
                        m.role === 1
                          ? "bg-sky-500/15 text-sky-200 ring-1 ring-sky-500/40"
                          : m.role === 2
                          ? "bg-emerald-500/15 text-emerald-200 ring-1 ring-emerald-500/40"
                          : "bg-slate-500/20 text-slate-200 ring-1 ring-slate-500/40",
                      ].join(" ")}
                    >
                      {roleLabel(m.role)}
                    </span>

                    {/* 状態 */}
                    <span
                      className={[
                        "inline-flex items-center rounded-full px-2.5 py-1 text-[10px] font-medium",
                        m.isActive
                          ? "bg-emerald-500/15 text-emerald-200 ring-1 ring-emerald-500/40"
                          : "bg-slate-600/30 text-slate-200 ring-1 ring-slate-500/50",
                      ].join(" ")}
                    >
                      {m.isActive ? "有効" : "無効"}
                    </span>
                  </div>
                </div>

                <div className="mt-4 flex flex-wrap justify-end gap-2">
                  {/* ✅ 詳細は常に表示 */}
                  <Link
                    href={`/members/${m.id}`}
                    className="inline-flex items-center justify-center rounded-lg border border-slate-600 bg-slate-900 px-3 py-1.5 text-[11px] font-medium text-slate-200 hover:bg-slate-800"
                  >
                    詳細
                  </Link>

                  {/* ✅ 管理者は「退会」ボタンを出さない */}
                  {m.role !== 1 && (
                    <DeactivateMemberButton
                      id={m.id}
                      name={m.name}
                      isActive={m.isActive}
                      role={m.role}
                    />
                  )}
                </div>
              </div>
            ))
          )}
        </section>

        {/* =========================
            ✅ テーブル版（PC）
            ========================= */}
        <div className="hidden min-w-0 max-w-full overflow-x-auto rounded-2xl border border-slate-800 bg-slate-900/80 shadow-md md:block">
          <table className="w-full min-w-[760px] text-xs">
            <thead className="bg-slate-900/90 text-[11px] font-medium uppercase tracking-wide text-slate-400">
              <tr>
                <th className="px-4 py-3 text-left">ID</th>
                <th className="px-4 py-3 text-left">名前</th>
                <th className="px-4 py-3 text-left">メールアドレス</th>
                <th className="px-4 py-3 text-left">ロール</th>
                <th className="px-4 py-3 text-left">状態</th>
                <th className="px-4 py-3 text-right">操作</th>
              </tr>
            </thead>

            <tbody className="divide-y divide-slate-800/80">
              {members.length === 0 ? (
                <tr>
                  <td
                    colSpan={6}
                    className="px-4 py-6 text-center text-xs text-slate-400"
                  >
                    該当する会員はいません。
                  </td>
                </tr>
              ) : (
                members.map((m) => (
                  <tr
                    key={m.id}
                    className="bg-slate-900/60 hover:bg-slate-800/80"
                  >
                    <td className="px-4 py-3 align-middle text-slate-300">
                      {m.id}
                    </td>
                    <td className="px-4 py-3 align-middle font-semibold text-slate-50">
                      {m.name}
                    </td>
                    <td className="px-4 py-3 align-middle text-slate-200">
                      {m.email}
                    </td>

                    <td className="px-4 py-3 align-middle">
                      <span
                        className={[
                          "inline-flex items-center rounded-full px-2.5 py-1 text-[10px] font-medium",
                          m.role === 1
                            ? "bg-sky-500/15 text-sky-200 ring-1 ring-sky-500/40"
                            : m.role === 2
                            ? "bg-emerald-500/15 text-emerald-200 ring-1 ring-emerald-500/40"
                            : "bg-slate-500/20 text-slate-200 ring-1 ring-slate-500/40",
                        ].join(" ")}
                      >
                        {roleLabel(m.role)}
                      </span>
                    </td>

                    <td className="px-4 py-3 align-middle">
                      <span
                        className={[
                          "inline-flex items-center rounded-full px-2.5 py-1 text-[10px] font-medium",
                          m.isActive
                            ? "bg-emerald-500/15 text-emerald-200 ring-1 ring-emerald-500/40"
                            : "bg-slate-600/30 text-slate-200 ring-1 ring-slate-500/50",
                        ].join(" ")}
                      >
                        {m.isActive ? "有効" : "無効"}
                      </span>
                    </td>

                    <td className="px-4 py-3 text-right align-middle">
                      <div className="flex justify-end gap-2">
                        {/* ✅ 詳細は常に表示 */}
                        <Link
                          href={`/members/${m.id}`}
                          className="inline-flex items-center justify-center rounded-lg border border-slate-600 bg-slate-900 px-3 py-1.5 text-[11px] font-medium text-slate-200 hover:bg-slate-800"
                        >
                          詳細
                        </Link>

                        {/* ✅ 管理者は「退会」ボタンを出さない */}
                        {m.role !== 1 && (
                          <DeactivateMemberButton
                            id={m.id}
                            name={m.name}
                            isActive={m.isActive}
                            role={m.role}
                          />
                        )}
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {/* ◀ ページネーション ▶ */}
        <div className="flex flex-col gap-3 text-xs text-slate-300 sm:flex-row sm:items-center sm:justify-between">
          <p>
            {currentFrom === 0
              ? "0件"
              : `${currentFrom}–${currentTo}件を表示（1ページあたり ${PAGE_SIZE}件）`}
          </p>

          <div className="flex items-center gap-2">
            {/* 前へ */}
            <Link
              aria-disabled={!hasPrevPage}
              href={
                hasPrevPage
                  ? (() => {
                      const qs = buildQueryString({
                        keyword: resolved.keyword,
                        role: resolved.role,
                        isActive: resolved.isActive,
                        page: String(page - 1),
                      });
                      return `/members${qs}`;
                    })()
                  : "#"
              }
              className={[
                "inline-flex items-center gap-1 rounded-lg px-3 py-1.5 text-xs font-medium",
                hasPrevPage
                  ? "border border-slate-600 bg-slate-900 text-slate-100 hover:bg-slate-800"
                  : "border border-slate-800 bg-slate-900/60 text-slate-500 cursor-not-allowed",
              ].join(" ")}
            >
              <span aria-hidden>←</span>
              <span>前へ</span>
            </Link>

            {/* 次へ */}
            <Link
              aria-disabled={!hasNextPage}
              href={
                hasNextPage
                  ? (() => {
                      const qs = buildQueryString({
                        keyword: resolved.keyword,
                        role: resolved.role,
                        isActive: resolved.isActive,
                        page: String(page + 1),
                      });
                      return `/members${qs}`;
                    })()
                  : "#"
              }
              className={[
                "inline-flex items-center gap-1 rounded-lg px-3 py-1.5 text-xs font-medium",
                hasNextPage
                  ? "border border-slate-600 bg-slate-900 text-slate-100 hover:bg-slate-800"
                  : "border border-slate-800 bg-slate-900/60 text-slate-500 cursor-not-allowed",
              ].join(" ")}
            >
              <span>次へ</span>
              <span aria-hidden>→</span>
            </Link>
          </div>
        </div>
      </main>
    </div>
  );
}
