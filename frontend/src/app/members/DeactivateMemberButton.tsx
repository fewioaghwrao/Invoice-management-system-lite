// src/app/members/DeactivateMemberButton.tsx
"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";

type Props = {
  id: number;
  name: string;
  isActive: boolean;
  role: number; // 1:管理者 2:一般 9:退会
};

export function DeactivateMemberButton({ id, name, isActive, role }: Props) {
  const router = useRouter();
  const [loading, setLoading] = useState(false);

  // ✅ 管理者は退会させない（非表示 or ロック）
  // 今回は「非表示」にする（要件どおり）
  if (role === 1) return null;

  const disabled = loading || !isActive || role === 9;

  const handleClick = async () => {
    if (disabled) return;

    // ✅ 退会＝即 disable の確認ダイアログ
    const ok = window.confirm(
      `「${name}」を退会（無効化）しますか？\n\n※この操作は元に戻せません。`
    );
    if (!ok) return;

    setLoading(true);
    try {
      // ✅ API のURLを統一（/disable）
      const res = await fetch(
        `${process.env.NEXT_PUBLIC_API_BASE_URL}/api/members/${id}/disable`,
        { method: "PUT" }
      );

      if (!res.ok) {
        const text = await res.text().catch(() => "");
        throw new Error(text || "退会（無効化）に失敗しました。");
      }

      // ✅ App Routerなら reload より refresh が自然
      router.refresh();
    } catch (e: any) {
      alert(e?.message ?? "退会（無効化）に失敗しました。");
    } finally {
      setLoading(false);
    }
  };

  return (
    <button
      type="button"
      onClick={handleClick}
      disabled={disabled}
      className={[
        "inline-flex items-center justify-center rounded-lg border px-3 py-1.5 text-[11px] font-medium",
        disabled
          ? "border-slate-700 bg-slate-900/60 text-slate-500 cursor-not-allowed"
          : "border-rose-500/40 bg-rose-500/10 text-rose-200 hover:bg-rose-500/15",
      ].join(" ")}
    >
      {loading ? "退会中..." : "退会"}
    </button>
  );
}

