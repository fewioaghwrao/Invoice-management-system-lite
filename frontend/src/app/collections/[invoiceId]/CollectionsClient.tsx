// src/app/collections/[invoiceId]/CollectionsClient.tsx
"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { apiGetClient, apiPostClient } from "@/lib/api.client";


type Channel = "EMAIL" | "PHONE" | "LETTER";
type Tone = "SOFT" | "NORMAL" | "STRONG";

type InvoiceSnapshot = {
  invoiceId: string;
  invoiceNumber: string;
  memberName: string;
  memberEmail: string;
  invoiceDate: string; // ISO想定
  dueDate: string;     // ISO想定
  total: number;
  paidTotal: number;
};

type DunningLog = {
  id: string;
  at: string; // ISO想定
  channel: Channel;
  title: string;
  memo?: string;
  tone?: Tone;
  nextActionDate?: string; // ISO or yyyy-MM-dd
};

function formatCurrency(value?: number): string {
  return (value ?? 0).toLocaleString("ja-JP", {
    style: "currency",
    currency: "JPY",
    maximumFractionDigits: 0,
  });
}

function formatDateYmd(value?: string): string {
  // "2025-03-10T00:00:00" -> "2025-03-10"
  if (!value) return "-";
  return value.length >= 10 ? value.slice(0, 10) : value;
}

function channelLabel(c: Channel) {
  return c === "EMAIL" ? "メール" : c === "PHONE" ? "電話" : "書面";
}

function toneLabel(t: Tone) {
  return t === "SOFT" ? "ソフト" : t === "NORMAL" ? "標準" : "強め";
}

function buildTemplate(args: {
  tone: Tone;
  name: string;
  invoiceNumber: string;
  dueDate: string; // yyyy-MM-dd
  remaining: number;
}) {
  const { tone, name, invoiceNumber, dueDate, remaining } = args;

  const subject =
    tone === "SOFT"
      ? `【ご確認】お支払い状況のご確認のお願い（${invoiceNumber}）`
      : tone === "NORMAL"
      ? `【重要】お支払いのお願い（${invoiceNumber}）`
      : `【至急】お支払いのお願い（${invoiceNumber}）`;

  const intro =
    tone === "SOFT"
      ? `${name} 様\n\nいつもお世話になっております。`
      : tone === "NORMAL"
      ? `${name} 様\n\nお世話になっております。`
      : `${name} 様\n\n恐れ入りますが、至急ご確認ください。`;

  const body =
    tone === "SOFT"
      ? `下記請求書につきまして、支払期限（${dueDate}）を過ぎている可能性がございます。\n行き違いがございましたら失礼いたしますが、ご入金状況をご確認いただけますでしょうか。`
      : tone === "NORMAL"
      ? `下記請求書につきまして、支払期限（${dueDate}）を過ぎております。\n恐れ入りますが、ご入金状況をご確認のうえ、ご対応をお願いいたします。`
      : `下記請求書につきまして、支払期限（${dueDate}）を過ぎております。\n至急ご対応をお願いいたします。`;

  const footer = `\n\n【請求書番号】${invoiceNumber}\n【未回収残額】${formatCurrency(
    remaining
  )}\n\n本メールと行き違いでご入金済みの場合は、ご容赦ください。\nよろしくお願いいたします。`;

  return { subject, bodyText: `${intro}\n\n${body}${footer}` };
}

