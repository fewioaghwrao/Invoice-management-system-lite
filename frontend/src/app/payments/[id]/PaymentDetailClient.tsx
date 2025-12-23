"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { apiGetClient, apiPutClient } from "@/lib/api.client";

type PaymentStatus = "UNALLOCATED" | "PARTIAL" | "ALLOCATED";

type PaymentDetailDto = {
  id: number;
  paymentDate: string;
  amount: number;
  payerName?: string;
  method?: string;
  allocatedAmount: number;
  unallocatedAmount: number;
  status: PaymentStatus;
  allocations: Array<{
    invoiceId: number;
    invoiceNumber: string;
    amount: number;
  }>;
};

type InvoiceCandidateDto = {
  id: number;
  invoiceNumber: string;
  memberName?: string;
  totalAmount?: number;
  statusCode?: string; // UNPAID/PARTIAL/PAID 等
};

type AllocationRow = {
  invoiceId: number | null;  // 選択された invoiceId
  invoiceNumber: string;     // 表示用
  invoiceQuery: string;      // 検索入力
  amount: string;            // 入力は文字列
};

function formatCurrency(value: number): string {
  return value.toLocaleString("ja-JP", {
    style: "currency",
    currency: "JPY",
    maximumFractionDigits: 0,
  });
}

function statusLabel(s: PaymentStatus) {
  if (s === "ALLOCATED") return "割当済";
  if (s === "PARTIAL") return "一部割当";
  return "未割当";
}
function statusPillClass(s: PaymentStatus) {
  if (s === "ALLOCATED") return "bg-emerald-500/10 text-emerald-300 border-emerald-500/30";
  if (s === "PARTIAL") return "bg-sky-500/10 text-sky-300 border-sky-500/30";
  return "bg-amber-500/10 text-amber-300 border-amber-500/30";
}

