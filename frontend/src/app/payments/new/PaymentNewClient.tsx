"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";

type MemberOption = {
  id: number;
  name: string;
  email: string;
};

type CreatePaymentRequestDto = {
  memberId: number;
  paymentDate: string; // "YYYY-MM-DD"
  amount: number;
  payerName?: string | null;
  method?: string | null;
};

type RawMember = {
  id: number;
  name: string;
  email: string;
};

function isDateLike(v: string) {
  return /^\d{4}-\d{2}-\d{2}$/.test(v);
}

function getApiBase(): string {
  const base = process.env.NEXT_PUBLIC_API_BASE_URL;
  if (!base || !base.trim()) {
    // .env.local 未設定や、再起動忘れに気づけるように明示
    throw new Error("NEXT_PUBLIC_API_BASE_URL が未設定です。.env.local を確認して Next.js を再起動してください。");
  }
  return base.replace(/\/+$/, ""); // 末尾スラッシュ除去
}

export default function PaymentNewClient() {
  const router = useRouter();

  // default date: today (local)
  const today = new Date();
  const yyyy = today.getFullYear();
  const mm = String(today.getMonth() + 1).padStart(2, "0");
  const dd = String(today.getDate()).padStart(2, "0");

  const [paymentDate, setPaymentDate] = useState(`${yyyy}-${mm}-${dd}`);

  // members
  const [members, setMembers] = useState<MemberOption[]>([]);
  const [memberId, setMemberId] = useState<string>("");

  // payerName auto set but editable
  const [payerName, setPayerName] = useState("");
  const [payerNameTouched, setPayerNameTouched] = useState(false);

  const [amount, setAmount] = useState<string>("");
  const [method, setMethod] = useState<string>("BANK_TRANSFER"); // 任意：初期値

  const [loadingMembers, setLoadingMembers] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // load members (active customers)
  useEffect(() => {
    let cancelled = false;

    (async () => {
      setLoadingMembers(true);
      setError(null);
      try {
        const base = getApiBase();
        // MemberEndpoints 仕様：RoleId=2(Customer), IsActive=true
        // API 側が MapGet("/") の場合もあるので、末尾 / を付けておく
        const url = `${base}/api/members/?RoleId=2&IsActive=true&Page=1&PageSize=200`;

        const res = await fetch(url, {
          method: "GET",
          headers: { Accept: "application/json" },
        });

        if (!res.ok) {
          const text = await res.text();
          throw new Error(text || "会員一覧の取得に失敗しました。");
        }

        const data: unknown = await res.json();

        // 返却が配列 or ページング結果 { rows: [...] } の両対応
        const list: unknown[] =
          Array.isArray(data) ? data :
          (typeof data === "object" && data !== null && Array.isArray((data as any).rows)) ? (data as any).rows :
          [];

        const normalized: MemberOption[] = (list as RawMember[])
          .map((x: RawMember) => ({
            id: Number(x.id),
            name: String(x.name ?? ""),
            email: String(x.email ?? ""),
          }))
          .filter((x) => Number.isFinite(x.id) && x.id > 0 && x.name);

        if (!cancelled) setMembers(normalized);
      } catch (e: any) {
        if (!cancelled) setError(e?.message ?? "会員一覧の取得に失敗しました。");
      } finally {
        if (!cancelled) setLoadingMembers(false);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, []);

  // auto set payerName when member changes (unless user already edited it)
  useEffect(() => {
    if (!memberId) return;
    if (payerNameTouched) return;

    const m = members.find((x) => String(x.id) === memberId);
    if (m) setPayerName(m.name);
  }, [memberId, members, payerNameTouched]);

  const issues = useMemo(() => {
    const x: string[] = [];

    if (loadingMembers) {
      x.push("会員一覧を読み込み中です…");
      return x;
    }

    if (members.length === 0) {
      x.push("有効な会員が存在しません。会員を作成してから入金登録してください。");
      return x;
    }

    if (!memberId) x.push("会員（入金元）を選択してください。");

    if (!paymentDate.trim()) x.push("入金日は必須です。");
    if (paymentDate.trim() && !isDateLike(paymentDate.trim()))
      x.push("入金日は YYYY-MM-DD 形式で入力してください。");

    if (!payerName.trim()) x.push("入金名義は必須です。");

    if (!amount.trim()) x.push("入金額は必須です。");
    const num = Number(amount);
    if (amount.trim() && (!Number.isFinite(num) || num <= 0))
      x.push("入金額は 1 以上の数値で入力してください。");

    return x;
  }, [memberId, paymentDate, payerName, amount, loadingMembers, members.length]);

  const canSave = issues.length === 0;

  const onSubmit = async () => {
    setError(null);

    if (!canSave) {
      setError(issues.join("\n"));
      return;
    }

    const ok = window.confirm("入金を登録します。続けて割当（詳細）へ進みます。よろしいですか？");
    if (!ok) return;

    setSaving(true);
    try {
      const base = getApiBase();

      const req: CreatePaymentRequestDto = {
        memberId: Number(memberId),
        paymentDate: paymentDate.trim(),
        amount: Number(amount),
        payerName: payerName.trim(),
        method: method.trim() ? method.trim() : null,
      };

      // ★ ここが重要：相対 /api/payments ではなく、必ず base 付きで叩く
      const res = await fetch(`${base}/api/payments`, {
        method: "POST",
        headers: { "Content-Type": "application/json", Accept: "application/json" },
        body: JSON.stringify(req),
      });

      if (!res.ok) {
        const text = await res.text();
        throw new Error(text || "登録に失敗しました。");
      }

      const data = (await res.json()) as { id: number };
      if (!data?.id) throw new Error("登録結果の形式が不正です。");

      // ✅ 登録→割当（詳細）へ（DBのID）
      router.push(`/payments/${encodeURIComponent(String(data.id))}`);
      router.refresh();
    } catch (e: any) {
      setError(e?.message ?? "登録に失敗しました。");
    } finally {
      setSaving(false);
    }
  };

  return (
    <section className="rounded-2xl border border-slate-800 bg-slate-900/80 p-5 shadow-md">
      <div className="grid gap-4 md:grid-cols-2">
        {/* 会員選択 */}
        <div className="flex flex-col gap-1 md:col-span-2">
          <label className="text-[11px] font-medium text-slate-300">会員（入金元）</label>

          <select
            value={memberId}
            onChange={(e) => {
              setMemberId(e.target.value);
              // 会員を変えたら payerName を自動追従させたいので「未編集状態」に戻す
              setPayerNameTouched(false);
            }}
            className="rounded-lg border border-slate-700 bg-slate-950/60 px-3 py-2 text-xs text-slate-100 outline-none focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500 disabled:opacity-60"
            disabled={saving || loadingMembers}
          >
            <option value="">
              {loadingMembers ? "会員を読み込み中..." : "会員を選択してください"}
            </option>
            {members.map((m) => (
              <option key={m.id} value={String(m.id)}>
                {m.name}（{m.email}）
              </option>
            ))}
          </select>

          <p className="text-[11px] text-slate-500">
            ※ 一般 & 有効の会員のみ表示（API側でフィルタ）
          </p>
        </div>

        {/* 入金日 */}
        <div className="flex flex-col gap-1">
          <label className="text-[11px] font-medium text-slate-300">入金日</label>
          <input
            value={paymentDate}
            onChange={(e) => setPaymentDate(e.target.value)}
            className="rounded-lg border border-slate-700 bg-slate-950/60 px-3 py-2 text-xs text-slate-100 outline-none focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500"
            placeholder="YYYY-MM-DD"
            disabled={saving}
          />
          <p className="text-[11px] text-slate-500">例：2025-03-10</p>
        </div>

        {/* 入金名義（自動セット + 編集可） */}
        <div className="flex flex-col gap-1">
          <label className="text-[11px] font-medium text-slate-300">入金名義</label>
          <input
            value={payerName}
            onChange={(e) => {
              setPayerName(e.target.value);
              setPayerNameTouched(true);
            }}
            className="rounded-lg border border-slate-700 bg-slate-950/60 px-3 py-2 text-xs text-slate-100 outline-none focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500"
            placeholder="会員を選ぶと自動入力されます（編集可）"
            disabled={saving}
          />
          <p className="text-[11px] text-slate-500">
            ※ 会員名を初期値にします。振込名義が異なる場合は編集してください。
          </p>
        </div>

        {/* 入金額 */}
        <div className="flex flex-col gap-1 md:col-span-2">
          <label className="text-[11px] font-medium text-slate-300">入金額（円）</label>
          <input
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
            className="rounded-lg border border-slate-700 bg-slate-950/60 px-3 py-2 text-xs text-slate-100 outline-none focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500"
            placeholder="例：60000"
            inputMode="numeric"
            disabled={saving}
          />
          <p className="text-[11px] text-slate-500">※ここでは手動入力のみ（CSV取り込みは後で追加）</p>
        </div>

        {/* 入金方法（任意） */}
        <div className="flex flex-col gap-1 md:col-span-2">
          <label className="text-[11px] font-medium text-slate-300">入金方法（任意）</label>
          <select
            value={method}
            onChange={(e) => setMethod(e.target.value)}
            className="rounded-lg border border-slate-700 bg-slate-950/60 px-3 py-2 text-xs text-slate-100 outline-none focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500 disabled:opacity-60"
            disabled={saving}
          >
            <option value="BANK_TRANSFER">振込</option>
            <option value="CASH">現金</option>
            <option value="CARD">クレジットカード</option>
            <option value="OTHER">その他</option>
          </select>
        </div>
      </div>

      {error && (
        <pre className="mt-4 whitespace-pre-wrap rounded-lg border border-rose-500/30 bg-rose-500/10 p-3 text-xs text-rose-200">
          {error}
        </pre>
      )}

      <div className="mt-6 flex flex-wrap items-center justify-end gap-2">
        <button
          type="button"
          onClick={() => router.back()}
          className="rounded-lg border border-slate-600 bg-slate-900 px-4 py-2 text-xs font-medium text-slate-200 hover:bg-slate-800 disabled:opacity-60"
          disabled={saving}
        >
          戻る
        </button>

        <button
          type="button"
          onClick={onSubmit}
          className="rounded-lg bg-emerald-600 px-4 py-2 text-xs font-medium text-slate-50 shadow-sm shadow-emerald-500/30 hover:bg-emerald-500 disabled:opacity-60"
          disabled={saving || !canSave}
          title={!canSave ? "入力内容を確認してください" : undefined}
        >
          {saving ? "登録中..." : "登録して割当へ"}
        </button>
      </div>
    </section>
  );
}
