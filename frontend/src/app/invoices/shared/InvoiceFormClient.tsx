"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiGetClient, apiPostClient, apiPutClient } from "@/lib/api.client";

// DB: 1 UNPAID, 2 PARTIAL, 3 PAID, 4 OVERDUE, 5 CANCELLED
type StatusId = 1 | 2 | 3 | 4 | 5;

// 明細行：編集のため id を保持（既存行は id あり / 新規行は id なし）
type Item = { id?: number; name: string; qty: number; unitPrice: number };

type Initial = {
  id?: string;
  invoiceNumber: string;
  memberId: number;
  statusId: number; // 1..5（実質 StatusId）
  invoiceDate: string;
  dueDate: string;
  notes?: string;
  items: Item[];
};

type MemberOption = { id: number; name: string };

// C# UpdateInvoiceRequestDto に合わせる
type UpdateInvoiceRequestDto = {
  invoiceNumber: string;
  invoiceDate: string;
  dueDate: string;
  memberId: number;
  statusId: number;
  remarks?: string;
  lines: {
    id?: number;
    lineNo: number;
    name: string;
    qty: number;
    unitPrice: number;
  }[];
};

function formatCurrency(value: number): string {
  return value.toLocaleString("ja-JP", {
    style: "currency",
    currency: "JPY",
    maximumFractionDigits: 0,
  });
}