export default function PaymentDetailClient({ paymentId }: { paymentId: string }) {
  const router = useRouter();

  const [payment, setPayment] = useState<PaymentDetailDto | null>(null);
  const [rows, setRows] = useState<AllocationRow[]>([
    { invoiceId: null, invoiceNumber: "", invoiceQuery: "", amount: "" },
  ]);

  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // 候補（行ごと）
  const [candidatesByRow, setCandidatesByRow] = useState<Record<number, InvoiceCandidateDto[]>>({});
  const [loadingCandRow, setLoadingCandRow] = useState<Record<number, boolean>>({});
  const searchTimersRef = useRef<Record<number, number | null>>({}); // idx -> timerId
  const lastQueryRef = useRef<Record<number, string>>({});          // idx -> last query

  // 初期ロード
  useEffect(() => {
    (async () => {
      setError(null);
      try {
        const dto = await apiGetClient<PaymentDetailDto>(`/api/payments/${Number(paymentId)}`);
        setPayment(dto);

        if (dto.allocations.length > 0) {
          setRows(
            dto.allocations.map((a) => ({
              invoiceId: a.invoiceId,
              invoiceNumber: a.invoiceNumber,
              invoiceQuery: a.invoiceNumber, // 表示は番号
              amount: String(a.amount),
            }))
          );
        } else {
          setRows([{ invoiceId: null, invoiceNumber: "", invoiceQuery: "", amount: "" }]);
        }
      } catch (e: any) {
        setError(e?.message ?? "取得に失敗しました。");
        setPayment(null);
      }
    })();
  }, [paymentId]);

  // 行追加・削除
  const addRow = () =>
    setRows((p) => [...p, { invoiceId: null, invoiceNumber: "", invoiceQuery: "", amount: "" }]);

  const removeRow = (idx: number) => {
    setRows((p) => p.filter((_, i) => i !== idx));
    // タイマー掃除（念のため）
    const t = searchTimersRef.current[idx];
    if (t) window.clearTimeout(t);
    delete searchTimersRef.current[idx];
    delete lastQueryRef.current[idx];

    setCandidatesByRow((p) => {
      const copy = { ...p };
      delete copy[idx];
      return copy;
    });
    setLoadingCandRow((p) => {
      const copy = { ...p };
      delete copy[idx];
      return copy;
    });
  };

  const updateRow = (idx: number, patch: Partial<AllocationRow>) =>
    setRows((p) => p.map((r, i) => (i === idx ? { ...r, ...patch } : r)));

  const chooseInvoice = (idx: number, inv: InvoiceCandidateDto) => {
    updateRow(idx, {
      invoiceId: inv.id,
      invoiceNumber: inv.invoiceNumber,
      invoiceQuery: inv.invoiceNumber,
    });
    setCandidatesByRow((p) => ({ ...p, [idx]: [] }));
  };

  const clearInvoice = (idx: number) => {
    updateRow(idx, { invoiceId: null, invoiceNumber: "", invoiceQuery: "" });
    setCandidatesByRow((p) => ({ ...p, [idx]: [] }));
  };

  // ✅ 検索（debounce）: rows の invoiceQuery を監視して、行ごとにタイマー検索
  useEffect(() => {
    // rows が変わるたびに、各行の query を確認し必要ならタイマーを張り直す
    rows.forEach((r, idx) => {
      const q = (r.invoiceQuery ?? "").trim();

      // 既に選択済みで、queryがinvoiceNumberに一致してるなら検索しない（好み）
      if (r.invoiceId && q === r.invoiceNumber) {
        setCandidatesByRow((p) => ({ ...p, [idx]: [] }));
        return;
      }

      // 短すぎるなら候補消す＆検索しない
      if (q.length < 2) {
        // 既存タイマーがあれば消す
        const t = searchTimersRef.current[idx];
        if (t) window.clearTimeout(t);
        searchTimersRef.current[idx] = null;

        lastQueryRef.current[idx] = q;
        setCandidatesByRow((p) => ({ ...p, [idx]: [] }));
        setLoadingCandRow((p) => ({ ...p, [idx]: false }));
        return;
      }

      // 同じクエリで二重予約しない
      if (lastQueryRef.current[idx] === q) return;

      // 既存タイマーがあれば張り替え
      const old = searchTimersRef.current[idx];
      if (old) window.clearTimeout(old);

      setLoadingCandRow((p) => ({ ...p, [idx]: true }));

      const timerId = window.setTimeout(async () => {
        try {
          lastQueryRef.current[idx] = q;

          const url = `/api/invoices?keyword=${encodeURIComponent(q)}&status=UNPAID,PARTIAL&page=1&pageSize=20`;
          const result = await apiGetClient<any>(url);

          const list: any[] = Array.isArray(result)
            ? result
            : Array.isArray(result?.rows)
              ? result.rows
              : [];

          const normalized: InvoiceCandidateDto[] = list
            .map((x) => ({
              id: Number(x.id),
              invoiceNumber: String(x.invoiceNumber ?? ""),
              memberName: x.memberName ? String(x.memberName) : undefined,
              totalAmount: Number.isFinite(Number(x.totalAmount)) ? Number(x.totalAmount) : undefined,
              statusCode: x.statusCode ? String(x.statusCode) : undefined,
            }))
            .filter((x) => Number.isFinite(x.id) && x.id > 0 && x.invoiceNumber);

          setCandidatesByRow((p) => ({ ...p, [idx]: normalized }));
        } catch {
          setCandidatesByRow((p) => ({ ...p, [idx]: [] }));
        } finally {
          setLoadingCandRow((p) => ({ ...p, [idx]: false }));
        }
      }, 250);

      searchTimersRef.current[idx] = timerId;
    });

    // unmount 時に全タイマー掃除
    return () => {
      Object.values(searchTimersRef.current).forEach((t) => {
        if (t) window.clearTimeout(t);
      });
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [rows]);

  const allocatedSum = useMemo(() => {
    return rows.reduce((sum, r) => {
      const amt = Number(r.amount);
      if (!Number.isFinite(amt) || amt <= 0) return sum;
      return sum + amt;
    }, 0);
  }, [rows]);

  const issues = useMemo(() => {
    const x: string[] = [];
    if (!payment) return x;

    const validLines = rows.filter((r) => r.invoiceQuery.trim() || r.amount.trim());
    for (const [i, r] of validLines.entries()) {
      if (!r.invoiceId) x.push(`割当行${i + 1}: 請求書を選択してください。`);

      const amt = Number(r.amount);
      if (!r.amount.trim()) x.push(`割当行${i + 1}: 金額は必須です。`);
      if (r.amount.trim() && (!Number.isFinite(amt) || amt <= 0)) x.push(`割当行${i + 1}: 金額は1以上の数値。`);
    }

    // 重複禁止（推奨）
    const ids = validLines
      .map((r) => r.invoiceId)
      .filter((v): v is number => typeof v === "number");
    const dup = ids.find((id, idx) => ids.indexOf(id) !== idx);
    if (dup) x.push("同じ請求書が複数行で選択されています（1行にまとめてください）。");

    if (allocatedSum > payment.amount) x.push("割当合計が入金額を超えています。");
    return x;
  }, [rows, allocatedSum, payment]);

  const saveAllocation = async () => {
    setError(null);
    if (!payment) return;

    if (issues.length > 0) {
      setError(issues.join("\n"));
      return;
    }

    const ok = window.confirm("割当内容を保存します。よろしいですか？");
    if (!ok) return;

    setSaving(true);
    try {
      const lines = rows
        .map((r) => ({
          invoiceId: r.invoiceId ?? 0,
          amount: Number(r.amount),
        }))
        .filter((x) => Number.isFinite(x.invoiceId) && x.invoiceId > 0 && Number.isFinite(x.amount) && x.amount > 0);

      await apiPutClient(`/api/payments/${payment.id}/allocations`, { lines });

      const dto = await apiGetClient<PaymentDetailDto>(`/api/payments/${payment.id}`);
      setPayment(dto);
      setRows(
        dto.allocations.length
          ? dto.allocations.map((a) => ({
              invoiceId: a.invoiceId,
              invoiceNumber: a.invoiceNumber,
              invoiceQuery: a.invoiceNumber,
              amount: String(a.amount),
            }))
          : [{ invoiceId: null, invoiceNumber: "", invoiceQuery: "", amount: "" }]
      );

      router.refresh();
      alert("保存しました。");
    } catch (e: any) {
      setError(e?.message ?? "保存に失敗しました。");
    } finally {
      setSaving(false);
    }
  };

  if (!payment) {
    return (
      <section className="rounded-2xl border border-slate-800 bg-slate-900/80 p-5 shadow-md">
        <p className="text-sm text-slate-300">データ取得中/または見つかりません。</p>
        {error && <pre className="mt-3 whitespace-pre-wrap text-xs text-rose-200">{error}</pre>}
      </section>
    );
  }

  return (
    <div className="space-y-6">
      {/* サマリー */}
      <section className="rounded-2xl border border-slate-800 bg-slate-900/80 p-5 shadow-md">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <div>
            <p className="text-xs text-slate-400">入金ID</p>
            <p className="text-base font-semibold">{payment.id}</p>
          </div>

          <span className={`inline-flex items-center rounded-full border px-2 py-0.5 text-[11px] ${statusPillClass(payment.status)}`}>
            {statusLabel(payment.status)}
          </span>
        </div>

        <div className="mt-4 grid gap-3 md:grid-cols-3">
          <div className="rounded-xl border border-slate-800 bg-slate-950/30 p-4">
            <p className="text-[11px] text-slate-400">入金額</p>
            <p className="mt-2 text-xl font-semibold text-emerald-300">{formatCurrency(payment.amount)}</p>
          </div>
          <div className="rounded-xl border border-slate-800 bg-slate-950/30 p-4">
            <p className="text-[11px] text-slate-400">割当合計</p>
            <p className="mt-2 text-xl font-semibold">{formatCurrency(payment.allocatedAmount)}</p>
            <p className="mt-1 text-[11px] text-slate-500">（入力中合計: {formatCurrency(allocatedSum)}）</p>
          </div>
          <div className="rounded-xl border border-slate-800 bg-slate-950/30 p-4">
            <p className="text-[11px] text-slate-400">未割当</p>
            <p className="mt-2 text-xl font-semibold text-amber-300">{formatCurrency(payment.unallocatedAmount)}</p>
          </div>
        </div>

        <div className="mt-4 grid gap-3 md:grid-cols-3 text-xs text-slate-200">
          <div className="rounded-xl border border-slate-800 bg-slate-950/30 p-4">
            <p className="text-[11px] text-slate-400">入金日</p>
            <p className="mt-2 font-medium">{payment.paymentDate.slice(0, 10)}</p>
          </div>
          <div className="rounded-xl border border-slate-800 bg-slate-950/30 p-4 md:col-span-2">
            <p className="text-[11px] text-slate-400">入金名義</p>
            <p className="mt-2 font-medium">{payment.payerName ?? "-"}</p>
          </div>
        </div>
      </section>

      {/* 割当入力 */}
      <section className="rounded-2xl border border-slate-800 bg-slate-900/80 p-5 shadow-md">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <div>
            <h2 className="text-sm font-semibold text-slate-100">請求書への割当</h2>
            <p className="mt-1 text-[11px] text-slate-400">請求書番号で検索して選択してください</p>
          </div>

          <button
            type="button"
            onClick={addRow}
            className="rounded-lg border border-slate-600 bg-slate-900 px-3 py-1.5 text-xs font-medium text-slate-200 hover:bg-slate-800"
            disabled={saving}
          >
            + 行を追加
          </button>
        </div>

        <div className="mt-4 space-y-3">
          {rows.map((r, idx) => {
            const cands = candidatesByRow[idx] ?? [];
            const loading = !!loadingCandRow[idx];

            return (
              <div key={idx} className="grid gap-2 rounded-xl border border-slate-800 bg-slate-950/30 p-3 md:grid-cols-12">
                <div className="md:col-span-7">
                  <label className="text-[11px] font-medium text-slate-300">請求書（検索して選択）</label>

                  <div className="relative mt-1">
                    <input
                      value={r.invoiceQuery}
                      onChange={(e) => {
                        updateRow(idx, { invoiceQuery: e.target.value, invoiceId: null, invoiceNumber: "" });
                      }}
                      className="w-full rounded-lg border border-slate-700 bg-slate-950/60 px-3 py-2 pr-20 text-xs text-slate-100 outline-none focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500"
                      placeholder="例：INV-2025 / 会員名（2文字以上）"
                      disabled={saving}
                    />

                    {r.invoiceId ? (
                      <button
                        type="button"
                        onClick={() => clearInvoice(idx)}
                        className="absolute right-2 top-1/2 -translate-y-1/2 rounded-md border border-slate-700 bg-slate-900/60 px-2 py-1 text-[11px] text-slate-200 hover:bg-slate-800"
                        disabled={saving}
                      >
                        解除
                      </button>
                    ) : (
                      <span className="absolute right-2 top-1/2 -translate-y-1/2 text-[11px] text-slate-500">
                        {loading ? "検索中…" : ""}
                      </span>
                    )}

                    {cands.length > 0 && !saving && (
                      <div className="absolute z-20 mt-1 w-full overflow-hidden rounded-lg border border-slate-700 bg-slate-950/95 shadow-lg">
                        {cands.map((inv) => (
                          <button
                            key={inv.id}
                            type="button"
                            onClick={() => chooseInvoice(idx, inv)}
                            className="flex w-full items-center justify-between gap-3 px-3 py-2 text-left text-xs text-slate-100 hover:bg-slate-900"
                          >
                            <div className="min-w-0">
                              <div className="truncate font-medium">{inv.invoiceNumber}</div>
                              <div className="truncate text-[11px] text-slate-400">
                                {inv.memberName ? `会員: ${inv.memberName}` : ""}
                                {inv.statusCode ? ` / ${inv.statusCode}` : ""}
                              </div>
                            </div>
                            {typeof inv.totalAmount === "number" ? (
                              <div className="shrink-0 text-[11px] text-slate-300">{formatCurrency(inv.totalAmount)}</div>
                            ) : null}
                          </button>
                        ))}
                      </div>
                    )}
                  </div>

                  <div className="mt-1 text-[11px] text-slate-500">
                    {r.invoiceId ? (
                      <span>
                        選択中: <span className="text-slate-200">{r.invoiceNumber}</span>（ID:{r.invoiceId}）
                      </span>
                    ) : (
                      <span>※ 候補から選択してください</span>
                    )}
                  </div>
                </div>

                <div className="md:col-span-3">
                  <label className="text-[11px] font-medium text-slate-300">割当金額（円）</label>
                  <input
                    value={r.amount}
                    onChange={(e) => updateRow(idx, { amount: e.target.value })}
                    className="mt-1 w-full rounded-lg border border-slate-700 bg-slate-950/60 px-3 py-2 text-xs text-slate-100 outline-none focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500"
                    placeholder="例：45000"
                    inputMode="numeric"
                    disabled={saving}
                  />
                </div>

                <div className="md:col-span-2 flex items-end justify-end">
                  <button
                    type="button"
                    onClick={() => removeRow(idx)}
                    className="h-9 rounded-lg border border-slate-700 bg-slate-900/60 px-3 text-xs text-slate-200 hover:bg-slate-800 disabled:opacity-60"
                    disabled={saving || rows.length === 1}
                  >
                    削除
                  </button>
                </div>
              </div>
            );
          })}
        </div>

        {error && (
          <pre className="mt-4 whitespace-pre-wrap rounded-lg border border-rose-500/30 bg-rose-500/10 p-3 text-xs text-rose-200">
            {error}
          </pre>
        )}

        <div className="mt-5 flex flex-wrap items-center justify-between gap-2">
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
            onClick={saveAllocation}
            className="rounded-lg bg-emerald-600 px-4 py-2 text-xs font-medium text-slate-50 shadow-sm shadow-emerald-500/30 hover:bg-emerald-500 disabled:opacity-60"
            disabled={saving}
            title={issues.length ? "入力内容を確認してください" : undefined}
          >
            {saving ? "保存中..." : "割当を保存"}
          </button>
        </div>

        {issues.length > 0 && (
          <div className="mt-3 rounded-lg border border-slate-800 bg-slate-950/30 p-3">
            <p className="text-[11px] font-medium text-slate-300">入力チェック</p>
            <ul className="mt-2 list-disc pl-5 text-[11px] text-slate-400">
              {issues.map((m, i) => (
                <li key={i}>{m}</li>
              ))}
            </ul>
          </div>
        )}
      </section>
    </div>
  );
}
