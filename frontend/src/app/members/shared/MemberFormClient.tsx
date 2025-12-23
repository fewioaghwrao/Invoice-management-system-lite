// src/app/members/shared/MemberFormClient.tsx
"use client";

import { useRouter } from "next/navigation";
import { useMemo, useState } from "react";

type MemberFormValue = {
  id?: number;
  name: string;
  email: string;
  postalCode?: string | null;
  address?: string | null;
  phone?: string | null;

  // デモ破壊防止：ロールはロック（表示だけ）
  role: number; // 2(一般) or 9(退会) を想定
  isActive: boolean;
};

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

function roleLabel(role: number) {
  if (role === 1) return "管理者";
  if (role === 2) return "一般会員";
  if (role === 9) return "退会";
  return `不明(${role})`;
}

function isEmailLike(v: string) {
  // ゆるめ（実務でもこのくらいでOK）
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v);
}

export default function MemberFormClient(props: {
  mode: "create" | "edit";
  initialValue: MemberDetailDto | null;
}) {
  const router = useRouter();

  const [form, setForm] = useState<MemberFormValue>(() => {
    if (props.initialValue) return { ...props.initialValue };
    // 今回 create は使わない前提だが、型としては残しておく
    return {
      name: "",
      email: "",
      postalCode: null,
      address: null,
      phone: null,
      role: 2,
      isActive: true,
    };
  });

  const [saving, setSaving] = useState(false);
  const [disabling, setDisabling] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const apiBase = process.env.NEXT_PUBLIC_API_BASE_URL ?? "";

  const onChange = (patch: Partial<MemberFormValue>) => {
    setForm((p) => ({ ...p, ...patch }));
  };

  // ✅ バリデーション
  const validate = (): string[] => {
    const issues: string[] = [];

    if (!form.name?.trim()) issues.push("名前は必須です。");
    if (!form.email?.trim()) issues.push("メールアドレスは必須です。");
    if (form.email?.trim() && !isEmailLike(form.email.trim()))
      issues.push("メールアドレスの形式が正しくありません。");

    // デモ故障防止（管理者ロールの混入・変更を防ぐ）
    if (form.role === 1) issues.push("管理者ロールはこの画面では編集できません。");

    // 退会済みを編集したい要件がないならブロック
    if (form.role === 9 || form.isActive === false) {
      // 「退会＝disable」方針なら、通常保存を止める方が安全
      issues.push("この会員は無効（退会）です。編集はできません。");
    }

    return issues;
  };

  const canSave = useMemo(() => validate().length === 0, [form]);

  // ✅ 通常の保存（編集）
  const save = async () => {
    setError(null);

    const issues = validate();
    if (issues.length > 0) {
      setError(issues.join("\n"));
      return;
    }

    if (props.mode !== "edit" || !form.id) {
      setError("編集対象のIDが不明です。");
      return;
    }

    setSaving(true);
    try {
      const url = `${apiBase}/api/members/${form.id}`;

      // ★ Role / IsActive を送ると「変更可能」に見えるので、
      // ここでは編集対象だけ送る（デモ破壊防止）
      const res = await fetch(url, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          name: form.name,
          email: form.email,
          postalCode: form.postalCode,
          address: form.address,
          phone: form.phone,

          // もしC#側が必須なら固定で送る（今回は「ロック」なので初期値維持）
          roleId: form.role,
          isActive: form.isActive,
        }),
      });

      if (!res.ok) {
        const text = await res.text().catch(() => "");
        throw new Error(text || "保存に失敗しました。");
      }

      router.push("/members");
      router.refresh();
    } catch (e: any) {
      setError(e?.message ?? "保存に失敗しました。");
    } finally {
      setSaving(false);
    }
  };

  // ✅ 退会（= disable）
  const disable = async () => {
    setError(null);

    if (!form.id) {
      setError("IDが不明です。");
      return;
    }

    // 「退会＝即 disable の確認ダイアログ」
    const ok = window.confirm(
      `本当に退会（無効化）しますか？\n\n対象: ${form.name}（${form.email}）\n\n※この操作は元に戻せません。`
    );
    if (!ok) return;

    setDisabling(true);
    try {
      const url = `${apiBase}/api/members/${form.id}/disable`;
      const res = await fetch(url, { method: "PUT" });

      if (!res.ok) {
        const text = await res.text().catch(() => "");
        throw new Error(text || "退会（無効化）に失敗しました。");
      }

      router.push("/members");
      router.refresh();
    } catch (e: any) {
      setError(e?.message ?? "退会（無効化）に失敗しました。");
    } finally {
      setDisabling(false);
    }
  };

  const isDisabledMember = form.role === 9 || form.isActive === false;
  const isAdmin = form.role === 1; // ★追加：管理者ロック

  return (
    <section className="rounded-2xl border border-slate-800 bg-slate-900/80 p-5 shadow-md">
      <div className="grid gap-4 md:grid-cols-2">
        <div className="flex flex-col gap-1">
          <label className="text-[11px] font-medium text-slate-300">名前</label>
          <input
            value={form.name}
            onChange={(e) => onChange({ name: e.target.value })}
            className="rounded-lg border border-slate-700 bg-slate-950/60 px-3 py-2 text-xs text-slate-100 outline-none focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500 disabled:opacity-60"
            placeholder="例：山田 太郎"
            disabled={saving || disabling || isDisabledMember}
          />
        </div>

        <div className="flex flex-col gap-1">
          <label className="text-[11px] font-medium text-slate-300">メール</label>
          <input
            value={form.email}
            onChange={(e) => onChange({ email: e.target.value })}
            className="rounded-lg border border-slate-700 bg-slate-950/60 px-3 py-2 text-xs text-slate-100 outline-none focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500 disabled:opacity-60"
            placeholder="例：example@example.com"
            disabled={saving || disabling || isDisabledMember}
          />
          <p className="text-[11px] text-slate-500">
            ※管理画面編集のためメール認証は行いません（デモ用）。
          </p>
        </div>

        {/* 住所系（DTOに合わせて追加） */}
        <div className="flex flex-col gap-1">
          <label className="text-[11px] font-medium text-slate-300">郵便番号</label>
          <input
            value={form.postalCode ?? ""}
            onChange={(e) => onChange({ postalCode: e.target.value })}
            className="rounded-lg border border-slate-700 bg-slate-950/60 px-3 py-2 text-xs text-slate-100 outline-none focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500 disabled:opacity-60"
            placeholder="例：123-4567"
            disabled={saving || disabling || isDisabledMember}
          />
        </div>

        <div className="flex flex-col gap-1">
          <label className="text-[11px] font-medium text-slate-300">住所</label>
          <input
            value={form.address ?? ""}
            onChange={(e) => onChange({ address: e.target.value })}
            className="rounded-lg border border-slate-700 bg-slate-950/60 px-3 py-2 text-xs text-slate-100 outline-none focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500 disabled:opacity-60"
            placeholder="例：東京都〇〇区..."
            disabled={saving || disabling || isDisabledMember}
          />
        </div>

        <div className="flex flex-col gap-1">
          <label className="text-[11px] font-medium text-slate-300">電話番号</label>
          <input
            value={form.phone ?? ""}
            onChange={(e) => onChange({ phone: e.target.value })}
            className="rounded-lg border border-slate-700 bg-slate-950/60 px-3 py-2 text-xs text-slate-100 outline-none focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500 disabled:opacity-60"
            placeholder="例：090-1234-5678"
            disabled={saving || disabling || isDisabledMember}
          />
        </div>

        {/* ロール：ロック＆管理者除外（選択UIをやめて表示だけ） */}
        <div className="flex flex-col gap-1">
          <label className="text-[11px] font-medium text-slate-300">ロール</label>
          <input
            value={roleLabel(form.role)}
            readOnly
            className="rounded-lg border border-slate-800 bg-slate-950/40 px-3 py-2 text-xs text-slate-300"
          />
          <p className="text-[11px] text-slate-500">
            ※デモの故障防止のためロールは固定です（管理者選択も不可）。
          </p>
        </div>

        {/* 状態：ここもロック推奨（退会＝disableに寄せる） */}
        <div className="flex flex-col gap-1">
          <label className="text-[11px] font-medium text-slate-300">状態</label>
          <input
            value={form.isActive ? "有効" : "無効"}
            readOnly
            className="rounded-lg border border-slate-800 bg-slate-950/40 px-3 py-2 text-xs text-slate-300"
          />
          <p className="text-[11px] text-slate-500">
            ※状態変更は「退会（無効化）」操作で行います。
          </p>
        </div>
      </div>

      {error && (
        <pre className="mt-4 whitespace-pre-wrap rounded-lg border border-rose-500/30 bg-rose-500/10 p-3 text-xs text-rose-200">
          {error}
        </pre>
      )}

      <div className="mt-6 flex flex-wrap items-center justify-between gap-2">
        <button
          type="button"
          onClick={() => router.back()}
          className="rounded-lg border border-slate-600 bg-slate-900 px-4 py-2 text-xs font-medium text-slate-200 hover:bg-slate-800 disabled:opacity-60"
          disabled={saving || disabling}
        >
          戻る
        </button>

  <div className="flex items-center gap-2">
    {/* 退会ボタン：退会済み/管理者は出さない */}
    {!isDisabledMember && !isAdmin && (
      <button
        type="button"
        onClick={disable}
        className="rounded-lg border border-rose-500/40 bg-rose-500/10 px-4 py-2 text-xs font-medium text-rose-200 hover:bg-rose-500/15 disabled:opacity-60"
        disabled={saving || disabling}
      >
        {disabling ? "退会処理中..." : "退会（無効化）"}
      </button>
    )}

    <button
      type="button"
      onClick={save}
      className="rounded-lg bg-emerald-600 px-4 py-2 text-xs font-medium text-slate-50 shadow-sm shadow-emerald-500/30 hover:bg-emerald-500 disabled:opacity-60"
      disabled={saving || disabling || !canSave}
      title={!canSave ? "入力内容を確認してください" : undefined}
    >
      {saving ? "保存中..." : "保存"}
    </button>
  </div>
      </div>
    </section>
  );
}

