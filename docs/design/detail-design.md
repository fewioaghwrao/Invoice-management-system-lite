# Invoice Management System Lite 詳細設計書

## 1. システム概要

### 1.1 システム名

Invoice Management System Lite

### 1.2 システムの目的

本システムは、中小規模事業者における請求・入金管理業務を想定した業務向けWebアプリケーションである。

請求書の発行、入金登録、入金割当、入金状況の確認、売上集計など、請求・入金管理における中核的な業務をWeb上で管理できるようにすることを目的とする。

従来、Excelや手作業で管理されがちな請求・入金情報を一元管理し、管理者と会員で操作可能な範囲を分離することで、業務の見通しやすさと安全性を高める。

### 1.3 システムの背景

請求・入金管理業務では、以下のような課題が発生しやすい。

* 請求書と入金情報が別々に管理され、入金状況の確認に手間がかかる
* 一部入金や複数請求への入金割当など、Excelでは管理が複雑になりやすい
* 管理者と会員で参照・操作できる情報を分ける必要がある
* 月次売上や未入金状況の集計が手作業になりやすい
* PDF出力やCSV出力など、帳票・集計業務が後回しになりやすい

本システムでは、これらの課題に対して、請求書・入金・入金割当を中心としたデータ設計を行い、入金状況をシステム上で確認できる構成としている。

### 1.4 システムの特徴

本システムの主な特徴は以下のとおりである。

| 項目        | 内容                                  |
| --------- | ----------------------------------- |
| 権限分離      | 管理者と会員を分離し、APIレベルで操作範囲を制御する         |
| 請求管理      | 請求書の一覧、詳細、登録、編集、PDF出力を行う            |
| 入金管理      | 入金情報の登録、入金詳細確認、請求書への割当を行う           |
| 入金ステータス管理 | 入金履歴・入金割当をもとに、未入金・一部入金・入金済みを判定する    |
| 売上集計      | 年月・会員単位で売上や回収状況を確認する                |
| CSV出力     | 売上一覧、会員別売上集計をCSVとして出力する             |
| PDF出力     | 請求書PDFを出力する                         |
| 結合テスト     | Heroku上の本番相当環境で、画面・API・認証・権限分離を確認する |

### 1.5 システム構成概要

本システムは、フロントエンドとバックエンドを分離したWebアプリケーション構成とする。

```text
[ Frontend: Next.js ]
        |
        | REST API / JWT
        |
[ Backend API: ASP.NET Core ]
        |
        | EF Core
        |
[ Database: PostgreSQL ]
```

フロントエンドは画面表示、入力受付、画面遷移、API呼び出しを担当する。

バックエンドは認証・認可、業務ロジック、データベースアクセス、PDF生成、CSV生成を担当する。

データベースには、会員、請求書、請求明細、入金、入金割当、督促履歴、監査ログなどの業務データを保持する。

### 1.6 利用者区分

本システムでは、主に以下の2種類の利用者を想定する。

| 利用者 | 概要                         |
| --- | -------------------------- |
| 管理者 | 会員、請求書、入金、売上集計などを管理する利用者   |
| 会員  | 自身の請求書、支払状況、プロフィールを確認する利用者 |

管理者は全体管理を行い、会員は自身に紐づく情報のみを参照する。

### 1.7 技術スタック

| 区分      | 技術                                  |
| ------- | ----------------------------------- |
| フロントエンド | Next.js / TypeScript / Tailwind CSS |
| バックエンド  | ASP.NET Core / .NET 8 / Minimal API |
| データアクセス | Entity Framework Core               |
| データベース  | PostgreSQL                          |
| 認証方式    | JWT                                 |
| PDF出力   | QuestPDF                            |
| CI      | GitHub Actions                      |
| デプロイ環境  | Heroku / Azure Static Web Apps      |

---

## 2. 対象範囲

### 2.1 本詳細設計書の対象

本詳細設計書では、Invoice Management System Lite における以下の設計を対象とする。

