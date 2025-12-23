export default function PrivacyPage() {
  return (
    <div className="min-h-screen bg-slate-950 text-slate-50">
      <div className="pointer-events-none fixed inset-0 bg-[radial-gradient(circle_at_top,_#0ea5e9_0,_transparent_55%),radial-gradient(circle_at_bottom,_#6366f1_0,_transparent_55%)] opacity-60" />

      <main className="relative z-10 mx-auto max-w-3xl px-4 py-10">
        <h1 className="text-xl font-semibold">プライバシーポリシー</h1>

        <p className="mt-4 text-xs text-slate-300">
          本プライバシーポリシーは、本アプリにおける情報の取扱いについて定めるものです。
        </p>

        <section className="mt-6 space-y-4 text-xs text-slate-300">
          <div>
            <h2 className="font-semibold text-slate-100">1. 取得する情報</h2>
            <p className="mt-1">
              本アプリでは、画面表示や操作確認を目的として、利用者が任意で入力した情報を扱う場合があります。
              ただし、実在する個人情報の入力は推奨されていません。
            </p>
          </div>

          <div>
            <h2 className="font-semibold text-slate-100">2. 利用目的</h2>
            <p className="mt-1">
              入力された情報は、本アプリのデモ表示および機能確認の目的にのみ利用されます。
            </p>
          </div>

          <div>
            <h2 className="font-semibold text-slate-100">3. 第三者提供</h2>
            <p className="mt-1">
              本アプリでは、入力された情報を第三者に提供することはありません。
            </p>
          </div>

          <div>
            <h2 className="font-semibold text-slate-100">4. 情報の管理</h2>
            <p className="mt-1">
              本アプリはポートフォリオ目的のデモアプリであり、
              入力情報の永続的な保存や管理を保証するものではありません。
            </p>
          </div>

          <div>
            <h2 className="font-semibold text-slate-100">5. 実運用について</h2>
            <p className="mt-1">
              本アプリは商用サービスではなく、実際の業務利用や実データの登録を想定していません。
            </p>
          </div>
        </section>

        <p className="mt-8 text-[11px] text-slate-400">
          ※ 本ページはポートフォリオ用の簡易プライバシーポリシーです。
        </p>
      </main>
    </div>
  );
}
