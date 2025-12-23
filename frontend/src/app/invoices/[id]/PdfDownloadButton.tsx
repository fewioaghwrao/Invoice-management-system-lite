"use client";

export default function PdfDownloadButton({ invoiceId }: { invoiceId: number }) {
  const onClick = () => {
    const ok = window.confirm("PDFをダウンロードします。よろしいですか？");
    if (!ok) return;

    // 別タブで開く（ブラウザのPDFビューア or ダウンロード）
    window.open(`/api/invoices/${invoiceId}/pdf`, "_blank", "noopener,noreferrer");
  };

  return (
    <button
      type="button"
      onClick={onClick}
      className="inline-flex items-center justify-center rounded-lg border border-slate-700 bg-slate-900 px-3 py-1.5 text-xs font-medium text-slate-200 hover:bg-slate-800 hover:text-sky-200"
      title="PDFをダウンロード"
    >
      PDFダウンロード
    </button>
  );
}
