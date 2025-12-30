"use client";

import { useMemo, useState } from "react";
import { useSearchParams } from "next/navigation";
import { apiGetBlobClient } from "@/lib/api.client"; // ★追加

type Props = {
  exportPath?: string;
  label?: string;
  estimatedCount?: number; // 対象件数（推定）
  memberName?: string;     // 顧客名（表示用）
  fileNamePrefix?: string; // "sales" / "sales_by_member" など
};

function buildExportQueryFromSearchParams(sp: URLSearchParams) {
  const out = new URLSearchParams();

  const now = new Date();
  out.set("year", sp.get("year") ?? String(now.getFullYear()));

  const month = sp.get("month");
  if (month && month !== "all") out.set("month", month);

  const status = sp.get("status");
  if (status && status !== "all") out.set("status", status);

  const q = sp.get("q");
  if (q) out.set("q", q);

  const memberId = sp.get("memberId");
  if (memberId) out.set("memberId", memberId);

  return out;
}

function downloadBlob(blob: Blob, filename: string) {
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}

export default function CsvExportButton({
  exportPath = "/api/sales/export",
  label = "CSV出力",
  estimatedCount,
  memberName,
  fileNamePrefix = "sales",
}: Props) {
  const sp = useSearchParams();
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(false);

  // query を “文字列” ではなく “URLSearchParams” として保持
  const exportQuery = useMemo(() => {
    return buildExportQueryFromSearchParams(new URLSearchParams(sp.toString()));
  }, [sp]);

  /** ファイル名プレビュー */
  const fileName = useMemo(() => {
    const year = sp.get("year") ?? String(new Date().getFullYear());
    const month = sp.get("month");
    return month && month !== "all"
      ? `${fileNamePrefix}_${year}-${month.padStart(2, "0")}.csv`
      : `${fileNamePrefix}_${year}.csv`;
  }, [sp, fileNamePrefix]);

  const onConfirm = async () => {
    setOpen(false);
    setLoading(true);

    try {
      // apiGetBlobClient は baseUrl + path で叩く想定なので
      // exportPath は "/api/..." の形でOK
      const queryObj: Record<string, string> = {};
      exportQuery.forEach((v, k) => (queryObj[k] = v));

      const { blob, filename } = await apiGetBlobClient(exportPath, queryObj);

      // サーバーが Content-Disposition を返すならそれを優先、無ければプレビュー名
      downloadBlob(blob, filename ?? fileName);
    } catch (e: any) {
      console.error(e);
      alert(e?.message ?? "CSV出力に失敗しました");
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      <button
        type="button"
        onClick={() => setOpen(true)}
        disabled={loading}
        className="inline-flex items-center justify-center gap-2 rounded-lg border border-slate-700 bg-slate-900 px-3 py-2 text-xs text-slate-200 hover:bg-slate-800 disabled:opacity-60"
      >
        {loading ? "出力中…" : label}
      </button>

      {open && (
        <div className="fixed inset-0 z-50">
          <div
            className="absolute inset-0 bg-black/60"
            onClick={() => setOpen(false)}
          />

          <div
            className="absolute left-1/2 top-1/2 w-[min(92vw,520px)]
                       -translate-x-1/2 -translate-y-1/2
                       rounded-2xl border border-slate-800
                       bg-slate-950 p-4 shadow-xl"
          >
            <h3 className="text-sm font-semibold text-slate-50">
              CSVを出力しますか？
            </h3>

            <p className="mt-1 text-[11px] text-slate-400">
              ※ 画面のページングに関係なく{" "}
              <span className="text-slate-200 font-medium">全件出力</span>{" "}
              されます
            </p>

            <div className="mt-3 space-y-2 text-xs">
              <div className="text-slate-300">
                ファイル名：
                <span className="ml-1 font-medium text-slate-50">
                  {fileName}
                </span>
              </div>

              {typeof estimatedCount === "number" && (
                <div className="text-slate-300">
                  対象件数（推定）：
                  <span className="ml-1 font-medium text-slate-50">
                    {estimatedCount.toLocaleString()} 件
                  </span>
                </div>
              )}

              {memberName && (
                <div className="text-slate-300">
                  顧客：
                  <span className="ml-1 font-medium text-slate-50">
                    {memberName}
                  </span>
                </div>
              )}
            </div>

            <div className="mt-4 flex justify-end gap-2">
              <button
                onClick={() => setOpen(false)}
                className="rounded-lg border border-slate-800
                           bg-slate-900/40 px-3 py-2 text-xs
                           text-slate-200 hover:bg-slate-900"
              >
                キャンセル
              </button>
              <button
                onClick={onConfirm}
                className="rounded-lg border border-sky-500/30
                           bg-sky-500/10 px-3 py-2 text-xs
                           text-sky-200 hover:bg-sky-500/20"
              >
                出力する
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}
