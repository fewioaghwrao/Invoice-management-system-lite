export default function TermsPage() {
  return (
    <div className="min-h-screen bg-slate-950 text-slate-50">
      <div className="pointer-events-none fixed inset-0 bg-[radial-gradient(circle_at_top,_#0ea5e9_0,_transparent_55%),radial-gradient(circle_at_bottom,_#6366f1_0,_transparent_55%)] opacity-60" />

      <main className="relative z-10 mx-auto max-w-3xl px-4 py-10">
        <h1 className="text-xl font-semibold">利用規約</h1>

        <p className="mt-4 text-xs text-slate-300">
          本利用規約（以下「本規約」）は、本アプリケーション（以下「本アプリ」）の利用条件を定めるものです。
        </p>

        <section className="mt-6 space-y-4 text-xs text-slate-300">
          <div>
            <h2 className="font-semibold text-slate-100">第1条（目的）</h2>
            <p className="mt-1">
              本アプリは、ポートフォリオおよびデモンストレーションを目的として作成されたものであり、
              実際の商取引、請求、決済、契約行為を行うものではありません。
            </p>
          </div>

          <div>
            <h2 className="font-semibold text-slate-100">第2条（利用条件）</h2>
            <p className="mt-1">
              利用者は、本アプリがデモ目的で提供されていることを理解した上で利用するものとします。
              本アプリに入力された情報の正確性、保存性、継続性は保証されません。
            </p>
          </div>

          <div>
            <h2 className="font-semibold text-slate-100">第3条（禁止事項）</h2>
            <ul className="mt-1 list-disc list-inside space-y-1">
              <li>実在する第三者の個人情報や機密情報を入力する行為</li>
              <li>本アプリを用いた実取引・実請求・実決済行為</li>
              <li>本アプリの運営・表示を妨害する行為</li>
            </ul>
          </div>

          <div>
            <h2 className="font-semibold text-slate-100">第4条（免責事項）</h2>
            <p className="mt-1">
              本アプリの利用により生じたいかなる損害についても、
              作成者は一切の責任を負いません。
            </p>
          </div>

          <div>
            <h2 className="font-semibold text-slate-100">第5条（規約の変更）</h2>
            <p className="mt-1">
              本規約の内容は、予告なく変更される場合があります。
            </p>
          </div>
        </section>

        <p className="mt-8 text-[11px] text-slate-400">
          ※ 本ページはポートフォリオ用の簡易利用規約です。
        </p>
      </main>
    </div>
  );
}