* システム構成
* ディレクトリ構成
* 権限設計
* 画面設計
* API詳細設計
* DB詳細設計
* 業務ロジック詳細
* PDF / CSV出力設計
* 認証・認可設計
* エラーハンドリング
* テスト設計
* CI/CD設計

本書は、実装済みのコードおよび既存ドキュメントをもとに、画面、API、DB、業務ロジックの仕様を整理することを目的とする。

### 2.2 対象機能

本システムで対象とする主な機能は以下のとおりである。

#### 管理者機能

| 機能         | 内容                          |
| ---------- | --------------------------- |
| 管理者ログイン    | 管理者アカウントでログインし、管理者向け機能を利用する |
| 管理者ダッシュボード | 売上、未入金、回収率などの概要を確認する        |
| 会員管理       | 会員の一覧検索、詳細確認、編集、無効化を行う      |
| 請求書管理      | 請求書の一覧検索、詳細確認、登録、編集、削除を行う   |
| 請求書PDF出力   | 請求書をPDFとして出力する              |
| 入金管理       | 入金の一覧検索、登録、詳細確認を行う          |
| 入金割当       | 1件の入金を1件または複数の請求書へ割り当てる     |
| 督促管理       | 請求書に対する督促履歴を登録・確認する         |
| 売上集計       | 年月別、会員別の売上・入金・未回収状況を確認する    |
| CSV出力      | 売上一覧、会員別集計をCSVとして出力する       |
| 操作ログ確認     | 管理者操作の直近ログを確認する             |

#### 会員機能

| 機能          | 内容                          |
| ----------- | --------------------------- |
| 会員登録        | 新規会員として登録する                 |
| メール確認       | メール確認トークンによりメールアドレスを確認する    |
| 会員ログイン      | 会員アカウントでログインし、会員向け機能を利用する   |
| パスワード再設定    | パスワード再設定メールを受け取り、パスワードを更新する |
| 会員ダッシュボード   | 自身の請求・支払状況の概要を確認する          |
| 自分の請求書一覧    | 自身に紐づく請求書一覧を確認する            |
| 自分の請求書詳細    | 自身に紐づく請求書の詳細、入金状況を確認する      |
| 自分の請求書PDF出力 | 自身の請求書PDFを取得する              |
| プロフィール確認・更新 | 自身の会員情報を確認・更新する             |
| 退会          | 自身のアカウントを無効化する              |

### 2.3 対象外機能

本システムは Lite 版として中核機能に絞っているため、以下は本詳細設計書の対象外、または将来拡張扱いとする。

| 対象外・将来拡張   | 内容                     |
| ---------- | ---------------------- |
| 請求締め処理     | 月次締め、確定処理、締め後修正制御など    |
| 自動入金消込     | 銀行明細CSVからの完全自動照合       |
| 請求書再発行履歴   | 再発行履歴、版管理、再発行理由管理      |
| 税率・消費税詳細管理 | 複数税率、税区分、端数処理の詳細制御     |
| メール自動送信    | 請求書送付、督促メールの自動送信       |
| 多拠点・多組織対応  | 複数会社、部署、拠点単位の管理        |
| 外部会計システム連携 | 会計ソフト、銀行API、外部SaaSとの連携 |
| 高度な監査機能    | 差分履歴、承認ワークフロー、詳細な操作追跡  |

### 2.4 設計対象ファイル

本書の作成にあたり、主に以下のファイルを参照する。

