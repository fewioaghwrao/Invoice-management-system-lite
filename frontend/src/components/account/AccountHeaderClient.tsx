"use client";

import { usePathname } from "next/navigation";
import CurrentUserBadge from "@/components/CurrentUserBadge";
import LogoutButton from "@/components/LogoutButton";

type HeaderMeta = {
  title: string;
  desc: string;
};

function getHeaderMeta(pathname: string): HeaderMeta {
  if (pathname.startsWith("/account/invoices/")) {
    return {
      title: "請求書詳細",
      desc: "請求内容やお支払い状況を確認できます。",
    };
  }

  if (pathname === "/account/invoices") {
    return {
      title: "請求書一覧",
      desc: "請求書の金額・支払期限・入金状況を確認できます。",
    };
  }

  if (pathname === "/account/unpaid") {
    return {
      title: "未入金の確認",
      desc: "未入金の請求書と支払期限を確認できます。",
    };
  }

  if (pathname === "/account/profile") {
    return {
      title: "登録情報",
      desc: "氏名・メールアドレス・住所などを確認・変更できます。",
    };
  }

  return {
    title: "会員メニュー",
    desc: "請求書や登録情報を確認できます。",
  };
}

export default function AccountHeaderClient() {
  const pathname = usePathname();
  const { title, desc } = getHeaderMeta(pathname);

  return (
    <header className="border-b border-slate-800 bg-slate-950/40 backdrop-blur">
      <div className="mx-auto flex w-full max-w-[1440px] flex-col gap-3 px-4 py-4 sm:px-6 md:flex-row md:items-center md:justify-between md:gap-4 md:py-5 xl:px-8">
        {/* スマホではタイトルとユーザー操作を別の行に表示 */}
        <div className="min-w-0">
          <h1 className="text-base font-semibold text-slate-100 sm:text-lg">
            {title}
          </h1>
          <p className="mt-1 text-xs leading-relaxed text-slate-400 sm:text-sm">
            {desc}
          </p>
        </div>

        {/* ナビゲーションに「マイページ」があるため戻るボタンは置かない */}
        <div className="flex w-full flex-wrap items-center justify-between gap-2 md:w-auto md:shrink-0 md:justify-end md:gap-3">
          <CurrentUserBadge />
          <LogoutButton />
        </div>
      </div>
    </header>
  );
}