export default function CollectionsClient({
  invoiceId,
  from,
}: {
  invoiceId: string;
  from?: string;
}) {
  // --- data state ---
  const [snap, setSnap] = useState<InvoiceSnapshot | null>(null);
  const [logs, setLogs] = useState<DunningLog[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  // --- ui state ---
  const [tone, setTone] = useState<Tone>("NORMAL");
  const [channel, setChannel] = useState<Channel>("EMAIL");
  const [nextActionDate, setNextActionDate] = useState<string>("");

  // ✅ 本文コピー機能：使える環境だけ表示（使えないならUIから消す）
  const canCopy = typeof navigator !== "undefined" && !!navigator.clipboard;

  // 初期ロード
  useEffect(() => {
    console.log("[Collections] mounted invoiceId=", invoiceId);

    let mounted = true;
    (async () => {
      try {
        setLoading(true);
        setErr(null);

        const snapshotPath = `/api/collections/${invoiceId}/snapshot`;
        const logsPath = `/api/collections/${invoiceId}/logs`;

        const [s, l] = await Promise.all([
          apiGetClient<InvoiceSnapshot>(snapshotPath),
          apiGetClient<DunningLog[]>(logsPath),
        ]);

        if (!mounted) return;

        setSnap(s);
        setLogs(l);

        // 次回アクション日の初期値（最新ログがあればそれ / なければ支払期限）
        const lastNext = l?.[0]?.nextActionDate;
        setNextActionDate(formatDateYmd(lastNext ?? s.dueDate ?? ""));
      } catch (e: any) {
        if (!mounted) return;
        console.error("[Collections] load failed:", e);
        setErr(e?.message ?? "読み込みに失敗しました");
      } finally {
        if (!mounted) return;
        setLoading(false);
      }
    })();

    return () => {
      mounted = false;
    };
  }, [invoiceId]);

const remaining = useMemo(() => {
  if (!snap) return 0;
  return Math.max(0, (snap.total ?? 0) - (snap.paidTotal ?? 0));
}, [snap]);

  const tpl = useMemo(() => {
    if (!snap) return { subject: "", bodyText: "" };

    return buildTemplate({
      tone,
      name: snap.memberName,
      invoiceNumber: snap.invoiceNumber,
      dueDate: formatDateYmd(snap.dueDate), // ✅ テンプレ内も yyyy-MM-dd にする
      remaining,
    });
  }, [tone, snap, remaining]);

  const backToInvoicesHref = from ? `/invoices?${from}` : "/invoices";

  // ✅ 送信完了として記録：確認ポップアップを追加
  async function handleRecordSent() {
    if (!snap) return;

    if (remaining <= 0) {
      alert("未回収残額が 0 円のため、督促の記録は不要です。");
      return;
    }

    // ✅ confirm ポップアップ
    const ok = window.confirm(
      [
        "督促処理を受け付けます。よろしいですか？",
        "",
        `請求書：${snap.invoiceNumber}`,
        `チャネル：${channelLabel(channel)}`,
        `トーン：${toneLabel(tone)}`,
        `次回アクション日：${nextActionDate || "-"}`,
        `未回収残額：${formatCurrency(remaining)}`,
      ].join("\n")
    );
    if (!ok) return;

    try {
      setSaving(true);

      const title =
        logs.length === 0
          ? `初回督促（${toneLabel(tone)}）`
          : `督促（${logs.length + 1}回目 / ${toneLabel(tone)}）`;

      const postPath = `/api/collections/${invoiceId}/logs`;
      const payload = {
        channel,
        tone,
        title,
        memo: "テンプレ送付。",
        nextActionDate: nextActionDate || null,
        subject: tpl.subject,
        bodyText: tpl.bodyText,
      };

      await apiPostClient(postPath, payload);

      // 再取得
      const l = await apiGetClient<DunningLog[]>(`/api/collections/${invoiceId}/logs`);
      setLogs(l);

      // ✅ 成功ポップアップ
      alert("督促処理を受け付けました。メール送信はバックグラウンドで処理されます。");
    } catch (e: any) {
      console.error("[Collections] record failed:", e);
      alert(e?.message ?? "記録に失敗しました");
    } finally {
      setSaving(false);
    }
  }

  async function handleCopyBody() {
    if (!canCopy) return;
    try {
      await navigator.clipboard.writeText(tpl.bodyText);
      alert("本文をコピーしました。");
    } catch {
      alert("コピーに失敗しました。");
    }
  }

  if (loading) {
    return (
      <div className="flex min-h-48 w-full min-w-0 items-center justify-center rounded-2xl border border-slate-800 bg-slate-900/70 p-6 text-xs text-slate-400" role="status">
        読み込み中…
      </div>
    );
  }

  if (err || !snap) {
    return (
      <div className="w-full min-w-0 rounded-2xl border border-slate-800 bg-slate-900/80 p-4 text-slate-200 shadow-sm sm:p-6">
        <div className="text-xs">
          <p className="font-semibold">エラー</p>
          <p className="mt-2 text-slate-400">{err ?? "データが取得できません"}</p>
          <Link
            href={backToInvoicesHref}
            className="mt-4 inline-flex text-sky-300 hover:text-sky-200"
          >
            請求書一覧へ戻る
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="w-full min-w-0 space-y-6">
      {/* ページ固有の見出し。共通ヘッダーは collections/layout.tsx が描画します。 */}
      <section aria-label="督促画面ヘッダー" className="min-w-0 rounded-2xl border border-slate-800 bg-slate-900/60 p-4 shadow-sm sm:p-5">
          <div className="flex min-w-0 flex-col gap-4 xl:flex-row xl:items-start xl:justify-between">
            <div className="min-w-0">
              <div className="flex flex-wrap items-center gap-x-2 gap-y-1 text-[11px] text-slate-400">
                <Link href="/dashboards/admin" className="hover:text-sky-300">
                  ダッシュボード
                </Link>
                <span>/</span>
                <Link href={backToInvoicesHref} className="hover:text-sky-300">
                  請求書一覧
                </Link>
                <span>/</span>
                <Link
                  href={`/invoices/${snap.invoiceId}${
                    from ? `?from=${encodeURIComponent(from)}` : ""
                  }`}
                  className="hover:text-sky-300"
                >
                  {snap.invoiceNumber}
                </Link>
                <span>/</span>
                <span>督促</span>
              </div>

              <h1 className="mt-2 break-words text-lg font-semibold leading-snug sm:text-xl">
                督促（{snap.invoiceNumber}）
              </h1>
              <p className="mt-1 break-words text-xs leading-relaxed text-slate-400">
                テンプレ選択 → 文面プレビュー → 履歴管理 → 次アクションまで一画面で管理します。
              </p>
            </div>

            <div className="grid w-full grid-cols-1 gap-2 sm:grid-cols-2 xl:w-auto xl:flex xl:flex-wrap xl:justify-end">
              <Link
                href={`/invoices/${snap.invoiceId}${
                  from ? `?from=${encodeURIComponent(from)}` : ""
                }`}
                className="inline-flex min-h-10 items-center justify-center rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-center text-xs text-slate-200 hover:bg-slate-800"
              >
                ← 請求書詳細へ
              </Link>

              {/* ✅ 本文コピー：使えないなら表示しない（＝削除でよい要件を満たす） */}
              {canCopy && (
                <button
                  onClick={handleCopyBody}
                  className="inline-flex min-h-10 items-center justify-center rounded-lg border border-slate-700 bg-slate-900 px-3 py-2 text-xs text-slate-200 hover:bg-slate-800"
                >
                  本文コピー
                </button>
              )}

              <button
                disabled={saving}
                onClick={handleRecordSent}
                className={[
                  "inline-flex min-h-10 items-center justify-center rounded-lg border border-emerald-500/40 bg-emerald-500/10 px-3 py-2 text-center text-xs font-medium text-emerald-200 hover:bg-emerald-500/15",
                  saving ? "opacity-60 cursor-not-allowed" : "",
                ].join(" ")}
              >
                {saving ? "受付中…" : "督促処理を受け付ける"}
              </button>
            </div>
          </div>
      </section>

      <main className="min-w-0 space-y-6">
        {/* スナップショット */}
        <section className="rounded-2xl border border-slate-800 bg-slate-900/80 p-4 sm:p-5 shadow-md">
          <div className="grid min-w-0 grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-4">
            <div className="rounded-xl border border-slate-800 bg-slate-950/30 p-4">
              <p className="text-[11px] text-slate-400">顧客</p>
              <p className="mt-2 break-words text-sm font-semibold text-slate-100">
                {snap.memberName}
              </p>
              <p className="mt-1 text-[11px] text-slate-400 break-all">
                {snap.memberEmail}
              </p>
            </div>

            <div className="rounded-xl border border-slate-800 bg-slate-950/30 p-4">
              <p className="text-[11px] text-slate-400">支払期限</p>
              {/* ✅ yyyy-MM-dd 表示（時間なし） */}
              <p className="mt-2 text-sm font-semibold text-slate-100">
                {formatDateYmd(snap.dueDate)}
              </p>
              <p className="mt-1 text-[11px] text-slate-400">
                請求日：{formatDateYmd(snap.invoiceDate)}
              </p>
            </div>

            <div className="rounded-xl border border-slate-800 bg-slate-950/30 p-4">
              <p className="text-[11px] text-slate-400">請求合計</p>
              <p className="mt-2 text-sm font-semibold text-slate-100 tabular-nums">
                {formatCurrency(snap.total)}
              </p>
              <p className="mt-1 text-[11px] text-slate-400 tabular-nums">
                入金済：{formatCurrency(snap.paidTotal)}
              </p>
            </div>

            <div className="rounded-xl border border-amber-500/30 bg-amber-500/10 p-4">
              <p className="text-[11px] text-amber-200/90">未回収残額</p>
              <p className="mt-2 text-sm font-semibold text-amber-200 tabular-nums">
                {formatCurrency(remaining)}
              </p>
              <p className="mt-1 text-[11px] text-slate-300">
                次回対応日を決めて追う
              </p>
            </div>
          </div>
        </section>

        <section className="grid min-w-0 grid-cols-1 gap-5 xl:grid-cols-3">
          {/* 左：設定 */}
          <div className="min-w-0 space-y-5 xl:col-span-1">
            <div className="rounded-2xl border border-slate-800 bg-slate-900/80 p-4 sm:p-5">
              <h2 className="text-sm font-semibold">督促設定</h2>

              <div className="mt-4 space-y-4 text-xs">
                <div>
                  <p className="text-[11px] text-slate-400">チャネル</p>
                  <div className="mt-2 grid min-w-0 grid-cols-3 gap-2">
                    {(["EMAIL", "PHONE", "LETTER"] as const).map((c) => (
                      <button
                        key={c}
                        onClick={() => setChannel(c)}
                        className={[
                          "min-w-0 rounded-lg border px-2 py-2 text-xs sm:px-3",
                          channel === c
                            ? "border-sky-500/60 bg-sky-500/10 text-sky-200"
                            : "border-slate-700 bg-slate-950/30 text-slate-200 hover:bg-slate-800",
                        ].join(" ")}
                      >
                        {channelLabel(c)}
                      </button>
                    ))}
                  </div>
                </div>

                <div>
                  <p className="text-[11px] text-slate-400">トーン</p>
                  <div className="mt-2 grid min-w-0 grid-cols-3 gap-2">
                    {(["SOFT", "NORMAL", "STRONG"] as const).map((t) => (
                      <button
                        key={t}
                        onClick={() => setTone(t)}
                        className={[
                          "min-w-0 rounded-lg border px-2 py-2 text-xs sm:px-3",
                          tone === t
                            ? "border-sky-500/60 bg-sky-500/10 text-sky-200"
                            : "border-slate-700 bg-slate-950/30 text-slate-200 hover:bg-slate-800",
                        ].join(" ")}
                      >
                        {toneLabel(t)}
                      </button>
                    ))}
                  </div>
                </div>

                <div>
                  <label className="text-[11px] text-slate-400">
                    次回アクション日
                  </label>
                  <input
                    type="date"
                    value={nextActionDate}
                    onChange={(e) => setNextActionDate(e.target.value)}
                    className="mt-2 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-xs text-slate-900 outline-none focus:border-sky-500 focus:ring-2 focus:ring-sky-200"
                  />
                </div>
              </div>
            </div>
          </div>

          {/* 右：プレビュー＋履歴 */}
          <div className="min-w-0 space-y-5 xl:col-span-2">
            <div className="rounded-2xl border border-slate-800 bg-slate-900/80 p-4 sm:p-5">
              <h2 className="text-sm font-semibold">メール送信文面プレビュー</h2>

              <div className="mt-4 rounded-xl border border-slate-800 bg-slate-950/40 p-4">
                <p className="text-[11px] text-slate-400">件名</p>
                <p className="mt-1 break-words text-xs text-slate-100 [overflow-wrap:anywhere]">
                  {tpl.subject}
                </p>

                <div className="mt-4">
                  <p className="text-[11px] text-slate-400">本文</p>
                  <pre className="mt-2 max-w-full whitespace-pre-wrap break-words text-xs leading-relaxed text-slate-200 [overflow-wrap:anywhere]">
                    {tpl.bodyText}
                  </pre>
                </div>
              </div>
            </div>

            <div className="rounded-2xl border border-slate-800 bg-slate-900/80 p-4 sm:p-5">
              <h2 className="text-sm font-semibold">督促履歴</h2>
              <div className="mt-4 space-y-3">
                {logs.length === 0 ? (
                  <div className="rounded-xl border border-slate-800 bg-slate-950/40 p-4 text-xs text-slate-400">
                    履歴はまだありません。
                  </div>
                ) : (
                  logs.map((l) => (
                    <div
                      key={l.id}
                      className="rounded-xl border border-slate-800 bg-slate-950/40 p-4"
                    >
                      <p className="break-words text-xs text-slate-200 [overflow-wrap:anywhere]">
                        {/* ✅ 履歴日時も yyyy-MM-dd に寄せたいなら formatDateYmd(l.at) にする */}
                        {l.at} / {channelLabel(l.channel)}
                        {l.tone ? ` / ${toneLabel(l.tone)}` : ""}
                        {l.nextActionDate
                          ? ` / 次回: ${formatDateYmd(l.nextActionDate)}`
                          : ""}
                      </p>
                      <p className="mt-1 break-words text-sm font-semibold text-slate-100">
                        {l.title}
                      </p>
                      {l.memo && (
                        <p className="mt-1 break-words text-xs leading-relaxed text-slate-400">{l.memo}</p>
                      )}
                    </div>
                  ))
                )}
              </div>
            </div>
          </div>
        </section>
      </main>
    </div>
  );
}