| 区分                    | 主な参照ファイル                                                                                                                |
| --------------------- | ----------------------------------------------------------------------------------------------------------------------- |
| 概要・構成                 | `README.md`, `docs/architecture.md`                                                                                     |
| 結合テスト                 | `docs/Integration_test.md`                                                                                              |
| 起動・共通設定               | `backend/InvoiceSystem.Api/Program.cs`                                                                                  |
| API                   | `backend/InvoiceSystem.Api/Endpoints/*.cs`                                                                              |
| ドメインモデル               | `backend/InvoiceSystem.Domain/Entities/*.cs`                                                                            |
| 業務ロジック                | `backend/InvoiceSystem.Application/Services/**/*.cs`, `backend/InvoiceSystem.Infrastructure/Services/**/*.cs`           |
| DTO / Query / Command | `backend/InvoiceSystem.Application/Dtos/**/*.cs`, `Queries/**/*.cs`, `Commands/**/*.cs`                                 |
| フロントエンド画面             | `frontend/src/app/**/*.tsx`, `frontend/src/app/**/route.ts`                                                             |
| API通信                 | `frontend/src/lib/*.ts`, `frontend/src/hooks/*.ts`, `frontend/src/proxy.ts`                                             |
| 図                     | `docs/diagram/er-diagram.drawio.png`, `docs/diagram/admin-diagram.drawio.png`, `docs/diagram/member-diagram.drawio.png` |
| CI                    | `.github/workflows/ci.yml`                                                                                              |

### 2.5 既存図の扱い

以下の図はすでに作成済みのため、本詳細設計書では画像を新規作成せず、該当章で参照する。

* `docs/diagram/er-diagram.drawio.png`
* `docs/diagram/admin-diagram.drawio.png`
* `docs/diagram/member-diagram.drawio.png`

ER図は「DB詳細設計」、管理者・会員の画面遷移図は「画面設計」で参照する。

### 2.6 本書の位置づけ

本書は、READMEやarchitectureで説明している設計方針を、実装ファイル単位の詳細仕様として補足するドキュメントである。

READMEは利用者・閲覧者向けの概要説明、architectureは設計意図の説明、本詳細設計書は実装理解・保守・説明用の技術資料として位置づける。

## 3. DB・エンティティ設計

### 3.1 DB設計方針

本システムでは、請求・入金管理業務に必要な情報を、会員、請求書、請求明細、入金、入金割当、督促履歴、監査ログに分けて管理する。

特に入金ステータスについては、単純なフラグのみで管理するのではなく、請求書に対する入金割当情報をもとに判定できる構成とする。

これにより、以下のような業務ケースに対応する。

* 1件の請求書に対して一部入金がある
* 1件の入金を複数の請求書に割り当てる
* 入金割当を追加・削除し、請求状態を再計算する
* 未入金、一部入金、入金済みなどの状態を管理する
* 督促履歴や操作ログを後から確認できるようにする

DBアクセスには Entity Framework Core を使用し、`AppDbContext` によりエンティティとテーブルの対応、リレーション、インデックス、制約を定義する。

### 3.2 テーブル一覧

| テーブル名                | エンティティ             | 概要                |
| -------------------- | ------------------ | ----------------- |
| Members              | Member             | 管理者・会員情報を管理する     |
| Invoices             | Invoice            | 請求書ヘッダ情報を管理する     |
| InvoiceLines         | InvoiceLine        | 請求書明細を管理する        |
| InvoiceStatuses      | InvoiceStatus      | 請求ステータスのマスタを管理する  |
| Payments             | Payment            | 入金情報を管理する         |
| PaymentAllocations   | PaymentAllocation  | 入金と請求書の割当情報を管理する  |
| PaymentImportBatches | PaymentImportBatch | 入金取込単位を管理する       |
| ReminderHistories    | ReminderHistory    | 督促履歴を管理する         |
| PasswordResetTokens  | PasswordResetToken | パスワード再設定トークンを管理する |
| AuditLogs            | AuditLog           | 管理者操作などの監査ログを管理する |

### 3.3 エンティティ関連概要

主なリレーションは以下のとおりである。

```text id="fwhzqf"
Member 1 ── * Invoice
Member 1 ── * Payment

Invoice 1 ── * InvoiceLine
Invoice 1 ── * PaymentAllocation
Invoice 1 ── * ReminderHistory

Payment 1 ── * PaymentAllocation

PaymentImportBatch 1 ── * Payment

InvoiceStatus 1 ── * Invoice
```

入金管理の中核は `PaymentAllocation` である。

`Payment` は実際に入金された金額を表し、`Invoice` は請求書を表す。
`PaymentAllocation` は、どの入金をどの請求書にいくら割り当てたかを表す中間テーブルである。

