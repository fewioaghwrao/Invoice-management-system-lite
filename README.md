# Invoice Management System (Lite)

請求書の発行・入金管理を行う **業務向けWebアプリケーション** です。  
管理者と会員の権限を分離し、請求状況・入金状況を一元管理できます。

※ 本リポジトリは **実務を想定した Lite 版** として、  
　コア機能（請求・入金・集計）にフォーカスしています。

---

## デモURL

- **フロントエンド（操作画面）**  
  https://invoice-naoki-app-front-b97fea6c721d.herokuapp.com/auth/login

- **バックエンド API**  
  https://invoice-naoki-app-api-333afef82093.herokuapp.com  
  Health Check: https://invoice-naoki-app-api-333afef82093.herokuapp.com/health

---

## デモアカウント

| 区分 | メールアドレス | パスワード |
|----|----|----|
| 管理者 | admin@example.com | Admin1234! |
| 会員 | member@example.com | Member1234! |

---

## スクリーンショット

### 管理者ダッシュボード
売上・未入金・請求数・回収率などを集約し、請求・入金状況を一目で把握できる管理者向けダッシュボードです。  
![管理者ダッシュボード](docs/screenshots/A-admin-dashboard.png)

---

### 請求書一覧（管理者）
請求番号・会員・ステータス・期間で検索でき、日常的な請求管理業務を想定した一覧画面です。  
![請求書一覧（管理者）](docs/screenshots/B-invoice-list.png)

---

### 請求書詳細・入金状況
請求金額・入金履歴・残額を確認でき、一部入金から完済までの状態遷移を確認できます。  
![請求書詳細・入金状況](docs/screenshots/C-invoice-detail.png)

---

### 入金割当（部分入金対応）
1件の入金を複数の請求書に割り当て可能で、実務を想定した柔軟な入金管理に対応しています。  
![入金割当（部分入金対応）](docs/screenshots/D-payment-allocation.png)

---

### 会員用ダッシュボード（任意）
会員自身が請求書と入金状況を確認できる画面で、管理者と会員の権限分離を実装しています。  
![会員用ダッシュボード](docs/screenshots/F-member-dashboard.png)

---

## 主な機能

### 管理者
- 会員管理（一覧・詳細）
- 請求書の発行・編集
- 請求書PDF出力
- 入金登録（全額／一部）
- 入金状況の自動判定（未入金／一部入金／入金済み）
- 月次・会員別の売上集計
- CSVエクスポート

### 会員
- 自身の請求書一覧・詳細確認
- 支払状況の確認

---

## 技術スタック

### Frontend
- Next.js (App Router)
- TypeScript
- Tailwind CSS

### Backend
- ASP.NET Core (.NET 8)
- Entity Framework Core
- JWT認証

### Database
- PostgreSQL  
  - ローカル：Docker  
  - 本番：Heroku Postgres（マネージドDB）

### Infrastructure
- Frontend：Vercel
- Backend：Heroku

---

## 設計上のポイント

- **フロントエンド / バックエンド分離**
  - APIベースの設計で責務を明確化
- **入金合計からステータスを自動判定**
  - 未入金／一部入金／入金済みをロジックで一元管理
- **レイヤードアーキテクチャ**
  - Domain / Application / Infrastructure を分離し、変更に強い構成

---

## データベース管理方針

- スキーマ管理は **EF Core Migrations** を使用
- 完成SQLファイルは管理せず、差分管理を採用
- 本番DBはマネージドDBのバックアップ機構に依存

---

## リポジトリ構成

```text
invoice-management-system-lite/
├─ frontend/   # Next.js
├─ backend/    # ASP.NET Core
├─ docs/
│  └─ screenshots/
│     ├─ A-admin-dashboard.png
│     ├─ B-invoice-list.png
│     ├─ C-invoice-detail.png
│     ├─ D-payment-allocation.png
│     └─ F-member-dashboard.png
└─ README.md   # 本ドキュメント

```
---

## 補足
- 本アプリは 学習目的ではなく、業務アプリ設計の再現 を目的としています
- 実運用を想定した機能拡張（締め処理、権限拡張など）は Lite 版では省略しています

---
