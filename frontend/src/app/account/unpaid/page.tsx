import Link from "next/link";
import { apiGetServer } from "@/lib/api.server";

const PAGE_SIZE = 50;

type MyInvoiceListItemDto = {
  id: number;
  invoiceNumber: string;
  invoiceDate: string;
  dueDate: string;
  totalAmount: number;
  paidAmount: number;
  remainingAmount: number;
  statusCode: string;
  statusName: string;
  isOverdue: boolean;
};

type MyInvoiceListResultDto = {
  year: number;
  availableYears: number[];
  month: string;   // "all" or "1".."12"
  status: string;  // "all"|"unpaid"|"partial"|"paid"
  q: string;
  page: number;
  pageSize: number;
  totalCount: number;
  items: MyInvoiceListItemDto[];
};

function formatYmd(iso: string) {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleDateString("ja-JP", { year: "numeric", month: "2-digit", day: "2-digit" });
}

function formatJPY(value: number) {
  return value.toLocaleString("ja-JP", {
    style: "currency",
    currency: "JPY",
    maximumFractionDigits: 0,
  });
}

export default async function UnpaidPage() {
  const year = new Date().getFullYear();
  const month = "all";
  const q = "";
  const page = 1;

  let unpaid: MyInvoiceListResultDto;
  let partial: MyInvoiceListResultDto;
  let error: string | null = null;

  try {
    // 未入金
    unpaid = await apiGetServer<MyInvoiceListResultDto>(
      "/api/members/me/invoices/with-balance",
      {
        year,
        month,
        status: "unpaid",
        q,
        page,
        pageSize: PAGE_SIZE,
      }
    );

    // 一部入金
    partial = await apiGetServer<MyInvoiceListResultDto>(
      "/api/members/me/invoices/with-balance",
      {
        year,
        month,
        status: "partial",
        q,
        page,
        pageSize: PAGE_SIZE,
      }
    );
  } catch (e) {
    error = e instanceof Error ? e.message : "未払い状況の取得に失敗しました。";
    unpaid = {
      year,
      availableYears: [year],
      month,
      status: "unpaid",
      q,
      page: 1,
      pageSize: PAGE_SIZE,
      totalCount: 0,
      items: [],
    };
    partial = {
      year,
      availableYears: [year],
      month,
      status: "partial",
      q,
      page: 1,
      pageSize: PAGE_SIZE,
      totalCount: 0,
      items: [],
    };
  }

  // まとめる（期限が近い順）
const items = [...(unpaid.items ?? []), ...(partial.items ?? [])]
  .sort((a, b) => new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime())
  .slice(0, PAGE_SIZE);

  const unpaidCount = items.length;

  // remainingAmount を正で合計（過不足・念のため）
  const remainingTotal = items.reduce(
    (sum, x) => sum + Math.max(0, Number(x.remainingAmount ?? 0)),
    0
  );

  const overdueCount = items.filter(
    (x) => x.isOverdue && Math.max(0, Number(x.remainingAmount ?? 0)) > 0
  ).length;

  const hasOverdue = overdueCount > 0;

  return (
    <div className="space-y-6">
      {/* 上部導線 */}
      <div className="flex items-center justify-between">
        <div>
          <div className="text-sm font-semibold text-slate-100">
            未入金の確認
          </div>
          <p className="mt-1 text-xs text-slate-400">
            未入金 / 一部入金の請求書をまとめて確認できます。
          </p>
        </div>

        <Link href="/account/invoices" className="text-xs text-slate-300 hover:text-white">
          請求書一覧へ →
        </Link>
      </div>

      {hasOverdue && (
        <div className="rounded-2xl border border-amber-500/30 bg-amber-500/10 p-4 text-amber-200">
          <div className="font-semibold">⚠ お支払いの遅れている請求があります</div>
          <div className="mt-1 text-xs text-amber-200/80">
            期限超過の行にラベルが表示されています。
          </div>
        </div>
      )}

      {error && (
        <div className="rounded-2xl border border-rose-500/30 bg-rose-500/10 p-4 text-rose-200 text-sm">
          {error}
        </div>
      )}

      {/* サマリー */}
      <section className="grid gap-3 sm:grid-cols-3">
        <div className="rounded-2xl border border-slate-800 bg-slate-900/70 p-4">
          <div className="text-[11px] text-slate-400">未払い件数</div>
          <div className="mt-1 text-lg font-semibold text-slate-100">
            {unpaidCount.toLocaleString("ja-JP")} 件
          </div>
        </div>

        <div className="rounded-2xl border border-slate-800 bg-slate-900/70 p-4">
          <div className="text-[11px] text-slate-400">未払い残額合計</div>
          <div className="mt-1 text-lg font-semibold text-slate-100">
            {formatJPY(remainingTotal)}
          </div>
          <div className="mt-1 text-[11px] text-slate-500">
            ※入金割当から算出しております。
          </div>
        </div>

        <div className="rounded-2xl border border-slate-800 bg-slate-900/70 p-4">
          <div className="text-[11px] text-slate-400">期限超過</div>
          <div className="mt-1 text-lg font-semibold text-slate-100">
            {overdueCount.toLocaleString("ja-JP")} 件
          </div>
        </div>
      </section>

      {/* 一覧 */}
      <section className="rounded-2xl border border-slate-800 bg-slate-900/70 shadow-sm overflow-hidden">
        <div className="px-4 py-3 border-b border-slate-800 flex items-center justify-between">
          <div className="text-xs text-slate-300">未払い一覧</div>
          <div className="text-[11px] text-slate-500">※ PDFは新規タブで開きます</div>
        </div>

        <div className="overflow-x-auto">
          <table className="min-w-full text-sm">
            <thead className="bg-slate-950/40 text-slate-300">
              <tr>
                <th className="px-4 py-3 text-left font-semibold">請求書番号</th>
                <th className="px-4 py-3 text-left font-semibold">支払期限</th>
                <th className="px-4 py-3 text-right font-semibold">残額</th>
                <th className="px-4 py-3 text-left font-semibold">ステータス</th>
                <th className="px-4 py-3 text-right font-semibold">操作</th>
              </tr>
            </thead>

            <tbody className="divide-y divide-slate-800">
              {items.length === 0 ? (
                <tr>
                  <td colSpan={5} className="px-4 py-10 text-center text-slate-400">
                    未払いの請求書はありません。
                  </td>
                </tr>
              ) : (
                items.map((inv) => (
                  <tr key={inv.id} className="hover:bg-slate-950/30">
                    <td className="px-4 py-3">
                      <div className="font-semibold text-slate-100">{inv.invoiceNumber}</div>
                      {inv.isOverdue && inv.remainingAmount > 0 && (
                        <div className="mt-1 inline-flex items-center rounded-full bg-amber-500/10 px-2 py-0.5 text-xs font-semibold text-amber-200 border border-amber-500/30">
                          ⚠ 期限超過
                        </div>
                      )}
                    </td>

                    <td className="px-4 py-3 text-slate-200">{formatYmd(inv.dueDate)}</td>

                    <td className="px-4 py-3 text-right font-semibold text-slate-100">
                      {formatJPY(Math.max(0, inv.remainingAmount))}
                    </td>

                    <td className="px-4 py-3 text-slate-200">{inv.statusName}</td>

                    <td className="px-4 py-3">
                      <div className="flex items-center justify-end gap-3">
                        <Link
                          href={`/account/invoices/${inv.id}`}
                          className="text-sky-300 hover:text-white font-semibold"
                        >
                          詳細
                        </Link>

                        <a
                          href={`/account/invoices/${inv.id}/pdf`}
                          target="_blank"
                          rel="noreferrer"
                          className="text-slate-200 hover:text-white"
                          title="PDF表示"
                        >
                          📄 PDF
                        </a>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </section>

    </div>
  );
}