この設計により、以下のような実務上の入金管理に対応できる。

* 一部入金
* 複数請求への入金割当
* 入金割当の修正
* 請求書ごとの入金済み金額・残額計算

---

## 3.4 Members テーブル

### 概要

`Members` は、管理者および会員を管理するテーブルである。

管理者と会員は別テーブルに分けず、`Role` により区分する。
会員の有効・無効状態は `IsActive` で管理し、退会時は `Role` を `Disabled` として扱う。

### 主なカラム

| カラム名                            |          型 | 必須  | 内容            |
| ------------------------------- | ---------: | --- | ------------- |
| Id                              |       long | Yes | 会員ID          |
| Name                            |     string | Yes | 氏名            |
| Email                           |     string | Yes | メールアドレス       |
| PostalCode                      |    string? | No  | 郵便番号          |
| Address                         |    string? | No  | 住所            |
| Phone                           |    string? | No  | 電話番号          |
| PasswordHash                    |     string | Yes | ハッシュ化済みパスワード  |
| IsActive                        |       bool | Yes | 有効状態          |
| Role                            | MemberRole | Yes | 会員種別          |
| CreatedAt                       |   DateTime | Yes | 作成日時          |
| UpdatedAt                       |   DateTime | Yes | 更新日時          |
| IsEmailConfirmed                |       bool | Yes | メール確認済みか      |
| EmailVerificationToken          |    string? | No  | メール確認トークン     |
| EmailVerificationTokenExpiresAt |  DateTime? | No  | メール確認トークン有効期限 |

### Role定義

|  値 | 名称       | 内容    |
| -: | -------- | ----- |
|  1 | Admin    | 管理者   |
|  2 | Customer | 一般会員  |
|  9 | Disabled | 退会・無効 |

---

## 3.5 Invoices テーブル

### 概要

`Invoices` は、請求書のヘッダ情報を管理するテーブルである。

会員に紐づく請求情報として、請求番号、請求日、支払期限、請求金額、ステータス、PDFパス、備考などを保持する。

請求書の明細は `InvoiceLines`、入金割当は `PaymentAllocations`、督促履歴は `ReminderHistories` として別テーブルで管理する。

### 主なカラム

| カラム名          |             型 | 必須  | 内容        |
| ------------- | ------------: | --- | --------- |
| Id            |          long | Yes | 請求書ID     |
| MemberId      |          long | Yes | 会員ID      |
| InvoiceNumber |        string | Yes | 請求番号      |
| InvoiceDate   |      DateTime | Yes | 請求日       |
| DueDate       |      DateTime | Yes | 支払期限      |
| TotalAmount   | decimal(18,2) | Yes | 請求合計金額    |
| StatusId      |          long | Yes | 請求ステータスID |
| PdfPath       |       string? | No  | PDF保存パス   |
| Remarks       |       string? | No  | 備考        |
| CreatedAt     |      DateTime | Yes | 作成日時      |
| UpdatedAt     |      DateTime | Yes | 更新日時      |

### 制約・インデックス

| 内容                        | 説明                    |
| ------------------------- | --------------------- |
| InvoiceNumber 一意制約        | 請求番号の重複を防止する          |
| MemberId 外部キー             | Members と関連する         |
| StatusId 外部キー             | InvoiceStatuses と関連する |
| TotalAmount decimal(18,2) | 金額精度を固定する             |

---

## 3.6 InvoiceLines テーブル

### 概要

`InvoiceLines` は、請求書の明細行を管理するテーブルである。

1件の請求書に対して複数の明細行を持つ。
各明細は、表示順、品目名、数量、単価を保持する。

金額は `Qty * UnitPrice` により算出する。

### 主なカラム