export default function InvoiceFormClient(props: {
  mode: "new" | "edit";
  invoiceId?: string;
  from?: string;
  initial?: Initial;
}) {
  const router = useRouter();

  const backHref = props.from ? `/invoices?${props.from}` : "/invoices";
  const title = props.mode === "new" ? "請求書作成" : "請求書編集";

  const [invoiceNumber, setInvoiceNumber] = useState(
    props.initial?.invoiceNumber ?? "INV-NEW"
  );

  // 会員：選択式
  const [members, setMembers] = useState<MemberOption[]>([]);
  const [memberId, setMemberId] = useState<number>(props.initial?.memberId ?? 0);

  // statusId（DBのID）で持つ
  const [statusId, setStatusId] = useState<StatusId>(
    (props.initial?.statusId as StatusId) ?? 1
  );

  const [invoiceDate, setInvoiceDate] = useState(
    props.initial?.invoiceDate ?? "2025-03-01"
  );
  const [dueDate, setDueDate] = useState(props.initial?.dueDate ?? "2025-03-31");
  const [notes, setNotes] = useState(props.initial?.notes ?? "");

  // 編集時は initial.items をそのまま保持（id を落とさない）
  const [itemsArr, setItemsArr] = useState<Item[]>(
    props.initial?.items?.length
      ? props.initial.items
      : [{ id: undefined, name: "", qty: 1, unitPrice: 0 }]
  );

  // =========================
  // ✅ A案：入金済み/キャンセルは閲覧のみ
  // =========================
  const isLocked = props.mode === "edit" && (statusId === 3 || statusId === 5);
  const lockedReason =
    statusId === 3 ? "入金済み" : statusId === 5 ? "キャンセル" : "";

  const subtotal = useMemo(
    () => itemsArr.reduce((sum, it) => sum + it.qty * it.unitPrice, 0),
    [itemsArr]
  );
  const tax = 0;
  const total = subtotal + tax;

  function updateItem(idx: number, patch: Partial<Item>) {
    setItemsArr((prev) =>
      prev.map((it, i) => (i === idx ? { ...it, ...patch } : it))
    );
  }

  // 新規行：id なし（undefined）
  function addItem() {
    if (isLocked) return;
    setItemsArr((prev) => [
      ...prev,
      { id: undefined, name: "", qty: 1, unitPrice: 0 },
    ]);
  }

  function removeItem(idx: number) {
    if (isLocked) return;
    setItemsArr((prev) => prev.filter((_, i) => i !== idx));
  }

  // 会員 options 取得
  useEffect(() => {
    (async () => {
      try {
        const list = await apiGetClient<MemberOption[]>("/api/members/options");
        setMembers(list);

        // 新規作成で memberId が未選択なら先頭を選択
        if ((props.initial?.memberId ?? 0) === 0 && list.length > 0) {
          setMemberId(list[0].id);
        }
      } catch (e) {
        console.error(e);
        alert("会員一覧の取得に失敗しました。/api/members/options を確認してください。");
      }
    })();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const [saving, setSaving] = useState(false);

  async function onSubmit() {
    if (saving) return;

    // ✅ A案：ロック時は保存させない（フロント側の事前ガード）
    if (isLocked) {
      alert(`この請求書は「${lockedReason}」のため編集できません。`);
      return;
    }

    if (!invoiceNumber.trim()) return alert("請求番号を入力してください");
    if (!memberId || memberId <= 0) return alert("会員を選択してください");
    if (itemsArr.some((x) => !x.name.trim())) return alert("明細の項目名が空です");
    if (itemsArr.some((x) => x.qty <= 0)) return alert("数量は1以上にしてください");
    if (itemsArr.some((x) => x.unitPrice < 0)) return alert("単価は0以上にしてください");

    const payload: UpdateInvoiceRequestDto = {
      invoiceNumber: invoiceNumber.trim(),
      invoiceDate,
      dueDate,
      memberId,
      statusId,
      remarks: notes?.trim() ? notes.trim() : undefined,
      lines: itemsArr.map((x, idx) => ({
        id: x.id,
        lineNo: idx + 1,
        name: x.name.trim(),
        qty: Number(x.qty),
        unitPrice: Number(x.unitPrice),
      })),
    };

    try {
      setSaving(true);

      if (props.mode === "edit" && props.invoiceId) {
        await apiPutClient(`/api/invoices/${props.invoiceId}`, payload);
      } else {
        await apiPostClient<void, UpdateInvoiceRequestDto>(`/api/invoices`, payload);
      }

      router.push(backHref);
      router.refresh();
    } catch (e) {
      console.error(e);
      alert("保存に失敗しました。\n\n" + String(e));
    } finally {
      setSaving(false);
    }
  }

  const disableSave = saving || isLocked;

  return (
    <div className="min-h-screen bg-slate-950 text-slate-50">
      <div className="pointer-events-none fixed inset-0 bg-[radial-gradient(circle_at_top,_#0ea5e9_0,_transparent_55%),radial-gradient(circle_at_bottom,_#6366f1_0,_transparent_55%)] opacity-60" />

      <header className="relative z-10 border-b border-slate-800 bg-slate-950/80 backdrop-blur">
        <div className="mx-auto max-w-6xl px-4 sm:px-6 py-4">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <div className="flex items-center gap-2 text-[11px] text-slate-400">
                <Link href="/dashboards/admin" className="hover:text-sky-300">
                  ダッシュボード
                </Link>
                <span>/</span>
                <Link href={backHref} className="hover:text-sky-300">
                  請求書一覧
                </Link>
                <span>/</span>
                <span>{title}</span>
              </div>
              <h1 className="mt-1 text-lg font-semibold">{title}</h1>
              <p className="mt-1 text-xs text-slate-400">
                {props.mode === "new"
                  ? "新規で登録できるフォーム"
                  : isLocked
                  ? "閲覧専用（入金済み/キャンセルのため編集不可）"
                  : "既存データを編集するフォーム"}
              </p>
              <p className="mt-1 text-[11px] text-slate-500">
                小計：{formatCurrency(subtotal)} / 合計：{formatCurrency(total)}
              </p>
            </div>

            <div className="flex flex-wrap items-center gap-2">
              {props.mode === "edit" && props.invoiceId && (
                <Link
                  href={`/invoices/${props.invoiceId}${
                    props.from ? `?from=${encodeURIComponent(props.from)}` : ""
                  }`}
                  className="inline-flex items-center justify-center rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-1.5 text-xs text-slate-200 hover:bg-slate-800"
                >
                  ← 詳細へ
                </Link>
              )}
              <Link
                href={backHref}
                className="inline-flex items-center justify-center rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-1.5 text-xs text-slate-200 hover:bg-slate-800"
              >
                ← 一覧へ
              </Link>

              <button
                onClick={onSubmit}
                disabled={disableSave}
                className={[
                  "inline-flex items-center justify-center rounded-lg px-3 py-1.5 text-xs font-medium shadow-sm",
                  disableSave
                    ? "bg-slate-700 text-slate-300 cursor-not-allowed"
                    : "bg-sky-600 text-slate-50 shadow-sky-500/30 hover:bg-sky-500",
                ].join(" ")}
                title={isLocked ? "入金済み/キャンセルの請求書は編集できません" : ""}
              >
                {saving ? "保存中..." : isLocked ? "保存不可" : "保存"}
              </button>
            </div>
          </div>
        </div>
      </header>

      <main className="relative z-10 mx-auto max-w-6xl px-4 sm:px-6 py-6 sm:py-8 space-y-6">
        {/* ✅ ロック時のバナー（A案の要） */}
        {isLocked && (
          <div className="rounded-2xl border border-amber-400/30 bg-amber-500/10 p-4 text-sm text-amber-200">
            この請求書は <span className="font-semibold">「{lockedReason}」</span>{" "}
            のため編集できません。内容は確認できますが、保存はできません。
          </div>
        )}

        {/* 上段：基本情報 */}
        <section className="rounded-2xl border border-slate-800 bg-slate-900/80 p-4 sm:p-5 shadow-md">
          <h2 className="text-sm font-semibold">基本情報</h2>

          <div className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            <div className="flex flex-col gap-1">
              <label className="text-[11px] font-medium text-slate-300">請求番号</label>
              <input
                value={invoiceNumber}
                onChange={(e) => setInvoiceNumber(e.target.value)}
                disabled={isLocked}
                className="rounded-lg border border-slate-700 bg-slate-950/50 px-3 py-2 text-xs text-slate-100 outline-none focus:border-sky-500 focus:ring-1 focus:ring-sky-500 disabled:opacity-60"
              />
            </div>

            <div className="flex flex-col gap-1">
              <label className="text-[11px] font-medium text-slate-300">会員</label>
              <select
                value={memberId}
                onChange={(e) => setMemberId(Number(e.target.value))}
                disabled={isLocked}
                className="rounded-lg border border-slate-700 bg-slate-950/50 px-3 py-2 text-xs text-slate-100 outline-none focus:border-sky-500 focus:ring-1 focus:ring-sky-500 disabled:opacity-60"
              >
                {members.length === 0 && <option value={0}>読み込み中...</option>}
                {members.map((m) => (
                  <option key={m.id} value={m.id}>
                    {m.name}
                  </option>
                ))}
              </select>
            </div>

            <div className="flex flex-col gap-1">
              <label className="text-[11px] font-medium text-slate-300">ステータス</label>
              <select
                value={statusId}
                onChange={(e) => setStatusId(Number(e.target.value) as StatusId)}
                disabled={isLocked}
                className="rounded-lg border border-slate-700 bg-slate-950/50 px-3 py-2 text-xs text-slate-100 outline-none focus:border-sky-500 focus:ring-1 focus:ring-sky-500 disabled:opacity-60"
              >
                <option value={1}>未入金</option>
                <option value={2}>一部入金</option>
                <option value={3}>入金済み</option>
                <option value={4}>期限超過</option>
                <option value={5}>キャンセル</option>
              </select>
            </div>

            <div className="flex flex-col gap-1">
              <label className="text-[11px] font-medium text-slate-300">請求日</label>
              <input
                type="date"
                value={invoiceDate}
                onChange={(e) => setInvoiceDate(e.target.value)}
                disabled={isLocked}
                className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-xs text-slate-900 outline-none focus:border-sky-500 focus:ring-2 focus:ring-sky-200 disabled:opacity-60"
              />
            </div>

            <div className="flex flex-col gap-1">
              <label className="text-[11px] font-medium text-slate-300">支払期限</label>
              <input
                type="date"
                value={dueDate}
                onChange={(e) => setDueDate(e.target.value)}
                disabled={isLocked}
                className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-xs text-slate-900 outline-none focus:border-sky-500 focus:ring-2 focus:ring-sky-200 disabled:opacity-60"
              />
            </div>
          </div>

          <div className="mt-4">
            <label className="text-[11px] font-medium text-slate-300">備考</label>
            <textarea
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              disabled={isLocked}
              rows={3}
              className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950/50 px-3 py-2 text-xs text-slate-100 outline-none placeholder:text-slate-500 focus:border-sky-500 focus:ring-1 focus:ring-sky-500 disabled:opacity-60"
              placeholder="例：入金条件、担当者メモなど"
            />
          </div>
        </section>

        {/* 明細 */}
        <section className="rounded-2xl border border-slate-800 bg-slate-900/80 p-4 sm:p-5 shadow-md">
          <div className="flex items-center justify-between">
            <h2 className="text-sm font-semibold">請求明細</h2>
            <button
              onClick={addItem}
              disabled={isLocked}
              className={[
                "rounded-lg border px-3 py-1.5 text-xs",
                isLocked
                  ? "border-slate-800 bg-slate-900/50 text-slate-500 cursor-not-allowed"
                  : "border-slate-700 bg-slate-900 text-slate-200 hover:bg-slate-800",
              ].join(" ")}
            >
              ＋ 行追加
            </button>
          </div>

          <div className="mt-4 space-y-3">
            {itemsArr.map((it, idx) => (
              <div
                key={it.id ?? `new-${idx}`}
                className="rounded-xl border border-slate-800 bg-slate-950/40 p-4"
              >
                <div className="grid gap-3 sm:grid-cols-6">
                  <div className="sm:col-span-3">
                    <p className="text-[11px] text-slate-400">項目</p>
                    <input
                      value={it.name}
                      onChange={(e) => updateItem(idx, { name: e.target.value })}
                      disabled={isLocked}
                      className="mt-1 w-full rounded-lg border border-slate-700 bg-slate-950/50 px-3 py-2 text-xs text-slate-100 outline-none focus:border-sky-500 focus:ring-1 focus:ring-sky-500 disabled:opacity-60"
                      placeholder="例：月額利用料（3月）"
                    />
                  </div>

                  <div className="sm:col-span-1">
                    <p className="text-[11px] text-slate-400">数量</p>
                    <input
                      type="number"
                      min={1}
                      value={it.qty}
                      onChange={(e) =>
                        updateItem(idx, { qty: Number(e.target.value || 1) })
                      }
                      disabled={isLocked}
                      className="mt-1 w-full rounded-lg border border-slate-700 bg-slate-950/50 px-3 py-2 text-xs text-slate-100 outline-none focus:border-sky-500 focus:ring-1 focus:ring-sky-500 disabled:opacity-60"
                    />
                  </div>

                  <div className="sm:col-span-2">
                    <p className="text-[11px] text-slate-400">単価</p>
                    <input
                      type="number"
                      min={0}
                      value={it.unitPrice}
                      onChange={(e) =>
                        updateItem(idx, {
                          unitPrice: Number(e.target.value || 0),
                        })
                      }
                      disabled={isLocked}
                      className="mt-1 w-full rounded-lg border border-slate-700 bg-slate-950/50 px-3 py-2 text-xs text-slate-100 outline-none focus:border-sky-500 focus:ring-1 focus:ring-sky-500 disabled:opacity-60"
                    />
                  </div>
                </div>

                <div className="mt-3 flex items-center justify-between">
                  <p className="text-xs text-slate-300">
                    金額：{" "}
                    <span className="font-semibold text-slate-50 tabular-nums">
                      {formatCurrency(it.qty * it.unitPrice)}
                    </span>
                  </p>

                  <button
                    onClick={() => removeItem(idx)}
                    disabled={isLocked || itemsArr.length <= 1}
                    className={[
                      "rounded-lg border px-3 py-1.5 text-xs",
                      isLocked || itemsArr.length <= 1
                        ? "border-slate-800 bg-slate-900/50 text-slate-500 cursor-not-allowed"
                        : "border-red-500/40 bg-red-500/10 text-red-200 hover:bg-red-500/15",
                    ].join(" ")}
                  >
                    行削除
                  </button>
                </div>
              </div>
            ))}
          </div>

          <div className="mt-4 flex justify-end">
            <div className="w-full sm:w-80 rounded-xl border border-slate-800 bg-slate-950/30 p-4 text-xs">
              <div className="flex justify-between text-slate-300">
                <span>小計</span>
                <span className="tabular-nums">{formatCurrency(subtotal)}</span>
              </div>
              <div className="mt-2 flex justify-between text-slate-300">
                <span>税</span>
                <span className="tabular-nums">{formatCurrency(tax)}</span>
              </div>
              <div className="mt-3 flex justify-between text-slate-50 font-semibold">
                <span>合計</span>
                <span className="tabular-nums">{formatCurrency(total)}</span>
              </div>
            </div>
          </div>
        </section>
      </main>
    </div>
  );
}

