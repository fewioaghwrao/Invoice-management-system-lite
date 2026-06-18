# Invoice Management System (Lite) – Frontend

本プロジェクトは **Invoice Management System (Lite)** のフロントエンド実装です。  
管理者・会員向けの操作画面を提供し、バックエンド API と連携して  
請求書・入金状況の可視化および操作を行います。

本フロントエンドは、業務アプリにおいて重要となる「情報の一覧性」「誤操作を防ぐ導線」「権限による表示制御」を重視し、実運用を想定した画面構成で設計しています。

---

## 技術スタック

- Next.js (App Router)
- TypeScript
- Tailwind CSS
- Fetch API（Backend API 連携）
- JWT 認証
- Jest
- Testing Library

---

## 主な画面

### 管理者

- 管理者ダッシュボード（売上・未入金・回収率）
- 請求書一覧 / 詳細
- 入金登録・入金割当（部分入金対応）
- 売上集計（CSV エクスポート）
- 会員一覧 / 会員詳細・編集
- 入金一覧 / 入金詳細
- 督促履歴登録
- 操作ログ一覧

### 会員

- 会員用ダッシュボード
- 自身の請求書一覧 / 詳細
- 自身の請求書PDF取得
- 入金状況確認
- プロフィール確認・更新
- 退会

※ 画面遷移の全体像は `/docs` 配下の状態遷移図を参照してください。

---

## フロントエンド設計方針

- App Router + Server Components を基本とした構成
- 認証状態・権限（Admin / Member）による画面制御
- API 呼び出しロジックの共通化
- 業務画面を想定したシンプルで実用重視の UI
- 業務ロジックは保持せず、状態判定・認可はすべて API に委譲

---

## API連携

API のベースURLは `NEXT_PUBLIC_API_BASE_URL` で切り替えます。

フロントエンドは、認証状態やロールに応じて画面表示を制御しますが、最終的な認証・認可判定はバックエンド API 側で実施します。

---

## 環境変数

```env
NEXT_PUBLIC_API_BASE_URL=https://***.com
NEXT_PUBLIC_SHOW_DEMO_LOGIN=true
```

本番ではNEXT_PUBLIC_SHOW_DEMO_LOGINはfalseにする。

---

## 起動方法（ローカル）
```bash
npm install
npm run dev
```

---

## テスト

フロントエンドでは Jest / React Testing Library を使用し、主要画面・共通部品・認証関連処理を中心にテストを実施しています。

主なテスト対象は以下です。

- ログイン画面、パスワード再設定画面
- 管理者ダッシュボード
- 請求書一覧 / 詳細
- 入金一覧 / 入金登録 / 入金詳細
- 会員一覧
- 督促画面
- CurrentUserBadge、LogoutButton などの共通部品
- useCurrentUser などの認証関連Hook

```bash
npm test
```

カバレッジ確認:
```bash
npm run test:coverage
```

---

## 補足

- 本フロントエンドは業務アプリ向け UI 構成を重視しています。
- デザイン性よりも情報量・操作性を優先しています。
- フロントエンドの主要画面・認証関連処理を中心に、継続的にテストを追加しています。