| カラム名      |             型 | 必須  | 内容      |
| --------- | ------------: | --- | ------- |
| Id        |          long | Yes | 請求明細ID  |
| InvoiceId |          long | Yes | 請求書ID   |
| LineNo    |           int | Yes | 表示順     |
| Name      |        string | Yes | 品目名     |
| Qty       |           int | Yes | 数量      |
| UnitPrice | decimal(18,2) | Yes | 単価      |
| Amount    |           計算値 | -   | 数量 × 単価 |
| CreatedAt |      DateTime | Yes | 作成日時    |
| UpdatedAt |      DateTime | Yes | 更新日時    |

### 制約・インデックス

| 内容                      | 説明                |
| ----------------------- | ----------------- |
| InvoiceId 外部キー          | Invoices と関連する    |
| InvoiceId + LineNo 一意制約 | 同一請求書内の表示順重複を防止する |
| Name 最大200文字            | 品目名の最大文字数         |
| Invoice 削除時 Cascade     | 請求書削除時に明細も削除する    |

---

## 3.7 InvoiceStatuses テーブル

### 概要

`InvoiceStatuses` は、請求書の状態を表すマスタテーブルである。

未入金、一部入金、入金済み、期限超過、キャンセルなどの表示名・内部コード・完了状態・期限超過状態を保持する。

### 主なカラム

| カラム名      |      型 | 必須  | 内容      |
| --------- | -----: | --- | ------- |
| Id        |   long | Yes | ステータスID |
| Code      | string | Yes | 内部コード   |
| Name      | string | Yes | 表示名     |
| IsOverdue |   bool | Yes | 期限超過系か  |
| IsClosed  |   bool | Yes | 完了状態か   |
| SortOrder |    int | Yes | 表示順     |

### 初期データ

| Id | Code      | Name  | IsOverdue | IsClosed | SortOrder |
| -: | --------- | ----- | --------- | -------- | --------: |
|  1 | UNPAID    | 未入金   | false     | false    |        10 |
|  2 | PARTIAL   | 一部入金  | false     | false    |        20 |
|  3 | PAID      | 入金済み  | false     | true     |        30 |
|  4 | OVERDUE   | 期限超過  | true      | false    |        40 |
|  5 | CANCELLED | キャンセル | false     | true     |        50 |

### 制約・インデックス

| 内容        | 説明                |
| --------- | ----------------- |
| Code 一意制約 | ステータスコードの重複を防止する  |
| HasData   | 初期ステータスマスタをSeedする |

---

## 3.8 Payments テーブル

### 概要

`Payments` は、実際に発生した入金情報を管理するテーブルである。

入金日、入金額、振込名義、入金方法、取込バッチ情報を保持する。
請求書への割当は直接保持せず、`PaymentAllocations` により管理する。

### 主なカラム

| カラム名          |             型 | 必須  | 内容        |
| ------------- | ------------: | --- | --------- |
| Id            |          long | Yes | 入金ID      |
| MemberId      |          long | Yes | 会員ID      |
| PaymentDate   |      DateTime | Yes | 入金日       |
| Amount        | decimal(18,2) | Yes | 入金額       |
| PayerName     |       string? | No  | 振込名義      |
| Method        |       string? | No  | 入金方法      |
| ImportBatchId |         long? | No  | 入金取込バッチID |
| CreatedAt     |      DateTime | Yes | 作成日時      |
| UpdatedAt     |      DateTime | Yes | 更新日時      |

### 制約・インデックス

| 内容                            | 説明                         |
| ----------------------------- | -------------------------- |
| MemberId 外部キー                 | Members と関連する              |
| ImportBatchId 外部キー            | PaymentImportBatches と関連する |
| Amount decimal(18,2)          | 金額精度を固定する                  |
| MemberId + PaymentDate インデックス | 会員別・入金日検索を考慮する             |

---

## 3.9 PaymentAllocations テーブル

### 概要

`PaymentAllocations` は、入金と請求書の割当を管理するテーブルである。

1件の入金をどの請求書にいくら割り当てたかを表す。
本システムの入金ステータス判定、一部入金対応、複数請求への割当対応の中核となる。

### 主なカラム

