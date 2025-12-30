"use client";

export default function PdfDownloadButton({ invoiceId }: { invoiceId: number }) {
  const onClick = (e: React.MouseEvent<HTMLAnchorElement>) => {
    e.preventDefault();

    const ok = window.confirm("PDFを別タブで開きます。よろしいですか？");
    if (!ok) return;

    // ✅ ポップアップ（別タブ）で開く：ブロックされにくいよう同期イベント内で実行
    window.open(`/invoices/${invoiceId}/pdf`, "_blank", "noopener,noreferrer");
  };

  return (
    <a
      href={`/invoices/${invoiceId}/pdf`} // ポップアップがブロックされた時の保険
      onClick={onClick}
      className="inline-flex items-center justify-center rounded-lg border border-slate-700 bg-slate-900 px-3 py-1.5 text-xs font-medium text-slate-200 hover:bg-slate-800 hover:text-sky-200"
      title="PDFを表示"
    >
      PDFダウンロード
    </a>
  );
}

