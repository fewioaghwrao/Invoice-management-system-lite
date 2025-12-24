# Invoice Management System (Lite) – Backend API

本プロジェクトは **Invoice Management System (Lite)** のバックエンド API 実装です。  
請求書・入金・会員情報を管理し、フロントエンドに API を提供します。

---

## 技術スタック

- ASP.NET Core (.NET 8)
- Entity Framework Core
- PostgreSQL
- JWT認証
- QuestPDF（請求書PDF生成）

---

## 主な機能

- 会員管理（管理者 / 会員）
- 請求書管理
- 入金管理（全額 / 一部入金）
- 入金割当（Payment Allocations）
- 請求ステータス自動判定
- 売上集計 API
- CSVエクスポート
- 催促履歴管理

---

## API 設計方針

- REST API ベース
- フロントエンドと完全分離
- 業務ロジックは Application / Domain に集約
- Controller（Endpoint）は薄く保つ構成

---

## ドメイン設計のポイント

- 請求ステータスは **入金合計から自動算出**
- 一部入金・複数請求への割当を考慮し  
  `PaymentAllocations` を中間テーブルとして設計
- 催促履歴を独立テーブルとして保持し、業務ログを明確化

※ ER図・状態遷移図は `/docs` 配下を参照してください。

---

## データベース管理

- EF Core Migrations による差分管理
- 完成 SQL ファイルは管理しない方針
- 本番 DB はマネージド DB のバックアップ機構を利用

---

## 起動方法（ローカル）

```bash
dotnet restore
dotnet ef database update
dotnet run
```
---

## 環境変数例

```bash
ConnectionStrings__Default=Host=localhost;Database=invoice;...
Jwt__Secret=xxxxxxxx
```
---
## 補足

- 本 API は 実務向け業務アプリ設計の再現 を目的としています
- Lite 版のため、締め処理や高度な権限制御は省略しています