| カラム名      |             型 | 必須  | 内容     |
| --------- | ------------: | --- | ------ |
| Id        |          long | Yes | 入金割当ID |
| PaymentId |          long | Yes | 入金ID   |
| InvoiceId |          long | Yes | 請求書ID  |
| Amount    | decimal(18,2) | Yes | 割当金額   |

### 制約・インデックス

| 内容                         | 説明                    |
| -------------------------- | --------------------- |
| PaymentId 外部キー             | Payments と関連する        |
| InvoiceId 外部キー             | Invoices と関連する        |
| PaymentId + InvoiceId 一意制約 | 同一入金と同一請求書の重複割当を防止する  |
| InvoiceId インデックス           | 請求書単位の入金合計計算を考慮する     |
| PaymentId インデックス           | 入金単位の割当確認を考慮する        |
| Payment 削除時 Cascade        | 入金削除時に割当も削除する         |
| Invoice 削除時 Restrict       | 入金割当済み請求書の不用意な削除を防止する |

---

## 3.10 PaymentImportBatches テーブル

### 概要

`PaymentImportBatches` は、CSVなどから入金データを取り込む場合の取込単位を管理するテーブルである。

Lite版では手動登録が中心だが、将来的なCSV取込や入金インポート機能を考慮した構成としている。

### 主なカラム

| カラム名       |        型 | 必須  | 内容      |
| ---------- | -------: | --- | ------- |
| Id         |     long | Yes | 取込バッチID |
| Source     |   string | Yes | 取込元     |
| FileName   |  string? | No  | ファイル名   |
| ImportedAt | DateTime | Yes | 取込日時    |

### 補足

`Source` には、CSV、MANUAL などの値を想定する。

---

## 3.11 ReminderHistories テーブル

### 概要

`ReminderHistories` は、請求書に対する督促履歴を管理するテーブルである。

督促日時、方法、文面のトーン、タイトル、メモ、次回対応日、件名、本文などを保持する。

### 主なカラム

| カラム名           |         型 | 必須  | 内容     |
| -------------- | --------: | --- | ------ |
| Id             |      long | Yes | 督促履歴ID |
| InvoiceId      |      long | Yes | 請求書ID  |
| RemindedAt     |  DateTime | Yes | 督促日時   |
| Method         |    string | Yes | 督促方法   |
| Tone           |   string? | No  | 文面トーン  |
| Title          |   string? | No  | 履歴タイトル |
| Note           |   string? | No  | メモ     |
| NextActionDate | DateTime? | No  | 次回対応日  |
| Subject        |   string? | No  | 件名     |
| BodyText       |   string? | No  | 本文     |
| CreatedAt      |  DateTime | Yes | 作成日時   |

### 制約・インデックス

| 内容                  | 説明               |
| ------------------- | ---------------- |
| InvoiceId 外部キー      | Invoices と関連する   |
| Invoice 削除時 Cascade | 請求書削除時に督促履歴も削除する |

---

## 3.12 PasswordResetTokens テーブル

### 概要

`PasswordResetTokens` は、パスワード再設定用のトークンを管理するテーブルである。

会員に紐づく再設定トークン、有効期限、作成日時、使用日時を保持する。

### 主なカラム

| カラム名      |         型 | 必須  | 内容             |
| --------- | --------: | --- | -------------- |
| Id        |      long | Yes | パスワード再設定トークンID |
| MemberId  |      long | Yes | 会員ID           |
| Token     |    string | Yes | 再設定トークン        |
| ExpiresAt |  DateTime | Yes | 有効期限           |
| CreatedAt |  DateTime | Yes | 作成日時           |
| UsedAt    | DateTime? | No  | 使用日時           |

### 制約・インデックス

| 内容                 | 説明              |
| ------------------ | --------------- |
| MemberId 外部キー      | Members と関連する   |
| Token 一意制約         | トークンの重複を防止する    |
| Member 削除時 Cascade | 会員削除時にトークンも削除する |

---

## 3.13 AuditLogs テーブル

### 概要

`AuditLogs` は、管理者操作などの監査ログを保持するテーブルである。

