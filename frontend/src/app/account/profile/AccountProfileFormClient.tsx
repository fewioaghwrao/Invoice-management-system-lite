"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";

type AccountProfileDto = {
  id: number;
  name: string;
  email: string;
  postalCode?: string | null;
  address?: string | null;
  phone?: string | null;
};

function isEmailLike(v: string) {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v);
}

export default function AccountProfileFormClient(props: {
  initialValue: AccountProfileDto;
}) {
  const router = useRouter();
  const apiBase = process.env.NEXT_PUBLIC_API_BASE_URL ?? "";

  const [form, setForm] = useState<AccountProfileDto>(props.initialValue);

  // ★保存成功後に “初期メール” を更新できるようにする
  const [originalEmail, setOriginalEmail] = useState(props.initialValue.email);

  const [saving, setSaving] = useState(false);
  const [withdrawing, setWithdrawing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [confirmOpen, setConfirmOpen] = useState(false);

  const onChange = (patch: Partial<AccountProfileDto>) => {
    setForm((p) => ({ ...p, ...patch }));
  };

  const validate = (): string[] => {
    const issues: string[] = [];
    if (!form.name.trim()) issues.push("氏名は必須です。");
    if (!form.email.trim()) issues.push("メールアドレスは必須です。");
    if (form.email && !isEmailLike(form.email))
      issues.push("メールアドレスの形式が正しくありません。");
    return issues;
  };

  const canSave = useMemo(() => validate().length === 0, [form]);

  const emailChanged = form.email !== originalEmail;

  const doSave = async () => {
    setSaving(true);
    try {
const res = await fetch(`/api/account/me`, {
  method: "PUT",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({
    name: form.name,
    email: form.email,
    postalCode: form.postalCode,
    address: form.address,
    phone: form.phone,
  }),
});

      if (!res.ok) {
        const text = await res.text().catch(() => "");
        throw new Error(text || "保存に失敗しました。");
      }

      // ★保存が通ったら、今のメールを “確定した初期値” に更新
      setOriginalEmail(form.email);

      router.refresh(); // page.tsx が再fetchして最新初期値にもなる
    } catch (e: any) {
      setError(e?.message ?? "保存に失敗しました。");
    } finally {
      setSaving(false);
      setConfirmOpen(false);
    }
  };

  const save = async () => {
  setError(null);

  const issues = validate();
  if (issues.length > 0) {
    setError(issues.join("\n"));
    return;
  }

  // ★常に確認ポップアップを出す
  setConfirmOpen(true);
};


  const withdraw = async () => {
    setError(null);

    const ok = window.confirm(
      "本当に退会しますか？\n\n退会後はログインできなくなります。"
    );
    if (!ok) return;

    setWithdrawing(true);
    try {

      const res = await fetch(`/api/account/me`, { method: "DELETE" });

      if (!res.ok) {
        const text = await res.text().catch(() => "");
        throw new Error(text || "退会処理に失敗しました。");
      }

      // ★ cookie 即クリア（Next.js側）
      await fetch("/api/auth/logout", { method: "POST" });

      router.push("/auth/login");
    } catch (e: any) {
      setError(e?.message ?? "退会処理に失敗しました。");
    } finally {
      setWithdrawing(false);
    }
  };

  return (
    <>
      <section className="rounded-2xl border border-slate-800 bg-slate-900/80 p-6 shadow-md">
        <h2 className="mb-4 text-sm font-semibold text-slate-200">
          登録情報の確認・変更
        </h2>

        <div className="grid gap-4 md:grid-cols-2">
          <div className="flex flex-col gap-1">
            <label className="text-xs text-slate-300">氏名 *</label>
            <input
              value={form.name}
              onChange={(e) => onChange({ name: e.target.value })}
              className="rounded-lg border border-slate-700 bg-slate-950/60 px-3 py-2 text-xs text-slate-100 outline-none focus:border-emerald-500"
            />
          </div>

          <div className="flex flex-col gap-1">
            <label className="text-xs text-slate-300">メールアドレス *</label>
            <input
              value={form.email}
              onChange={(e) => onChange({ email: e.target.value })}
              className="rounded-lg border border-slate-700 bg-slate-950/60 px-3 py-2 text-xs text-slate-100 outline-none focus:border-emerald-500"
            />
            <p className="text-[11px] text-slate-500">
              ※ 本来は変更後に確認メールを送信して認証しますが、Lite版では省略しています。
            </p>
          </div>

          <div className="flex flex-col gap-1">
            <label className="text-xs text-slate-300">電話番号</label>
            <input
              value={form.phone ?? ""}
              onChange={(e) => onChange({ phone: e.target.value })}
              className="rounded-lg border border-slate-700 bg-slate-950/60 px-3 py-2 text-xs text-slate-100 outline-none focus:border-emerald-500"
            />
          </div>

          <div className="flex flex-col gap-1">
            <label className="text-xs text-slate-300">郵便番号</label>
            <input
              value={form.postalCode ?? ""}
              onChange={(e) => onChange({ postalCode: e.target.value })}
              className="rounded-lg border border-slate-700 bg-slate-950/60 px-3 py-2 text-xs text-slate-100 outline-none focus:border-emerald-500"
            />
          </div>

          <div className="md:col-span-2 flex flex-col gap-1">
            <label className="text-xs text-slate-300">住所</label>
            <input
              value={form.address ?? ""}
              onChange={(e) => onChange({ address: e.target.value })}
              className="rounded-lg border border-slate-700 bg-slate-950/60 px-3 py-2 text-xs text-slate-100 outline-none focus:border-emerald-500"
            />
          </div>
        </div>

        {error && (
          <pre className="mt-4 whitespace-pre-wrap rounded-lg border border-rose-500/30 bg-rose-500/10 p-3 text-xs text-rose-200">
            {error}
          </pre>
        )}

        <div className="mt-6 flex justify-between gap-2">
          <button
            onClick={withdraw}
            disabled={withdrawing || saving}
            className="rounded-lg border border-rose-500/40 bg-rose-500/10 px-4 py-2 text-xs text-rose-200 hover:bg-rose-500/15"
          >
            {withdrawing ? "退会処理中…" : "退会する"}
          </button>

          <button
            onClick={save}
            disabled={!canSave || saving || withdrawing}
            className="rounded-lg bg-emerald-600 px-4 py-2 text-xs text-white hover:bg-emerald-500 disabled:opacity-60"
          >
            {saving ? "保存中…" : "保存"}
          </button>
        </div>
      </section>

{confirmOpen && (
  <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
    <div className="w-full max-w-md rounded-2xl bg-slate-900 p-6 shadow-xl">
      <h3 className="text-sm font-semibold text-slate-100">
        保存内容の確認
      </h3>

      <p className="mt-3 text-xs text-slate-300">
        次の内容で保存します。よろしいですか？
      </p>

      {/* メール変更がある場合は注意表示 */}
      {emailChanged && (
        <div className="mt-3 rounded-lg border border-amber-500/30 bg-amber-500/10 p-3 text-xs text-amber-100">
          <div className="font-semibold">メールアドレスが変更されます</div>
          <div className="mt-1">変更前：{originalEmail}</div>
          <div className="mt-1">変更後：{form.email}</div>
          <div className="mt-2 text-[11px] text-amber-200/80">
            ※ Lite版のため、確認メールは送信されません。
          </div>
        </div>
      )}

      {/* 確認用のサマリー（好みで増減OK） */}
      <div className="mt-3 rounded-lg border border-slate-700 bg-slate-950/60 p-3 text-xs text-slate-200 space-y-1">
        <div>氏名：{form.name}</div>
        <div>メール：{form.email}</div>
        <div>電話：{form.phone || "—"}</div>
        <div>郵便番号：{form.postalCode || "—"}</div>
        <div>住所：{form.address || "—"}</div>
      </div>

      <div className="mt-5 flex justify-end gap-2">
        <button
          onClick={() => setConfirmOpen(false)}
          className="rounded-lg border border-slate-600 bg-slate-800 px-4 py-2 text-xs text-slate-200 hover:bg-slate-700"
          disabled={saving}
        >
          キャンセル
        </button>
        <button
          onClick={doSave}
          className="rounded-lg bg-emerald-600 px-4 py-2 text-xs text-white hover:bg-emerald-500"
          disabled={saving}
        >
          {saving ? "保存中…" : "保存する"}
        </button>
      </div>
    </div>
  </div>
)}

    </>
  );
}