誰が、何に対して、どのような操作を行ったかを記録する。
操作内容、対象エンティティ、概要、詳細JSON、IPアドレス、UserAgentなどを保持する。

### 主なカラム

| カラム名          |        型 | 必須  | 内容         |
| ------------- | -------: | --- | ---------- |
| Id            |     long | Yes | 監査ログID     |
| ActorUserId   |     long | Yes | 操作者ユーザーID  |
| ActorRole     |  string? | No  | 操作者ロール     |
| Action        |   string | Yes | 操作種別       |
| Entity        |   string | Yes | 対象エンティティ   |
| EntityId      |  string? | No  | 対象ID       |
| Summary       |  string? | No  | 操作概要       |
| DataJson      |  string? | No  | 詳細データJSON  |
| CorrelationId |  string? | No  | 相関ID       |
| IpAddress     |  string? | No  | IPアドレス     |
| UserAgent     |  string? | No  | ユーザーエージェント |
| CreatedAt     | DateTime | Yes | 作成日時       |

### 制約・インデックス

| 内容                       | 説明                  |
| ------------------------ | ------------------- |
| CreatedAt インデックス         | 日時順検索を考慮する          |
| Entity + EntityId インデックス | 対象データ単位の検索を考慮する     |
| Action インデックス            | 操作種別検索を考慮する         |
| ActorUserId インデックス       | 操作者単位の検索を考慮する       |
| DataJson text            | 詳細情報をJSON文字列として保持する |

---

## 3.14 DateTime管理方針

`AppDbContext` では、保存時に `DateTime` および `DateTime?` の値をUTCとして正規化する。

`SaveChanges` および `SaveChangesAsync` の実行前に、追加・更新対象のエンティティを走査し、DateTimeのKindに応じて以下のように変換する。

| Kind        | 処理       |
| ----------- | -------- |
| Utc         | そのまま保存する |
| Local       | UTCへ変換する |
| Unspecified | UTCとして扱う |

これにより、PostgreSQL の `timestamp with time zone` とアプリケーション側の日時管理の不整合を抑制する。

---

## 3.15 DB設計上の重要ポイント

### 3.15.1 入金ステータス判定の考え方

請求書の入金状況は、`Invoices.TotalAmount` と `PaymentAllocations.Amount` の合計値を比較して判定する。

| 状態    | 判定条件               |
| ----- | ------------------ |
| 未入金   | 割当済み入金額 = 0        |
| 一部入金  | 0 < 割当済み入金額 < 請求金額 |
| 入金済み  | 割当済み入金額 >= 請求金額    |
| 期限超過  | 支払期限を超過し、かつ未回収額がある |
| キャンセル | 請求自体を無効扱いにする       |

### 3.15.2 PaymentAllocationを分離する理由

入金情報を `Invoices` に直接持たせず、`PaymentAllocation` として分離することで、以下に対応できる。

* 1件の入金を複数請求書へ割り当てる
* 1件の請求書に複数回の入金を割り当てる
* 入金割当を後から修正する
* 入金額、割当額、未割当額を別々に管理する
* 請求書単位の残額を再計算できる

### 3.15.3 論理削除・退会管理

会員の退会は、物理削除ではなく `IsActive = false` および `Role = Disabled` により管理する。

これにより、過去の請求書・入金履歴との整合性を保ったまま、ログインや編集対象から除外できる。

### 3.15.4 監査ログ

重要な管理操作については `AuditLogs` に記録する。

監査ログには、操作者、操作内容、対象エンティティ、対象ID、概要、詳細JSON、IPアドレス、UserAgentなどを保存できる構成とする。

これにより、請求・入金・割当などの重要操作について、後から確認できる余地を残している。

---

## 3.16 ER図

ER図は以下の既存ファイルを参照する。

```text id="i2rvf4"
docs/diagram/er-diagram.drawio.png
```

本詳細設計書では、ER図を概要把握用として使用し、正確なテーブル定義・制約・リレーションについては、Entityクラス、AppDbContext、AppDbContextModelSnapshotを正とする。
