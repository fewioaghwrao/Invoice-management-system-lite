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
| バックエンド  | ASP.NET Core / .NET 10 / Minimal API |
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
| バックエンド単体テスト | `backend/InvoiceSystem.Tests/Services/**/*.cs` |
| バックエンド統合テスト | `backend/InvoiceSystem.Tests/Integration/**/*.cs` |

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
| ReminderJobs         | ReminderJob        | 督促メール等の非同期送信ジョブを管理する |

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
Invoice 1 ── * ReminderJob
Member 1 ── * PasswordResetToken
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

## 3.12 ReminderJobs テーブル

### 概要

`ReminderJobs` は、督促メール等の送信処理を非同期で実行するためのジョブを管理するテーブルである。

督促履歴の登録とは別に、送信対象、件名、本文、処理状態、リトライ回数、エラー内容、開始・完了日時を保持する。

### 主なカラム

| カラム名 | 型 | 必須 | 内容 |
| --- | ---: | --- | --- |
| Id | long | Yes | 督促ジョブID |
| InvoiceId | long | Yes | 請求書ID |
| ToEmail | string | Yes | 送信先メールアドレス |
| Subject | string | Yes | 件名 |
| Body | string | Yes | 本文 |
| Status | string | Yes | 処理状態 |
| RetryCount | int | Yes | リトライ回数 |
| ErrorMessage | string? | No | エラー内容 |
| CreatedAt | DateTime | Yes | 作成日時 |
| StartedAt | DateTime? | No | 処理開始日時 |
| CompletedAt | DateTime? | No | 処理完了日時 |

### 制約・インデックス

| 内容 | 説明 |
| --- | --- |
| InvoiceId 外部キー | Invoices と関連する |
| Invoice 削除時 Restrict | ジョブ履歴との整合性を保つ |
| ToEmail 最大256文字 | メールアドレス長を制限する |
| Subject 最大200文字 | 件名長を制限する |
| Status 最大30文字 | 処理状態の表現を制限する |
| ErrorMessage 最大2000文字 | エラー内容の保存上限を設ける |

---

## 3.13 PasswordResetTokens テーブル

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

## 3.14 AuditLogs テーブル

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

## 3.15 DateTime管理方針

`AppDbContext` では、保存時に `DateTime` および `DateTime?` の値をUTCとして正規化する。

`SaveChanges` および `SaveChangesAsync` の実行前に、追加・更新対象のエンティティを走査し、DateTimeのKindに応じて以下のように変換する。

| Kind        | 処理       |
| ----------- | -------- |
| Utc         | そのまま保存する |
| Local       | UTCへ変換する |
| Unspecified | UTCとして扱う |

これにより、PostgreSQL の `timestamp with time zone` とアプリケーション側の日時管理の不整合を抑制する。

---

## 3.16 DB設計上の重要ポイント

### 3.16.1 入金ステータス判定の考え方

請求書の入金状況は、`Invoices.TotalAmount` と `PaymentAllocations.Amount` の合計値を比較して判定する。

| 状態    | 判定条件               |
| ----- | ------------------ |
| 未入金   | 割当済み入金額 = 0        |
| 一部入金  | 0 < 割当済み入金額 < 請求金額 |
| 入金済み  | 割当済み入金額 >= 請求金額    |
| 期限超過  | 支払期限を超過し、かつ未回収額がある |
| キャンセル | 請求自体を無効扱いにする       |

### 3.16.2 PaymentAllocationを分離する理由

入金情報を `Invoices` に直接持たせず、`PaymentAllocation` として分離することで、以下に対応できる。

* 1件の入金を複数請求書へ割り当てる
* 1件の請求書に複数回の入金を割り当てる
* 入金割当を後から修正する
* 入金額、割当額、未割当額を別々に管理する
* 請求書単位の残額を再計算できる

### 3.16.3 論理削除・退会管理

会員の退会は、物理削除ではなく `IsActive = false` および `Role = Disabled` により管理する。

これにより、過去の請求書・入金履歴との整合性を保ったまま、ログインや編集対象から除外できる。

### 3.16.4 監査ログ

重要な管理操作については `AuditLogs` に記録する。

監査ログには、操作者、操作内容、対象エンティティ、対象ID、概要、詳細JSON、IPアドレス、UserAgentなどを保存できる構成とする。

これにより、請求・入金・割当などの重要操作について、後から確認できる余地を残している。

---

## 3.17 ER図

ER図は以下の既存ファイルを参照する。

```text id="i2rvf4"
docs/diagram/er-diagram.drawio.png
```

本詳細設計書では、ER図を概要把握用として使用し、正確なテーブル定義・制約・リレーションについては、Entityクラス、AppDbContext、AppDbContextModelSnapshotを正とする。

## 4. 業務ロジック設計

### 4.1 業務ロジック設計方針

本システムでは、画面やAPIエンドポイントに業務判断を直接記述せず、主な業務処理をService層に集約する。

Service層は、Application層で定義したInterfaceを実装し、Infrastructure層でEntity Framework Coreを利用してデータベースアクセスを行う。

主な業務ロジックは以下のServiceに分けて実装する。

| Service | 主な責務 |
| --- | --- |
| InvoiceService | 請求書の登録、更新、検索、詳細取得、削除、PDF生成、自分の請求書取得 |
| PaymentService | 入金登録、入金検索、入金詳細取得、入金割当、割当削除、請求ステータス再計算 |
| CollectionService | 督促対象請求書のスナップショット取得、督促履歴取得、督促履歴登録 |
| SalesService | 売上一覧、会員別売上集計、CSV出力用データ取得 |
| AdminSummaryService | 管理者ダッシュボード向けの年間サマリー取得 |
| AuditLogger | 管理者操作・入金割当操作などの監査ログ記録 |
| AdminOperationLogService | 直近の管理者操作ログ取得 |
| ReminderJobProcessor | 督促送信ジョブの取得、送信処理、成功・失敗状態の更新 |
| ReminderJobWorker | 未処理の督促ジョブを定期的に処理するバックグラウンドサービス |

業務ロジックでは、請求金額や入金済み金額などの重要な値を、クライアント側の入力値に依存せず、サーバー側で再計算する方針とする。

### 4.2 請求書管理ロジック

#### 4.2.1 概要

請求書管理ロジックは `InvoiceService` が担当する。

`IInvoiceService` では、請求書作成、詳細取得、一覧検索、ステータス更新、PDF生成、明細付き作成、更新、自分の請求書一覧取得、所有者取得などの操作を定義している。

#### 4.2.2 主な処理

| 処理 | メソッド | 内容 |
| --- | --- | --- |
| 請求書作成 | CreateAsync | 会員存在確認後、請求書を作成する |
| 明細付き請求書作成 | CreateWithLinesAsync | 請求ヘッダと請求明細を登録し、合計金額をサーバー側で再計算する |
| 請求書一覧検索 | SearchAsync | 請求番号、会員名、請求日、ステータスで検索する |
| 請求書詳細取得 | GetDetailByIdAsync | 請求ヘッダ、明細、入金割当、督促履歴を含めて取得する |
| 請求書更新 | UpdateAsync | ヘッダ情報と明細行を差分更新する |
| 請求書削除 | DeleteAsync | 入金割当が存在しない場合のみ削除する |
| 会員向け請求書一覧 | SearchMyInvoicesAsync | ログイン会員自身に紐づく請求書のみ取得する |
| 所有者取得 | GetOwnerMemberIdAsync / GetOwnerMemberIdByNumberAsync | OwnerOrAdmin判定用に請求書所有者を取得する |
| PDF生成 | GeneratePdfAsync | 請求書PDFをバイト配列として生成する |

#### 4.2.3 請求書作成

請求書作成時は、指定された会員IDに対応する会員が存在することを確認する。

通常の `CreateAsync` では、初期ステータスとして `UNPAID` を取得し、請求書を登録する。

明細付き作成である `CreateWithLinesAsync` では、リクエストの明細行を `LineNo` 順に並べたうえで、サーバー側で `LineNo` を 1 から振り直す。

また、請求合計金額はクライアントから受け取った値をそのまま使わず、以下の計算によりサーバー側で算出する。

```text
TotalAmount = Σ(Qty × UnitPrice)
```

これにより、画面側の計算ミスや改ざんによって請求合計金額が不整合になることを防ぐ。

#### 4.2.4 請求書更新

請求書更新では、対象請求書を明細行込みで取得し、以下の順で処理する。

1. 請求書の存在確認
2. 会員の存在確認
3. ステータスの存在確認
4. 更新禁止ステータスの確認
5. 明細行IDの重複チェック
6. 請求ヘッダ更新
7. 請求明細の差分更新
8. 合計金額の再計算
9. 保存

更新禁止ステータスとして、`PAID` および `CANCELLED` の請求書は編集不可とする。

明細行は、既存行の更新、新規行の追加、リクエストから除外された行の削除を行う。このとき、明細行の並び順はサーバー側で再正規化し、`LineNo` を 1 から振り直す。

#### 4.2.5 請求書削除

請求書削除時は、対象請求書に PaymentAllocation が存在するか確認する。

入金割当が存在する請求書は、入金履歴との整合性を保つため削除不可とする。

入金割当が存在しない場合のみ、請求書を削除する。

#### 4.2.6 会員向け請求書取得

会員向け請求書一覧では、ログイン会員の MemberId に紐づく請求書のみを対象とする。

検索条件として、年、月、ステータス、キーワードを利用する。

ステータス判定は、保存済みステータス名だけに依存せず、入金割当金額をもとに以下のように判定する。

| 判定 | 条件 |
| --- | --- |
| unpaid | PaidAmount <= 0 |
| partial | PaidAmount > 0 かつ PaidAmount < TotalAmount |
| paid | PaidAmount >= TotalAmount |

これにより、会員画面でも実際の入金割当状況に基づいた支払状況を表示できる。

### 4.3 入金・入金割当ロジック

#### 4.3.1 概要

入金管理ロジックは `PaymentService` が担当する。

入金情報は `Payments` に保持し、請求書への割当は `PaymentAllocations` に保持する。

1件の入金を複数の請求書に割り当てることができ、また1件の請求書に対して複数の入金割当を持つこともできる。

#### 4.3.2 主な処理

| 処理 | メソッド | 内容 |
| --- | --- | --- |
| 入金登録 | CreateAsync | 会員、入金日、金額を検証して入金を登録する |
| 入金一覧検索 | SearchAsync | 年、月、キーワード、割当状態で検索する |
| 入金詳細取得 | GetByIdAsync | 入金本体と割当済み請求書を取得する |
| 入金割当追加 | AddAllocationAsync | 入金を請求書へ割り当てる |
| 入金割当削除 | DeleteAllocationAsync | 指定した割当を削除する |
| 入金割当保存 | SaveAllocationsAsync | 既存割当を置き換えて保存する |
| 請求ステータス再計算 | RecalcInvoiceStatusAsync | 入金割当の合計に応じて請求ステータスを更新する |

#### 4.3.3 入金一覧検索

入金一覧検索では、以下の条件で検索を行う。

| 条件 | 内容 |
| --- | --- |
| 年 | `PaymentDate.Year` で絞り込み |
| 月 | 指定がある場合のみ `PaymentDate.Month` で絞り込み |
| キーワード | 入金ID、入金名義、請求書番号を対象に検索 |
| ステータス | UNALLOCATED / PARTIAL / ALLOCATED で絞り込み |

入金ステータスは、入金金額と割当済み金額の関係から以下のように判定する。

| ステータス | 条件 |
| --- | --- |
| UNALLOCATED | 割当済み金額が 0 |
| PARTIAL | 割当済み金額が 0 より大きく、入金金額未満 |
| ALLOCATED | 割当済み金額が入金金額以上 |

一覧レスポンスには、入金金額合計、割当済み合計、未割当合計をサマリーとして含める。

#### 4.3.4 入金登録

入金登録時は、以下を検証する。

| 項目 | 検証内容 |
| --- | --- |
| MemberId | 0より大きいこと、対象会員が存在すること |
| PaymentDate | 既定値でないこと |
| Amount | 0より大きいこと |

入金日はUTCに補正し、入金名義、入金方法は空文字の場合 `null` として登録する。

#### 4.3.5 入金割当追加

入金割当追加では、以下を検証する。

1. PaymentId が有効であること
2. InvoiceId が有効であること
3. 割当金額が 0 より大きいこと
4. 対象入金が存在すること
5. 対象請求書が存在すること
6. 同一入金に対して同一請求書が重複割当されていないこと
7. 割当金額が入金残額を超えないこと

割当追加後は、対象請求書のステータスを再計算する。

#### 4.3.6 入金割当の置き換え保存

`SaveAllocationsAsync` では、既存の入金割当を一度削除し、リクエストで指定された割当行で置き換える。

割当行が0件の場合は、既存割当をすべて削除する。

割当置き換え時は、以下を検証する。

| 項目 | 検証内容 |
| --- | --- |
| 割当金額 | すべて 0 より大きいこと |
| 割当合計 | 入金金額を超えないこと |
| 請求書ID | 指定された請求書がすべて存在すること |

置き換え前後で影響を受けた請求書IDを抽出し、それぞれの請求ステータスを再計算する。

#### 4.3.7 入金割当削除

入金割当削除では、対象の `PaymentAllocation` が指定された入金に紐づいていることを確認する。

削除後は、対象請求書のステータスを再計算する。

#### 4.3.8 請求ステータス再計算

入金割当の追加、削除、置き換え後は、対象請求書の入金済み金額を再集計し、請求ステータスを再計算する。

判定条件は以下のとおりである。

| ステータス | 条件 |
| --- | --- |
| PAID | 割当済み金額 >= 請求金額 |
| PARTIAL | 割当済み金額 > 0 かつ 請求金額未満 |
| OVERDUE | 割当済み金額 = 0 かつ 支払期限を過ぎている |
| UNPAID | 割当済み金額 = 0 かつ 支払期限内 |

ただし、`CANCELLED` の請求書は業務上の安全性を考慮し、ステータス再計算の対象外とする。

### 4.4 督促管理ロジック

#### 4.4.1 概要

督促管理ロジックは `CollectionService` が担当する。

督促管理では、請求書に対する督促履歴を `ReminderHistories` として登録し、請求書の現在状況と過去の督促履歴を確認できるようにする。

#### 4.4.2 主な処理

| 処理 | メソッド | 内容 |
| --- | --- | --- |
| 請求スナップショット取得 | GetSnapshotAsync | 請求書、会員、入金割当を取得し、督促対象の概要を返す |
| 督促履歴取得 | GetLogsAsync | 請求書に紐づく督促履歴を新しい順に取得する |
| 督促履歴登録 | CreateLogAsync | 督促履歴を登録し、必要に応じて請求ステータスをDUNNINGへ変更する |

#### 4.4.3 請求スナップショット取得

督促画面で利用する請求スナップショットでは、以下の情報を取得する。

| 項目 | 内容 |
| --- | --- |
| InvoiceId | 請求書ID |
| InvoiceNumber | 請求番号 |
| MemberName | 会員名 |
| MemberEmail | 会員メールアドレス |
| InvoiceDate | 請求日 |
| DueDate | 支払期限 |
| Total | 請求金額 |
| PaidTotal | 入金割当済み金額 |

`PaidTotal` は `PaymentAllocations` の合計から算出する。

#### 4.4.4 督促履歴登録

督促履歴登録では、対象請求書の存在確認を行ったうえで、`ReminderHistory` を登録する。

登録する主な項目は以下のとおりである。

| 項目             | 内容      |
| -------------- | ------- |
| RemindedAt     | 督促実施日時  |
| Method         | 督促方法    |
| Tone           | 督促トーン   |
| Title          | 件名・タイトル |
| Note           | メモ      |
| NextActionDate | 次回対応予定日 |
| Subject        | メール件名など |
| BodyText       | 本文      |

督促履歴登録時は、請求ステータスを督促専用の状態へ変更せず、`ReminderHistories` に履歴として記録する。

請求書の状態は、入金割当状況および支払期限に基づき、`UNPAID`、`PARTIAL`、`OVERDUE`、`PAID`、`CANCELLED` の範囲で管理する。
督促済みであることは、請求ステータスではなく `ReminderHistories` の有無および最新の督促日時により判断する。

これにより、請求ステータスは入金状態を表す責務に限定し、督促の実施履歴は履歴テーブルで管理する構成とする。

#### 4.4.5 督促送信ジョブ登録

督促履歴登録時、メール送信等の外部処理は即時実行せず、必要に応じて `ReminderJobs` にジョブとして登録する。

登録されたジョブは `ReminderJobWorker` によりバックグラウンドで処理される。  
これにより、APIレスポンスをメール送信処理に依存させず、送信失敗時もリトライやエラー内容の記録が可能となる。

### 4.5 売上集計ロジック

#### 4.5.1 概要

売上集計ロジックは `SalesService` が担当する。

売上集計では、請求書を基準に請求金額、入金済み金額、未回収金額、回収率を算出する。

入金済み金額は、請求書に紐づく `PaymentAllocations` の合計から算出する。

#### 4.5.2 主な処理

| 処理 | メソッド | 内容 |
| --- | --- | --- |
| 売上一覧検索 | SearchAsync | 請求書単位の売上・入金・未回収状況を取得する |
| 会員別売上集計 | SearchByMemberAsync | 会員単位で請求額、入金額、未回収額、回収率を集計する |
| CSV出力用データ取得 | ExportAsync | CSV出力用の売上一覧データを取得する |

#### 4.5.3 売上一覧検索

売上一覧検索では、以下の条件で請求書を検索する。

| 条件 | 内容 |
| --- | --- |
| 年 | 請求日の年 |
| 月 | 指定がある場合のみ請求日の月 |
| キーワード | 請求番号、会員名 |
| ステータス | paid / partial / unpaid / all |
| 会員ID | 指定がある場合、対象会員に絞り込み |

請求書ごとに以下を算出する。

| 項目 | 算出方法 |
| --- | --- |
| InvoiceAmount | 請求書の `TotalAmount` |
| PaidAmount | `PaymentAllocations.Amount` の合計 |
| RemainingAmount | InvoiceAmount - PaidAmount |
| LastPaidAt | 最終入金日 |
| StatusCode | 入金額に基づく状態 |

ステータスは以下のように判定する。

| ステータス | 条件 |
| --- | --- |
| UNPAID | PaidAmount <= 0 |
| PARTIAL | PaidAmount > 0 かつ PaidAmount < InvoiceAmount |
| PAID | PaidAmount >= InvoiceAmount |

#### 4.5.4 会員別売上集計

会員別売上集計では、請求書単位で請求額・入金額を算出した後、会員単位にグルーピングする。

会員ごとに以下を算出する。

| 項目 | 内容 |
| --- | --- |
| InvoiceTotal | 会員に紐づく請求金額合計 |
| PaidTotal | 会員に紐づく入金割当合計 |
| RemainingTotal | InvoiceTotal - PaidTotal |
| RecoveryRate | PaidTotal / InvoiceTotal × 100 |

一覧は請求金額合計の大きい順に表示する。

#### 4.5.5 CSV出力用データ取得

CSV出力用データでは、画面表示と同様に年、月、キーワード、ステータス、会員IDで絞り込む。

出力行には、請求番号、会員名、請求日、支払期限、ステータス、請求金額、入金済み金額、未回収金額、最終入金日を含める。

### 4.6 管理者ダッシュボード集計ロジック

#### 4.6.1 概要

管理者ダッシュボード向けの集計は `AdminSummaryService` が担当する。

指定年の請求書を対象に、請求金額合計、入金済み金額合計、未回収金額、回収率、請求件数、入金件数、月別売上、未回収TOP5を算出する。

#### 4.6.2 集計項目

| 項目 | 内容 |
| --- | --- |
| InvoiceTotal | 指定年の請求金額合計 |
| PaidTotal | 対象請求書に紐づく入金割当合計 |
| RemainingTotal | InvoiceTotal - PaidTotal |
| RecoveryRate | PaidTotal / InvoiceTotal × 100 |
| InvoiceCount | 指定年の請求件数 |
| PaymentCount | 指定年の入金件数 |
| MonthlySales | 1月〜12月の月別請求金額 |
| UnpaidTop5 | 未回収額がある請求書の上位5件 |

#### 4.6.3 未回収TOP5

未回収TOP5では、請求金額から入金済み金額を差し引いた残額を算出する。

残額が 0 より大きい請求書を対象とし、以下の優先順で上位5件を抽出する。

1. 支払期限超過の請求書を優先
2. 未回収金額が大きい順
3. 支払期限が古い順

これにより、管理者が優先的に確認すべき未回収請求をダッシュボード上で把握できる。

### 4.7 監査ログロジック

#### 4.7.1 概要

監査ログは `AuditLogger` が担当する。

入金割当の追加、削除、置き換えなど、業務上重要な操作について、操作主体、操作内容、対象エンティティ、補足情報を `AuditLogs` に記録する。

#### 4.7.2 記録項目

| 項目 | 内容 |
| --- | --- |
| ActorUserId | 操作者のユーザーID |
| ActorRole | 操作者のロール |
| Action | 操作種別 |
| Entity | 操作対象エンティティ |
| EntityId | 操作対象ID |
| Summary | 操作概要 |
| DataJson | 操作時の補足データ |
| CorrelationId | リクエスト追跡ID |
| IpAddress | 操作者IPアドレス |
| UserAgent | ユーザーエージェント |
| CreatedAt | 記録日時 |

#### 4.7.3 AuditActor必須方針

監査ログ記録時は、`AuditActor` を必須とする。

`AuditActor` が渡されない場合は例外とし、誰が操作したか不明な監査ログが作成されないようにする。

#### 4.7.4 操作ログ取得

管理者向け操作ログ一覧では、`AdminOperationLogService` により `AuditLogs` から直近のログを取得する。

取得件数は 1〜50 件に制限し、作成日時の降順、IDの降順で取得する。

## 5. DTO・Request・Query・Command設計

### 5.1 設計方針

本システムでは、API層、Application層、Infrastructure層の責務を分離するため、画面・APIで扱うデータをDTO、検索条件をQuery、更新・登録指示をCommandとして定義する。

Entityを直接APIレスポンスとして返却せず、画面やAPIの用途に合わせたDTOへ変換することで、以下を実現する。

| 方針 | 内容 |
| --- | --- |
| Entityの隠蔽 | DB構造をそのまま外部公開しない |
| 画面単位の最適化 | 一覧、詳細、選択肢など用途に応じたDTOを用意する |
| 検索条件の集約 | 一覧検索条件をQueryとしてまとめる |
| 登録・更新指示の明確化 | 登録・更新処理はCommandまたはRequest DTOで表現する |
| 権限別レスポンス | 管理者向け、会員向けで返却項目を分ける |
| 集計結果の表現 | 売上、入金、未回収、回収率などの集計値をDTOとして返却する |

DTOは主に以下の種類に分類する。

| 種類 | 用途 |
| --- | --- |
| Response DTO | APIレスポンスとして画面へ返すデータ |
| Request DTO | APIリクエストボディとして受け取るデータ |
| Query | 検索条件、ページング条件、絞り込み条件 |
| Command | Application層へ渡す登録・更新指示 |
| Result DTO | 一覧データ、検索条件、件数、サマリーをまとめた戻り値 |

### 5.2 請求書DTO設計

#### 5.2.1 概要

請求書関連DTOは、管理者向けの請求書管理、会員向けの自分の請求書確認、請求書詳細、請求明細、入金割当、督促履歴を表現する。

主なDTOは以下のとおりである。

| DTO | 用途 |
| --- | --- |
| InvoiceDto | 請求書の基本情報 |
| InvoiceListItemDto | 管理者向け請求書一覧の1行 |
| InvoiceDetailDto | 請求書詳細 |
| InvoiceLineDto | 請求明細行 |
| InvoicePaymentAllocationDto | 請求書に紐づく入金割当 |
| InvoiceReminderHistoryDto | 請求書に紐づく督促履歴 |
| UpdateInvoiceRequestDto | 請求書登録・更新リクエスト |
| MyInvoiceListItemDto | 会員向け請求書一覧の1行 |
| MyInvoiceListResultDto | 会員向け請求書一覧の検索結果 |

#### 5.2.2 InvoiceDto

`InvoiceDto` は、請求書の基本情報を表すDTOである。

一覧、登録後レスポンス、簡易詳細など、請求書の代表的な情報を返す用途で使用する。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| Id | long | 請求書ID |
| MemberId | long | 会員ID |
| MemberName | string | 会員名 |
| InvoiceNumber | string | 請求番号 |
| InvoiceDate | DateTime | 請求日 |
| DueDate | DateTime | 支払期限 |
| TotalAmount | decimal | 請求金額 |
| StatusId | long | 請求ステータスID |
| StatusName | string | 請求ステータス名 |
| PdfPath | string? | PDFパス |
| Remarks | string? | 備考 |
| CreatedAt | DateTime | 作成日時 |

#### 5.2.3 InvoiceListItemDto

`InvoiceListItemDto` は、管理者向け請求書一覧の1行を表すDTOである。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| Id | long | 請求書ID |
| InvoiceNumber | string | 請求番号 |
| InvoiceDate | DateTime | 請求日 |
| DueDate | DateTime | 支払期限 |
| TotalAmount | decimal | 請求金額 |
| MemberName | string | 会員名 |
| StatusName | string | 請求ステータス名 |

#### 5.2.4 InvoiceDetailDto

`InvoiceDetailDto` は、請求書詳細画面で使用するDTOである。

請求書ヘッダ情報に加えて、入金済み金額、残額、請求明細、入金割当、督促履歴を含む。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| Id | long | 請求書ID |
| MemberId | long | 会員ID |
| MemberName | string | 会員名 |
| InvoiceNumber | string | 請求番号 |
| InvoiceDate | DateTime | 請求日 |
| DueDate | DateTime | 支払期限 |
| TotalAmount | decimal | 請求金額 |
| PaidAmount | decimal | 入金済み金額 |
| RemainingAmount | decimal | 未回収金額 |
| StatusId | long | ステータスID |
| StatusName | string | ステータス名 |
| PdfPath | string? | PDFパス |
| Remarks | string? | 備考 |
| CreatedAt | DateTime | 作成日時 |
| Allocations | List<InvoicePaymentAllocationDto> | 入金割当一覧 |
| Reminders | List<InvoiceReminderHistoryDto> | 督促履歴一覧 |
| Lines | List<InvoiceLineDto> | 請求明細一覧 |

#### 5.2.5 InvoiceLineDto

`InvoiceLineDto` は、請求書明細行を表すDTOである。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| Id | long? | 明細ID。既存行は値あり、新規行はnull |
| LineNo | int | 行番号 |
| Name | string | 明細名 |
| Qty | int | 数量 |
| UnitPrice | decimal | 単価 |

請求金額は、明細行の `Qty × UnitPrice` の合計によりサーバー側で再計算する。

#### 5.2.6 InvoicePaymentAllocationDto

`InvoicePaymentAllocationDto` は、請求書詳細に表示する入金割当情報を表す。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| PaymentId | long | 入金ID |
| PaymentDate | DateTime | 入金日 |
| AllocatedAmount | decimal | 割当金額 |
| PayerName | string? | 入金名義 |
| Method | string? | 入金方法 |
| ImportBatchId | long? | 入金取込バッチID |

#### 5.2.7 InvoiceReminderHistoryDto

`InvoiceReminderHistoryDto` は、請求書詳細に表示する督促履歴を表す。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| Id | long | 督促履歴ID |
| RemindedAt | DateTime | 督促日時 |
| Method | string | 督促方法 |
| Note | string? | メモ |

#### 5.2.8 UpdateInvoiceRequestDto

`UpdateInvoiceRequestDto` は、請求書登録・更新時に使用するリクエストDTOである。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| InvoiceNumber | string | 請求番号 |
| InvoiceDate | DateTime | 請求日 |
| DueDate | DateTime | 支払期限 |
| MemberId | long | 会員ID |
| StatusId | long | ステータスID |
| Remarks | string? | 備考 |
| Lines | List<InvoiceLineDto> | 請求明細 |

#### 5.2.9 会員向け請求書一覧DTO

会員向けの請求書一覧では、ログイン会員自身に紐づく請求書のみを返却する。

`MyInvoiceListItemDto` は、会員向け請求書一覧の1行を表す。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| Id | long | 請求書ID |
| InvoiceNumber | string | 請求番号 |
| InvoiceDate | DateTime | 請求日 |
| DueDate | DateTime | 支払期限 |
| TotalAmount | decimal | 請求金額 |
| PaidAmount | decimal | 入金済み金額 |
| RemainingAmount | decimal | 未回収金額 |
| StatusCode | string | ステータスコード |
| StatusName | string | ステータス名 |
| IsOverdue | bool | 支払期限超過か |

`MyInvoiceListResultDto` は、検索条件、ページング情報、検索結果をまとめて返却する。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| Year | int | 対象年 |
| AvailableYears | IReadOnlyList<int> | 選択可能年一覧 |
| Month | string | 対象月。"all" または "1"〜"12" |
| Status | string | "all" / "unpaid" / "partial" / "paid" |
| Q | string | 検索キーワード |
| Page | int | ページ番号 |
| PageSize | int | 1ページ件数 |
| TotalCount | int | 総件数 |
| Items | IReadOnlyList<MyInvoiceListItemDto> | 検索結果 |

### 5.3 入金DTO設計

#### 5.3.1 概要

入金関連DTOは、入金登録、入金一覧、入金詳細、請求書への入金割当を表現する。

主なDTOは以下のとおりである。

| DTO | 用途 |
| --- | --- |
| CreatePaymentRequestDto | 入金登録リクエスト |
| CreatePaymentResponseDto | 入金登録レスポンス |
| CreatePaymentAllocationRequestDto | 入金割当登録リクエスト |
| PaymentListItemDto | 入金一覧の1行 |
| PaymentListResultDto | 入金一覧検索結果 |
| PaymentDetailDto | 入金詳細 |
| PaymentAllocationDto | 入金詳細内の割当情報 |
| InvoiceLinkDto | 入金に紐づく請求書リンク |

#### 5.3.2 CreatePaymentRequestDto

`CreatePaymentRequestDto` は、入金登録時に使用するリクエストDTOである。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| MemberId | long | 会員ID |
| PaymentDate | DateTime | 入金日 |
| Amount | decimal | 入金金額 |
| PayerName | string? | 入金名義 |
| Method | string? | 入金方法 |

#### 5.3.3 CreatePaymentResponseDto

`CreatePaymentResponseDto` は、入金登録後に作成された入金IDを返すDTOである。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| PaymentId | long | 作成された入金ID |

#### 5.3.4 CreatePaymentAllocationRequestDto

`CreatePaymentAllocationRequestDto` は、入金を請求書へ割り当てる際に使用するDTOである。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| InvoiceId | long | 割当先請求書ID |
| Amount | decimal | 割当金額 |

#### 5.3.5 PaymentListItemDto

`PaymentListItemDto` は、入金一覧の1行を表すDTOである。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| Id | long | 入金ID |
| PaymentDate | DateTime | 入金日 |
| PayerName | string? | 入金名義 |
| Amount | decimal | 入金金額 |
| AllocatedAmount | decimal | 割当済み金額 |
| Invoices | List<InvoiceLinkDto> | 紐づく請求書 |
| Status | string | "UNALLOCATED" / "PARTIAL" / "ALLOCATED" |

#### 5.3.6 PaymentListResultDto

`PaymentListResultDto` は、入金一覧の検索条件、ページング情報、検索結果、集計情報をまとめて返すDTOである。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| Year | int | 対象年 |
| Month | string | 対象月。"all" または "1"〜"12" |
| Keyword | string | 検索キーワード |
| Status | string | "all" / "UNALLOCATED" / "PARTIAL" / "ALLOCATED" |
| Page | int | ページ番号 |
| PageSize | int | 1ページ件数 |
| TotalCount | int | 総件数 |
| Rows | List<PaymentListItemDto> | 入金一覧 |
| Summary | SummaryDto | 入金集計 |

`SummaryDto` は以下の集計値を持つ。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| TotalAmount | decimal | 入金金額合計 |
| AllocatedTotal | decimal | 割当済み合計 |
| UnallocatedTotal | decimal | 未割当合計 |

#### 5.3.7 PaymentDetailDto

`PaymentDetailDto` は、入金詳細画面で使用するDTOである。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| Id | long | 入金ID |
| PaymentDate | DateTime | 入金日 |
| Amount | decimal | 入金金額 |
| PayerName | string? | 入金名義 |
| Method | string? | 入金方法 |
| AllocatedAmount | decimal | 割当済み金額 |
| UnallocatedAmount | decimal | 未割当金額 |
| Status | string | "UNALLOCATED" / "PARTIAL" / "ALLOCATED" |
| Allocations | List<PaymentAllocationDto> | 割当一覧 |

#### 5.3.8 PaymentAllocationDto

`PaymentAllocationDto` は、入金詳細内で、どの請求書にいくら割り当てられているかを表す。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| InvoiceId | long | 請求書ID |
| InvoiceNumber | string | 請求番号 |
| Amount | decimal | 割当金額 |

#### 5.3.9 InvoiceLinkDto

`InvoiceLinkDto` は、入金一覧で入金に紐づく請求書を簡易表示するためのDTOである。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| Id | long | 請求書ID |
| InvoiceNumber | string | 請求番号 |

### 5.4 会員DTO設計

#### 5.4.1 概要

会員関連DTOは、管理者向けの会員管理、会員選択肢、自分のプロフィール表示・更新で使用する。

主なDTOは以下のとおりである。

| DTO | 用途 |
| --- | --- |
| MemberDto | 会員詳細 |
| MemberListItemDto | 会員一覧の1行 |
| MemberOptionDto | 請求書登録画面などで使用する会員選択肢 |
| MyProfileResponse | 自分のプロフィール取得レスポンス |
| UpdateMyProfileRequest | 自分のプロフィール更新リクエスト |

#### 5.4.2 MemberDto

`MemberDto` は、管理者向け会員詳細などで使用するDTOである。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| Id | long | 会員ID |
| Name | string | 氏名 |
| Email | string | メールアドレス |
| PostalCode | string? | 郵便番号 |
| Address | string? | 住所 |
| Phone | string? | 電話番号 |
| Role | MemberRole | 会員ロール |
| IsActive | bool | 有効状態 |
| CreatedAt | DateTime | 作成日時 |

#### 5.4.3 MemberListItemDto

`MemberListItemDto` は、会員一覧画面の1行を表すDTOである。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| Id | long | 会員ID |
| Name | string | 氏名 |
| Email | string | メールアドレス |
| Role | MemberRole | 会員ロール |
| IsActive | bool | 有効状態 |

#### 5.4.4 MemberOptionDto

`MemberOptionDto` は、請求書登録・更新画面などで会員を選択するための軽量DTOである。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| Id | long | 会員ID |
| Name | string | 会員名 |

#### 5.4.5 MyProfileResponse

`MyProfileResponse` は、ログイン会員自身のプロフィール取得レスポンスである。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| Id | long | 会員ID |
| Name | string | 氏名 |
| Email | string | メールアドレス |
| PostalCode | string? | 郵便番号 |
| Address | string? | 住所 |
| Phone | string? | 電話番号 |
| IsEmailConfirmed | bool | メール確認済みか |

#### 5.4.6 UpdateMyProfileRequest

`UpdateMyProfileRequest` は、ログイン会員自身のプロフィール更新リクエストである。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| Name | string | 氏名 |
| Email | string | メールアドレス |
| PostalCode | string? | 郵便番号 |
| Address | string? | 住所 |
| Phone | string? | 電話番号 |

### 5.5 売上DTO設計

#### 5.5.1 概要

売上関連DTOは、売上一覧、会員別売上集計、売上サマリー、CSV出力用データで使用する。

本章では、添付されている会員別売上集計DTOを中心に整理する。

#### 5.5.2 SalesByMemberRowDto

`SalesByMemberRowDto` は、会員別売上集計の1行を表すDTOである。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| MemberId | long | 会員ID |
| MemberName | string | 会員名 |
| InvoiceTotal | decimal | 請求金額合計 |
| PaidTotal | decimal | 入金済み金額合計 |
| RemainingTotal | decimal | 未回収金額 |
| RecoveryRate | decimal | 回収率 |

#### 5.5.3 SalesByMemberResultDto

`SalesByMemberResultDto` は、会員別売上集計の検索結果全体を表すDTOである。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| Year | int | 対象年 |
| Month | string | 対象月。"all" または "1"〜"12" |
| Keyword | string | 検索キーワード |
| Page | int | ページ番号 |
| PageSize | int | 1ページ件数 |
| TotalCount | int | 総件数 |
| Rows | IReadOnlyList<SalesByMemberRowDto> | 会員別集計一覧 |
| Summary | SalesSummaryDto | 売上サマリー |

`Summary` には、売上一覧と共通の売上サマリーを設定する。

### 5.6 督促DTO設計

#### 5.6.1 概要

督促関連DTOは、督促対象請求書の確認、督促履歴の表示、督促履歴の登録で使用する。

主なDTOは以下のとおりである。

| DTO | 用途 |
| --- | --- |
| InvoiceSnapshotDto | 督促対象請求書の概要 |
| DunningLogDto | 督促履歴 |
| CreateDunningLogRequestDto | 督促履歴登録リクエスト |

#### 5.6.2 InvoiceSnapshotDto

`InvoiceSnapshotDto` は、督促対象請求書の現在状況を表示するDTOである。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| InvoiceId | long | 請求書ID |
| InvoiceNumber | string | 請求番号 |
| MemberName | string | 会員名 |
| MemberEmail | string? | 会員メールアドレス |
| InvoiceDate | DateTime | 請求日 |
| DueDate | DateTime | 支払期限 |
| Total | decimal | 請求金額 |
| PaidTotal | decimal | 入金済み金額 |

#### 5.6.3 DunningLogDto

`DunningLogDto` は、督促履歴の1件を表すDTOである。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| Id | long | 督促履歴ID |
| At | DateTime | 督促日時 |
| Channel | string | 督促方法。EMAIL / PHONE / LETTER |
| Title | string | タイトル |
| Memo | string? | メモ |
| Tone | string? | トーン。SOFT / NORMAL / STRONG |
| NextActionDate | DateTime? | 次回対応予定日 |

#### 5.6.4 CreateDunningLogRequestDto

`CreateDunningLogRequestDto` は、督促履歴を登録するためのリクエストDTOである。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| Channel | string | 督促方法。既定値は EMAIL |
| Tone | string? | 督促トーン |
| Title | string | タイトル |
| Memo | string? | メモ |
| NextActionDate | DateTime? | 次回対応予定日 |
| Subject | string? | 任意の件名 |
| BodyText | string? | 任意の本文 |

### 5.7 管理者DTO設計

#### 5.7.1 概要

管理者関連DTOは、管理者ダッシュボード、未回収一覧、月別売上、操作ログ確認で使用する。

主なDTOは以下のとおりである。

| DTO | 用途 |
| --- | --- |
| AdminSummaryDto | 管理者ダッシュボード集計 |
| MonthlySalesDto | 月別売上 |
| UnpaidInvoiceDto | 未回収請求書 |
| AdminOperationLogDto | 管理者操作ログ |

#### 5.7.2 AdminSummaryDto

`AdminSummaryDto` は、管理者ダッシュボードに表示する年間集計を表すDTOである。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| Year | int | 対象年 |
| InvoiceTotal | decimal | 売上、請求総額 |
| PaidTotal | decimal | 入金総額、割当合計 |
| RemainingTotal | decimal | 未回収額 |
| RecoveryRate | decimal | 回収率 |
| InvoiceCount | int | 請求件数 |
| PaymentCount | int | 入金件数 |
| MonthlySales | IReadOnlyList<MonthlySalesDto> | 月別売上 |
| UnpaidTop5 | IReadOnlyList<UnpaidInvoiceDto> | 未回収TOP5 |

#### 5.7.3 MonthlySalesDto

`MonthlySalesDto` は、月別の請求金額を表す。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| Month | int | 月。1〜12 |
| InvoiceTotal | decimal | 請求金額合計 |

#### 5.7.4 UnpaidInvoiceDto

`UnpaidInvoiceDto` は、未回収請求書の概要を表す。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| InvoiceId | long | 請求書ID |
| InvoiceNumber | string | 請求番号 |
| ClientName | string | 顧客名 |
| DueDate | DateTime | 支払期限 |
| InvoiceTotal | decimal | 請求金額 |
| PaidTotal | decimal | 入金済み金額 |
| RemainingTotal | decimal | 未回収金額 |
| IsOverdue | bool | 支払期限超過か |

#### 5.7.5 AdminOperationLogDto

`AdminOperationLogDto` は、管理者操作ログの1件を表すDTOである。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| Id | long | 操作ログID |
| At | DateTime | 操作日時 |
| ActorUserId | long | 操作者ユーザーID |
| Action | string | 操作種別 |
| Entity | string | 操作対象 |
| EntityId | string? | 操作対象ID |
| Summary | string | 操作概要 |

### 5.8 アカウントDTO設計

#### 5.8.1 概要

アカウント関連DTOは、会員自身の請求書一覧表示で使用する。

`AccountInvoiceListDto` は、フロントエンドが期待する命名に寄せた会員向け請求書一覧DTOである。

#### 5.8.2 AccountInvoiceListItemDto

`AccountInvoiceListItemDto` は、会員向け請求書一覧の1行を表す。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| Id | long | 請求書ID |
| InvoiceNumber | string | 請求番号 |
| IssuedAt | DateTime | 請求日 |
| DueAt | DateTime | 支払期限 |
| TotalAmount | decimal | 請求金額 |
| StatusName | string | ステータス名 |
| IsOverdue | bool | 支払期限超過か |

#### 5.8.3 AccountInvoiceListDto

`AccountInvoiceListDto` は、会員向け請求書一覧の検索結果全体を表す。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| Year | int | 対象年 |
| AvailableYears | IReadOnlyList<int> | 選択可能年一覧 |
| Month | string | 対象月。"all" または "1"〜"12" |
| Status | string | "all" / "unpaid" / "partial" / "paid" |
| Q | string | 検索キーワード |
| Page | int | ページ番号 |
| PageSize | int | 1ページ件数 |
| TotalCount | int | 総件数 |
| Items | IReadOnlyList<AccountInvoiceListItemDto> | 請求書一覧 |

### 5.9 Query設計

#### 5.9.1 概要

Queryは、一覧検索や集計検索の条件をApplication層へ渡すために使用する。

主なQueryは以下のとおりである。

| Query | 用途 |
| --- | --- |
| InvoiceSearchQuery | 管理者向け請求書検索 |
| MyInvoiceSearchQuery | 会員向け自分の請求書検索 |
| MemberSearchQuery | 会員検索 |
| PaymentSearchQuery | 入金検索 |
| SalesSearchRequest | 売上検索 |

#### 5.9.2 InvoiceSearchQuery

`InvoiceSearchQuery` は、管理者向け請求書検索条件である。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| InvoiceNumber | string? | 請求番号 |
| MemberName | string? | 会員名 |
| FromInvoiceDate | DateTime? | 請求日From |
| ToInvoiceDate | DateTime? | 請求日To |
| StatusId | long? | ステータスID |
| Page | int | ページ番号。既定値1 |
| PageSize | int | 1ページ件数。既定値50 |

#### 5.9.3 MyInvoiceSearchQuery

`MyInvoiceSearchQuery` は、会員向けの自分の請求書検索条件である。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| MemberId | long | 会員ID。ログインユーザーから確定 |
| Year | int | 対象年 |
| Month | string? | "all" または "1"〜"12" |
| Status | string? | "all" / "unpaid" / "partial" / "paid" |
| Q | string? | 請求書番号、備考などの検索文字 |
| Page | int | ページ番号。既定値1 |
| PageSize | int | 1ページ件数。既定値10 |

#### 5.9.4 MemberSearchQuery

`MemberSearchQuery` は、管理者向け会員検索条件である。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| Keyword | string? | 名前またはメールアドレス |
| Role | MemberRole? | 管理者、会員、退会など |
| IsActive | bool? | 有効状態 |
| Page | int | ページ番号。既定値1 |
| PageSize | int | 1ページ件数。既定値50 |

#### 5.9.5 PaymentSearchQuery

`PaymentSearchQuery` は、入金検索条件である。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| Year | int | 対象年 |
| Month | int? | 対象月。nullの場合は全月 |
| Keyword | string? | 入金ID、入金名義、請求書番号など |
| Status | string | "all" / "UNALLOCATED" / "PARTIAL" / "ALLOCATED" |
| Page | int | ページ番号。既定値1 |
| PageSize | int | 1ページ件数。既定値10 |

#### 5.9.6 SalesSearchRequest

`SalesSearchRequest` は、売上検索条件である。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| Year | int? | 対象年 |
| Month | int? | 対象月。nullの場合は全月 |
| Q | string? | 検索キーワード |
| Status | string? | all / unpaid / partial / paid |
| Page | int? | ページ番号 |
| PageSize | int? | 1ページ件数 |
| MemberId | long? | 会員ID |

### 5.10 Command設計

#### 5.10.1 概要

Commandは、Application層のServiceへ登録・更新処理の指示を渡すために使用する。

主なCommandは以下のとおりである。

| Command | 用途 |
| --- | --- |
| CreateInvoiceCommand | 請求書作成 |
| UpdateInvoiceStatusCommand | 請求書ステータス更新 |
| RegisterMemberCommand | 会員登録 |
| UpdateMemberCommand | 会員更新 |

#### 5.10.2 CreateInvoiceCommand

`CreateInvoiceCommand` は、請求書作成指示を表す。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| MemberId | long | 会員ID |
| InvoiceNumber | string | 請求番号 |
| InvoiceDate | DateTime | 請求日 |
| DueDate | DateTime | 支払期限 |
| TotalAmount | decimal | 請求金額 |
| Remarks | string? | 備考 |

#### 5.10.3 UpdateInvoiceStatusCommand

`UpdateInvoiceStatusCommand` は、請求書ステータス更新指示を表す。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| InvoiceId | long | 請求書ID |
| StatusId | long | 更新後ステータスID |

#### 5.10.4 RegisterMemberCommand

`RegisterMemberCommand` は、会員登録指示を表す。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| Name | string | 氏名 |
| Email | string | メールアドレス |
| PostalCode | string? | 郵便番号 |
| Address | string? | 住所 |
| Phone | string? | 電話番号 |
| Password | string | パスワード |
| Role | MemberRole? | 会員ロール。nullの場合はCustomer扱い |

#### 5.10.5 UpdateMemberCommand

`UpdateMemberCommand` は、管理者による会員更新指示を表す。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| Name | string | 氏名 |
| Email | string | メールアドレス |
| PostalCode | string? | 郵便番号 |
| Address | string? | 住所 |
| Phone | string? | 電話番号 |
| Role | MemberRole | 会員ロール |
| IsActive | bool | 有効状態 |

### 5.11 DTO・Query・Commandの使い分け

本システムでは、用途に応じて以下のようにDTO、Query、Commandを使い分ける。

| 種類 | 役割 | 例 |
| --- | --- | --- |
| DTO | 画面へ返すデータ、またはリクエストボディ | InvoiceDto, PaymentDetailDto, MemberDto |
| Result DTO | 一覧検索結果とページング・集計をまとめた戻り値 | PaymentListResultDto, MyInvoiceListResultDto |
| Query | 検索条件、絞り込み条件、ページング条件 | InvoiceSearchQuery, PaymentSearchQuery |
| Command | 登録・更新処理の指示 | CreateInvoiceCommand, UpdateMemberCommand |
| Request DTO | APIから受け取る登録・更新用データ | UpdateInvoiceRequestDto, CreatePaymentRequestDto |
| 軽量DTO | 選択肢やリンク表示用の最小データ | MemberOptionDto, InvoiceLinkDto |

このように用途ごとに型を分けることで、APIレスポンス、画面表示、検索条件、業務処理指示の責務を明確にする。

## 6. 認証・認可・アカウント設計

### 6.1 設計方針

本システムでは、JWT認証を利用してログイン状態を管理し、利用者のロールに応じてアクセス可能なAPIを制御する。

利用者は主に以下の2種類に分ける。

| 利用者区分 | ロール | 主な利用範囲 |
| --- | --- | --- |
| 管理者 | Admin | 会員管理、請求書管理、入金管理、売上集計、管理者ダッシュボード |
| 会員 | Customer | 自分のプロフィール確認、自分の請求書確認、自分の請求書PDF取得 |

退会済み、または無効化された会員は `Disabled` として扱い、既存トークンを保持していてもAPI利用できないようにする。

認証・認可に関する主な処理は以下で構成する。

| 区分 | 主な担当 |
| --- | --- |
| 管理者API | AdminEndpoints |
| 会員自身のAPI | MyAccountEndpoints |
| ログインユーザーID取得 | HttpContextExtensions |
| 会員登録 | MemberRegistrationService |
| パスワード再設定 | PasswordResetService |
| メール送信 | MailtrapEmailSender |
| 会員情報 | Member |
| パスワード再設定トークン | PasswordResetToken |

### 6.2 ロール設計

#### 6.2.1 ロール概要

本システムでは、会員種別を `Member.Role` で管理する。

| ロール | 内容 |
| --- | --- |
| Admin | 管理者 |
| Customer | 一般会員 |
| Disabled | 退会・無効化済み会員 |

`Member` エンティティは、氏名、メールアドレス、住所、電話番号、パスワードハッシュ、有効状態、ロール、メール認証情報を保持する。

#### 6.2.2 有効状態

会員の利用可否は、以下の2つで判定する。

| 項目 | 内容 |
| --- | --- |
| IsActive | 会員が有効かどうか |
| Role | Admin / Customer / Disabled の区分 |

退会時は `IsActive = false` とし、`Role = Disabled` に変更する。

これにより、退会後に古いJWTが残っている場合でも、API側で無効ユーザーとして扱う。

### 6.3 認可ポリシー設計

#### 6.3.1 管理者向けAPI

管理者向けAPIは、`AdminOnly` ポリシーを要求する。

`/api/admin` 配下のAPIは、管理者のみアクセス可能とする。

| API | メソッド | 認可 | 内容 |
| --- | --- | --- | --- |
| /api/admin/summary | GET | AdminOnly | 管理者ダッシュボード用の年間サマリー取得 |

#### 6.3.2 会員向けAPI

会員自身のAPIは、`MemberOnly` ポリシーを要求する。

`/api/members/me` 配下のAPIは、ログイン中の会員本人のみ利用可能とする。

| API | メソッド | 認可 | 内容 |
| --- | --- | --- | --- |
| /api/members/me | GET | MemberOnly | 自分の会員情報取得 |
| /api/members/me | PUT | MemberOnly | 自分の会員情報更新 |
| /api/members/me | DELETE | MemberOnly | 退会 |
| /api/members/me/invoices | GET | MemberOnly | 自分の請求書一覧取得 |
| /api/members/me/invoices/with-balance | GET | MemberOnly | 自分の請求書一覧取得、入金済み・残額付き |
| /api/members/me/invoices/{id} | GET | MemberOnly | 自分の請求書詳細取得 |
| /api/members/me/invoices/{id}/pdf | GET | MemberOnly | 自分の請求書PDF取得 |

### 6.4 ログインユーザー取得設計

#### 6.4.1 概要

会員向けAPIでは、JWTに含まれる `sub` クレームからログインユーザーの会員IDを取得する。

会員ID取得処理は `HttpContextExtensions.GetMemberIdAsync` に集約する。

#### 6.4.2 取得手順

ログインユーザーID取得処理は以下の順で行う。

1. 認証済みユーザーか確認する
2. JWTの `sub` または `NameIdentifier` からユーザーIDを取得する
3. ユーザーIDを `long` に変換する
4. DB上に会員が存在するか確認する
5. `IsActive` が true であることを確認する
6. `Role` が `Disabled` でないことを確認する
7. 会員IDを返す

#### 6.4.3 異常時

以下の場合は、認証エラーとして扱う。

| 条件 | 内容 |
| --- | --- |
| 未認証 | 認証済みユーザーでない |
| subなし | JWTからユーザーIDを取得できない |
| 会員なし | DB上に該当会員が存在しない |
| 無効会員 | `IsActive = false` |
| 退会済み | `Role = Disabled` |

退会後トークン対策として、JWTが有効期限内であってもDB上の会員状態を確認する。

### 6.5 会員登録設計

#### 6.5.1 概要

会員登録は `MemberRegistrationService` が担当する。

登録時は、メールアドレスの重複を確認し、パスワードをハッシュ化して会員情報を登録する。

また、メール確認用トークンを発行し、登録メールアドレス宛に確認URLを送信する。

#### 6.5.2 登録処理

会員登録処理は以下の順で行う。

1. メールアドレスが既に登録されていないか確認する
2. メール確認用トークンを生成する
3. トークン有効期限を設定する
4. 会員情報を作成する
5. パスワードをハッシュ化する
6. DBへ保存する
7. フロントエンドのメール確認URLを生成する
8. メール確認メールを送信する

#### 6.5.3 登録時の初期値

| 項目 | 値 |
| --- | --- |
| Role | 指定がない場合 Customer |
| IsActive | true |
| IsEmailConfirmed | false |
| EmailVerificationToken | 自動生成トークン |
| EmailVerificationTokenExpiresAt | 登録から24時間後 |

#### 6.5.4 メール確認URL

メール確認URLは、設定値 `Frontend:BaseUrl` をもとに生成する。

```text
{Frontend:BaseUrl}/auth/verify-email?token={token}
```

設定が存在しない場合は、既定値として `http://localhost:3000` を使用する。

#### 6.5.5 異常時

既に登録済みのメールアドレスが指定された場合は、登録失敗としてエラーを返す。

### 6.6 メール認証設計

#### 6.6.1 概要

メール認証では、会員登録時に発行した `EmailVerificationToken` を使用する。

会員はメール内の確認URLへアクセスし、メールアドレス確認を完了する。

#### 6.6.2 保持項目

メール認証に関する情報は `Member` に保持する。

| 項目 | 内容 |
| --- | --- |
| IsEmailConfirmed | メール確認済みか |
| EmailVerificationToken | メール確認トークン |
| EmailVerificationTokenExpiresAt | メール確認トークン有効期限 |

#### 6.6.3 プロフィール更新時の扱い

会員が自分のメールアドレスを変更した場合、メール確認状態をリセットする。

| 項目 | 更新内容 |
| --- | --- |
| IsEmailConfirmed | false |
| EmailVerificationToken | null |
| EmailVerificationTokenExpiresAt | null |

これにより、メールアドレス変更後は未確認状態として扱う。

### 6.7 パスワード再設定設計

#### 6.7.1 概要

パスワード再設定は `PasswordResetService` が担当する。

パスワード再設定は、以下の2段階で行う。

1. 再設定メール送信
2. 新しいパスワードへの更新

#### 6.7.2 再設定メール送信

再設定メール送信処理では、指定されたメールアドレスに対応する有効会員を検索する。

ただし、セキュリティ上の理由から、該当メールアドレスが存在しない場合でも成功扱いとする。これにより、登録済みメールアドレスの存在有無を外部から推測されにくくする。

処理手順は以下のとおりである。

1. メールアドレスに対応する有効会員を検索する
2. 存在しない場合は成功扱いで終了する
3. パスワード再設定トークンを生成する
4. 有効期限を1時間後に設定する
5. `PasswordResetToken` をDBに登録する
6. フロントエンドの再設定URLを生成する
7. 再設定メールを送信する

#### 6.7.3 再設定URL

再設定URLは、設定値 `Frontend:BaseUrl` をもとに生成する。

```text
{Frontend:BaseUrl}/auth/reset-password?token={token}
```

設定が存在しない場合は、既定値として `http://localhost:3000` を使用する。

#### 6.7.4 パスワード更新

パスワード更新処理では、以下の条件を満たすトークンを検索する。

| 条件 | 内容 |
| --- | --- |
| Token | リクエストされたトークンと一致する |
| ExpiresAt | 現在時刻より未来 |
| UsedAt | null |

条件を満たすトークンが存在する場合、会員のパスワードをハッシュ化して更新し、トークンの `UsedAt` に使用日時を設定する。

#### 6.7.5 無効なトークン

以下の場合は、パスワード再設定不可とする。

| 条件 | 内容 |
| --- | --- |
| トークン不一致 | DBに存在しない |
| 有効期限切れ | ExpiresAt <= 現在時刻 |
| 使用済み | UsedAt が設定済み |

この場合は、トークンが無効または期限切れである旨のエラーを返す。

### 6.8 自分のプロフィール設計

#### 6.8.1 自分の会員情報取得

会員は `/api/members/me` にアクセスすることで、自分のプロフィールを取得できる。

取得対象は、JWTから取得した会員IDに一致し、かつ有効な会員のみである。

返却項目は以下のとおりである。

| 項目 | 内容 |
| --- | --- |
| Id | 会員ID |
| Name | 氏名 |
| Email | メールアドレス |
| PostalCode | 郵便番号 |
| Address | 住所 |
| Phone | 電話番号 |
| IsEmailConfirmed | メール確認済みか |

#### 6.8.2 自分の会員情報更新

会員は `/api/members/me` にPUTすることで、自分のプロフィールを更新できる。

更新時は以下を検証する。

| 項目 | 検証内容 |
| --- | --- |
| Name | 必須 |
| Email | 必須 |

更新可能な項目は以下のとおりである。

| 項目 | 内容 |
| --- | --- |
| Name | 氏名 |
| Email | メールアドレス |
| PostalCode | 郵便番号 |
| Address | 住所 |
| Phone | 電話番号 |

メールアドレスが変更された場合は、メール確認済み状態をリセットする。

#### 6.8.3 退会

会員は `/api/members/me` にDELETEすることで退会できる。

退会時は物理削除ではなく、以下の論理削除を行う。

| 項目 | 更新内容 |
| --- | --- |
| IsActive | false |
| Role | Disabled |
| UpdatedAt | 現在日時 |

退会後は、`GetMemberIdAsync` のチェックによりAPI利用不可となる。

### 6.9 自分の請求書設計

#### 6.9.1 概要

会員は、自分に紐づく請求書のみ参照できる。

会員向け請求書APIでは、JWTから取得した会員IDを検索条件に設定し、他会員の請求書を参照できないようにする。

#### 6.9.2 自分の請求書一覧

`/api/members/me/invoices` では、会員自身の請求書一覧を取得する。

検索条件は以下のとおりである。

| 項目 | 内容 |
| --- | --- |
| Year | 対象年。未指定時は現在年 |
| Month | "all" または "1"〜"12" |
| Status | "all" / "unpaid" / "partial" / "paid" |
| Q | 検索キーワード |
| Page | ページ番号。未指定時は1 |
| PageSize | 1ページ件数。未指定時は10 |

レスポンスは、フロントエンド互換のため、`InvoiceDate` を `IssuedAt`、`DueDate` を `DueAt` に変換した `AccountInvoiceListDto` として返却する。

#### 6.9.3 入金残額付き請求書一覧

`/api/members/me/invoices/with-balance` では、入金済み金額、未回収金額を含む会員向け請求書一覧を取得する。

このAPIでは、`MyInvoiceListResultDto` をそのまま返却する。

#### 6.9.4 自分の請求書詳細

`/api/members/me/invoices/{id}` では、自分の請求書詳細を取得する。

取得前に、対象請求書がログイン会員のものであるかを確認する。

所有していない請求書の場合は、存在有無の漏洩を避けるため `404 Not Found` を返す。

#### 6.9.5 自分の請求書PDF

`/api/members/me/invoices/{id}/pdf` では、自分の請求書PDFを取得する。

PDF取得時も、詳細取得と同様に所有者チェックを行う。

所有者確認後、PDFを生成し、以下の形式で返却する。

| 項目 | 内容 |
| --- | --- |
| Content-Type | application/pdf |
| FileName | 請求番号.pdf |

### 6.10 メール送信設計

#### 6.10.1 概要

メール送信は `IEmailSender` の実装である `MailtrapEmailSender` が担当する。

本システムでは、開発・検証用途としてMailtrap SMTPを利用する。

#### 6.10.2 設定項目

メール送信に使用する設定値は以下のとおりである。

| 設定キー | 内容 |
| --- | --- |
| Mailtrap:Host | SMTPホスト |
| Mailtrap:Port | SMTPポート |
| Mailtrap:UserName | SMTPユーザー名 |
| Mailtrap:Password | SMTPパスワード |
| Mailtrap:From | 送信元メールアドレス |

`Mailtrap:Port` が未設定の場合は、既定値として `2525` を使用する。

`Mailtrap:From` が未設定の場合は、既定値として `no-reply@example.com` を使用する。

#### 6.10.3 送信方式

メール送信には `SmtpClient` を使用し、SMTP認証情報を設定したうえで `SendMailAsync` により非同期送信する。

メール本文はプレーンテキストとして送信する。

| 項目 | 内容 |
| --- | --- |
| EnableSsl | true |
| IsBodyHtml | false |

### 6.11 認証・アカウント関連の異常系

認証・アカウント関連で想定する主な異常系は以下のとおりである。

| ケース | 応答・処理 |
| --- | --- |
| 未認証ユーザーが会員APIへアクセス | Unauthorized |
| JWTに会員IDが含まれない | Unauthorized |
| 会員IDに対応する会員が存在しない | Unauthorized |
| 退会済み会員がAPIへアクセス | Unauthorized |
| 自分以外の請求書詳細へアクセス | NotFound |
| 自分以外の請求書PDFへアクセス | NotFound |
| プロフィール更新でName未入力 | BadRequest |
| プロフィール更新でEmail未入力 | BadRequest |
| パスワード再設定メールで存在しないメールを指定 | 成功扱い |
| パスワード再設定トークンが無効 | 失敗 |
| パスワード再設定トークンが期限切れ | 失敗 |
| パスワード再設定トークンが使用済み | 失敗 |

### 6.12 セキュリティ上の考慮

本章の認証・アカウント設計では、以下の点を考慮する。

| 観点 | 内容 |
| --- | --- |
| パスワード保護 | パスワードは平文保存せず、ハッシュ化して保存する |
| メール確認 | 登録時にメール確認トークンを発行する |
| トークン有効期限 | メール確認トークン、パスワード再設定トークンに有効期限を設ける |
| 退会後トークン対策 | JWTが残っていてもDB上の会員状態を確認する |
| 情報漏洩防止 | 他会員の請求書アクセス時はNotFoundを返す |
| メールアドレス列挙対策 | パスワード再設定要求では、存在しないメールでも成功扱いにする |
| 権限分離 | 管理者APIはAdminOnly、会員APIはMemberOnlyで保護する |

## 7. PDF・CSV出力設計

### 7.1 設計方針

本システムでは、請求書PDF出力および売上CSV出力を提供する。

PDF出力は、請求書を画面上で確認するだけでなく、外部提出・保管・印刷用途に利用できるようにする。

CSV出力は、売上一覧や会員別売上集計を表計算ソフトで確認・加工できるようにするために提供する。

出力処理は、APIエンドポイントから専用のServiceまたはBuilderを呼び出し、ファイルレスポンスとして返却する。

| 出力種別 | 主な用途 | 対象API |
| --- | --- | --- |
| 請求書PDF | 請求書の確認・印刷・保存 | `/api/invoices/{id}/pdf` |
| 売上一覧CSV | 請求単位の売上・入金・未回収確認 | `/api/sales/export` |
| 会員別売上CSV | 会員単位の請求・入金・未回収確認 | `/api/sales/by-member/export` |

PDF・CSV出力はいずれも業務データを含むため、認可制御を行う。

| 出力 | 認可 |
| --- | --- |
| 管理者向け請求書PDF | Admin または所有者 |
| 会員向け請求書PDF | ログイン会員本人 |
| 売上一覧CSV | AdminOnly |
| 会員別売上CSV | AdminOnly |

### 7.2 PDF出力設計

#### 7.2.1 概要

請求書PDF出力は、請求書詳細情報をもとにPDFファイルを生成し、APIレスポンスとして返却する機能である。

PDF生成処理は `IInvoiceService.GeneratePdfAsync` が担当する。

API側では、PDFのバイト配列を受け取り、`application/pdf` として返却する。

#### 7.2.2 対象API

| API | メソッド | 認可 | 内容 |
| --- | --- | --- | --- |
| `/api/invoices/{id}/pdf` | GET | ログイン必須、Adminまたは所有者 | 請求書PDF取得 |
| `/api/members/me/invoices/{id}/pdf` | GET | MemberOnly、本人所有チェック | 自分の請求書PDF取得 |

#### 7.2.3 管理者・所有者向けPDF取得

`/api/invoices/{id}/pdf` は、ログイン必須の請求書API配下に定義する。

取得前に `EnsureOwnerOrAdminAsync` により、以下のいずれかを満たすことを確認する。

| 条件 | 内容 |
| --- | --- |
| Admin | 管理者ロールを持つ |
| Owner | 請求書の `MemberId` がログイン会員IDと一致する |

認可に成功した場合、`GeneratePdfAsync` を呼び出してPDFバイト配列を取得し、ファイルとして返却する。

| 項目 | 内容 |
| --- | --- |
| Content-Type | `application/pdf` |
| FileName | `invoice-{id}.pdf` |

#### 7.2.4 会員向けPDF取得

`/api/members/me/invoices/{id}/pdf` は、会員自身の請求書PDF取得用APIである。

取得前に、対象請求書がログイン会員本人のものであるかDBで確認する。

所有していない請求書の場合は、存在有無の漏洩を防ぐため `404 Not Found` を返す。

認可に成功した場合、PDFを生成し、請求番号をファイル名として返却する。

| 項目 | 内容 |
| --- | --- |
| Content-Type | `application/pdf` |
| FileName | `{InvoiceNumber}.pdf` |

### 7.3 PDFフォント設計

#### 7.3.1 概要

請求書PDFでは日本語を扱うため、PDF生成時に日本語フォントを登録する。

フォント登録は `PdfFontRegistrar` が担当する。

#### 7.3.2 登録対象フォント

登録対象のフォントは以下のとおりである。

| フォント | 用途 | 必須 |
| --- | --- | --- |
| NotoSansJP-Regular.ttf | 通常文字 | 必須 |
| NotoSansJP-Bold.ttf | 太字 | 任意 |

フォントファイルは、実行ディレクトリ配下の以下パスから読み込む。

```text
Assets/Fonts/NotoSansJP-Regular.ttf
Assets/Fonts/NotoSansJP-Bold.ttf
```

#### 7.3.3 フォント登録処理

フォント登録処理では、`AppContext.BaseDirectory` を基準にフォントパスを組み立てる。

対象ファイルが存在する場合は、QuestPDFの `FontManager.RegisterFont` によりフォントを登録する。

フォントが存在しない場合は、ログへ出力する。通常フォントは必須扱いとし、太字フォントは任意扱いとする。

#### 7.3.4 フォント未配置時の扱い

フォントファイルが存在しない場合、アプリケーションは即時停止せず、コンソールに未検出ログを出力する。

ただし、通常フォントは日本語PDF表示に必要なため、デプロイ時には `NotoSansJP-Regular.ttf` が配置されていることを確認する。

### 7.4 CSV出力設計

#### 7.4.1 概要

CSV出力は、売上一覧および会員別売上集計を外部ファイルとして取得する機能である。

CSV生成は専用Builderで行い、APIエンドポイントでは生成された文字列をUTF-8 BOM付きのバイト配列に変換して返却する。

| Builder | 内容 |
| --- | --- |
| SalesCsvBuilder | 請求単位の売上一覧CSVを生成 |
| SalesByMemberCsvBuilder | 会員別売上集計CSVを生成 |

#### 7.4.2 文字コード

CSVは、Excelでの文字化けを避けるため、UTF-8 BOM付きで返却する。

| 項目 | 内容 |
| --- | --- |
| 文字コード | UTF-8 |
| BOM | あり |
| Content-Type | `text/csv; charset=utf-8` |

#### 7.4.3 改行コード

CSVの改行コードは、Windows環境で扱いやすいように CRLF とする。

```text
\r\n
```

#### 7.4.4 CSVエスケープ

CSV出力時、値に以下が含まれる場合はダブルクォートで囲む。

| 対象文字 |
| --- |
| `"` |
| `,` |
| 改行 `\n` |
| 復帰 `\r` |

値にダブルクォートが含まれる場合は、CSV仕様に従い `""` に置換する。

### 7.5 売上一覧CSV設計

#### 7.5.1 概要

売上一覧CSVは、請求書単位の売上、入金済み金額、未回収金額、最終入金日を出力するCSVである。

CSV生成は `SalesCsvBuilder` が担当する。

#### 7.5.2 対象API

| API | メソッド | 認可 | 内容 |
| --- | --- | --- | --- |
| `/api/sales/export` | GET | AdminOnly | 売上一覧CSV出力 |

#### 7.5.3 検索条件

CSV出力時は、画面の売上一覧と同様に検索条件を指定できる。

| パラメータ | 内容 |
| --- | --- |
| Year | 対象年。未指定時は現在年 |
| Month | 対象月。未指定時は全月 |
| Q | 検索キーワード |
| Status | all / unpaid / partial / paid |
| MemberId | 会員ID |

API側では、リクエスト値から `SalesSearchQuery` を作成し、`ISalesService.ExportAsync` を呼び出す。

#### 7.5.4 出力項目

売上一覧CSVのヘッダは以下のとおりである。

| 列 | 内容 |
| --- | --- |
| 請求番号 | 請求書番号 |
| 顧客名 | 会員名 |
| 発行日 | 請求日 |
| 期限 | 支払期限 |
| ステータス | 入金ステータス |
| 請求金額 | 請求金額 |
| 入金済 | 入金済み金額 |
| 残額 | 未回収金額 |
| 最終入金日 | 最終入金日 |

#### 7.5.5 書式

| 項目 | 書式 |
| --- | --- |
| 日付 | `yyyy/MM/dd` |
| 金額 | 小数なし |
| 最終入金日 | 未入金の場合は空文字 |

#### 7.5.6 ファイル名

ファイル名は、対象年月に応じて以下の形式とする。

| 条件 | ファイル名 |
| --- | --- |
| 月指定なし | `sales_{year}.csv` |
| 月指定あり | `sales_{year}-{month}.csv` |

例:

```text
sales_2026.csv
sales_2026-06.csv
```

### 7.6 会員別売上CSV設計

#### 7.6.1 概要

会員別売上CSVは、会員単位で請求合計、入金済み金額、未回収金額、回収率を出力するCSVである。

CSV生成は `SalesByMemberCsvBuilder` が担当する。

#### 7.6.2 対象API

| API | メソッド | 認可 | 内容 |
| --- | --- | --- | --- |
| `/api/sales/by-member/export` | GET | AdminOnly | 会員別売上CSV出力 |

#### 7.6.3 検索条件

会員別売上CSVでは、`SalesSearchRequest` の条件を使用する。

| パラメータ | 内容 |
| --- | --- |
| Year | 対象年。未指定時は現在年 |
| Month | 対象月。未指定時は全月 |
| Q | 検索キーワード |
| Page | ページ番号 |
| PageSize | 1ページ件数 |

#### 7.6.4 出力項目

会員別売上CSVのヘッダは以下のとおりである。

| 列 | 内容 |
| --- | --- |
| 顧客名 | 会員名 |
| 請求合計 | 請求金額合計 |
| 入金済 | 入金済み金額合計 |
| 未回収 | 未回収金額 |
| 回収率 | 回収率 |

#### 7.6.5 合計行

会員別売上CSVでは、明細行の後に合計行を出力する。

合計行には、`SalesSummaryDto` の以下の値を出力する。

| 列 | 内容 |
| --- | --- |
| 顧客名 | 合計 |
| 請求合計 | 請求金額合計 |
| 入金済 | 入金済み金額合計 |
| 未回収 | 未回収金額 |
| 回収率 | 全体回収率 |

#### 7.6.6 書式

| 項目 | 書式 |
| --- | --- |
| 金額 | 小数なし |
| 回収率 | 小数1桁 |

#### 7.6.7 ファイル名

ファイル名は、対象年月に応じて以下の形式とする。

| 条件 | ファイル名 |
| --- | --- |
| 月指定なし | `sales_by_member_{year}.csv` |
| 月指定あり | `sales_by_member_{year}-{month}.csv` |

例:

```text
sales_by_member_2026.csv
sales_by_member_2026-06.csv
```

### 7.7 売上出力API設計

#### 7.7.1 API一覧

売上関連APIは `/api/sales` 配下に定義し、すべて `AdminOnly` とする。

| API | メソッド | 内容 |
| --- | --- | --- |
| `/api/sales` | GET | 売上一覧取得 |
| `/api/sales/by-member` | GET | 会員別売上集計取得 |
| `/api/sales/export` | GET | 売上一覧CSV出力 |
| `/api/sales/by-member/export` | GET | 会員別売上CSV出力 |
| `/api/sales/by-member/worst-top5` | GET | 回収率ワースト顧客TOP5取得 |

#### 7.7.2 売上一覧取得

`/api/sales` では、`SalesSearchRequest` を受け取り、`ISalesService.SearchAsync` により売上一覧を取得する。

#### 7.7.3 会員別売上集計取得

`/api/sales/by-member` では、`SalesSearchRequest` を受け取り、`ISalesService.SearchByMemberAsync` により会員別売上集計を取得する。

#### 7.7.4 回収率ワースト顧客TOP5

`/api/sales/by-member/worst-top5` では、会員別売上集計結果から、未回収がある顧客のみを対象に回収率が低い順で上位5件を取得する。

並び順は以下のとおりである。

1. 回収率が低い順
2. 同率の場合、未回収金額が大きい順

Lite版では、十分大きい `PageSize` を指定して会員別集計を取得し、API側で上位5件を抽出する。

将来的には、専用Queryを用意し、DB側でTopN抽出を行う構成へ拡張する余地がある。

### 7.8 請求書PDF出力API設計

#### 7.8.1 API一覧

請求書PDF出力に関係するAPIは以下のとおりである。

| API | メソッド | 認可 | 内容 |
| --- | --- | --- | --- |
| `/api/invoices/{id}/pdf` | GET | ログイン必須、Adminまたは所有者 | 請求書PDF取得 |
| `/api/members/me/invoices/{id}/pdf` | GET | MemberOnly、本人所有 | 自分の請求書PDF取得 |

#### 7.8.2 `/api/invoices/{id}/pdf`

このAPIは、管理者または請求書所有者が使用できる。

処理手順は以下のとおりである。

1. JWT認証済みであることを確認する
2. 管理者でない場合、JWTの `sub` から会員IDを取得する
3. 請求書の所有者IDを取得する
4. 管理者または所有者であることを確認する
5. `GeneratePdfAsync` でPDFを生成する
6. `application/pdf` として返却する

#### 7.8.3 `/api/members/me/invoices/{id}/pdf`

このAPIは、会員本人が自分の請求書PDFを取得するために使用する。

処理手順は以下のとおりである。

1. JWTからログイン会員IDを取得する
2. 対象請求書がログイン会員に紐づくか確認する
3. 所有していない場合は `404 Not Found` を返す
4. `GeneratePdfAsync` でPDFを生成する
5. 請求番号を取得する
6. `{InvoiceNumber}.pdf` として返却する

### 7.9 出力ファイルの異常系

PDF・CSV出力で想定する主な異常系は以下のとおりである。

| ケース | 応答・処理 |
| --- | --- |
| 未ログインでPDF取得 | Unauthorized |
| 所有していない請求書PDF取得 | Forbidden または NotFound |
| 存在しない請求書PDF取得 | NotFound |
| PDF生成対象の請求書が存在しない | NotFound または業務例外 |
| 管理者以外が売上CSV取得 | Forbidden |
| CSV対象データなし | ヘッダのみ、または空の一覧CSVを返却 |
| 日本語フォント未配置 | コンソールログに出力 |
| CSV値にカンマ・改行・ダブルクォートを含む | CSVエスケープを行う |

会員向けAPIでは、他会員の請求書有無を推測されにくくするため、所有していない請求書に対して `NotFound` を返す。

### 7.10 今後の拡張余地

PDF・CSV出力機能はLite版として最小限の構成としている。

将来的な拡張余地は以下のとおりである。

| 拡張案 | 内容 |
| --- | --- |
| 請求書PDFレイアウト強化 | 会社ロゴ、振込先、税率、登録番号などの追加 |
| PDF再発行履歴 | PDF出力日時、出力者、再発行理由の記録 |
| CSV項目追加 | 税抜金額、消費税、会員ID、入金方法などの追加 |
| CSV文字コード選択 | UTF-8 / Shift_JIS の選択 |
| 非同期エクスポート | 大量データ出力時のバックグラウンド生成 |
| ダウンロード監査ログ | PDF・CSV出力操作のAuditLog記録 |
| 帳票テンプレート化 | PDFレイアウトをテンプレートとして管理 |

## 8. フロントエンド画面設計

### 8.1 フロントエンド設計方針

本システムのフロントエンドは、Next.js App Router を利用して構成する。

管理者向け画面と会員向け画面を分離し、ログインユーザーのロールに応じて表示する画面・アクセス可能な画面を制御する。

主な設計方針は以下のとおりである。

| 方針                 | 内容                                                                      |
| ------------------ | ----------------------------------------------------------------------- |
| App Router構成       | `src/app` 配下にURL単位で画面を配置する                                              |
| 管理者画面と会員画面の分離      | 管理者向けは `/dashboards/admin`、会員向けは `/dashboards/member`、`/account` 配下に分ける |
| Server Component活用 | 一覧・詳細など初期表示時に必要なデータはServer Componentで取得する                               |
| Client Component活用 | 入力フォーム、保存処理、モーダル、動的検索など画面操作が必要な部分はClient Componentで実装する                 |
| API通信の共通化          | サーバー側通信は `api.server.ts`、クライアント側通信は `api.client.ts` に集約する               |
| 画面認可               | `proxy.ts` により、未ログイン・ロール不一致の画面遷移を制御する                                   |
| 型定義                | バックエンドDTOに対応するTypeScript型を定義し、画面側のデータ構造を明確化する                           |
| レスポンシブ対応           | 管理画面・会員画面ともにPC/スマートフォン表示を考慮する                                           |

---

### 8.2 ルーティング構成

本システムの主なフロントエンドルーティングは以下のとおりである。

#### 8.2.1 認証関連画面

| URL                     | 画面           | 内容                   |
| ----------------------- | ------------ | -------------------- |
| `/auth/login`           | ログイン画面       | メールアドレス・パスワードでログイン   |
| `/auth/register`        | 会員登録画面       | 新規会員登録               |
| `/auth/forgot-password` | パスワード再設定依頼画面 | 再設定メール送信             |
| `/auth/reset-password`  | パスワード再設定画面   | トークン付きURLから新パスワードを設定 |
| `/auth/verify-email`    | メール確認画面      | メール確認トークンを送信して確認完了   |

#### 8.2.2 ダッシュボード

| URL                  | 画面         | 対象  |
| -------------------- | ---------- | --- |
| `/dashboards/admin`  | 管理者ダッシュボード | 管理者 |
| `/dashboards/member` | 会員ダッシュボード  | 会員  |

#### 8.2.3 管理者向け画面

| URL                        | 画面       | 内容                    |
| -------------------------- | -------- | --------------------- |
| `/invoices`                | 請求書一覧    | 請求番号、会員名、ステータス、請求日で検索 |
| `/invoices/new`            | 請求書作成    | 請求書ヘッダ・明細を登録          |
| `/invoices/[id]`           | 請求書詳細    | 請求情報、入金割当、督促履歴を表示     |
| `/invoices/[id]/edit`      | 請求書編集    | 請求書ヘッダ・明細を編集          |
| `/invoices/[id]/pdf`       | 請求書PDF中継 | バックエンドPDF APIへ中継      |
| `/payments`                | 入金一覧     | 入金日、名義、割当状況を一覧表示      |
| `/payments/new`            | 入金登録     | 入金情報を手動登録             |
| `/payments/[id]`           | 入金詳細・割当  | 入金を請求書へ割り当て           |
| `/members`                 | 会員一覧     | 会員検索、ロール、有効状態を表示      |
| `/members/[id]`            | 会員詳細・編集  | 管理者による会員情報編集          |
| `/sales`                   | 売上一覧     | 請求書単位の売上、入金、未回収を表示    |
| `/sales/by-member`         | 顧客別売上集計  | 会員単位で請求・入金・未回収を集計     |
| `/collections/[invoiceId]` | 督促管理     | 督促文面作成、履歴登録、次回対応日管理   |

#### 8.2.4 会員向け画面

| URL                          | 画面        | 内容                  |
| ---------------------------- | --------- | ------------------- |
| `/account/profile`           | プロフィール    | 自分の登録情報確認・変更        |
| `/account/invoices`          | 自分の請求書一覧  | 自分宛ての請求書を検索・確認      |
| `/account/invoices/[id]`     | 自分の請求書詳細  | 請求金額、入金済み、残額を確認     |
| `/account/invoices/[id]/pdf` | 自分の請求書PDF | 請求書PDFを別タブで表示       |
| `/account/unpaid`            | 未払い状況     | 未入金・一部入金の請求書をまとめて確認 |

---

### 8.3 認証画面設計

#### 8.3.1 ログイン画面

ログイン画面では、メールアドレスとパスワードを入力し、バックエンドのログインAPIへ送信する。

ログイン成功時は、APIから返却されたユーザー情報とJWTをブラウザ側に保存する。

| 保存先          | 保存内容          |
| ------------ | ------------- |
| localStorage | `currentUser` |
| Cookie       | `isLoggedIn`  |
| Cookie       | `role`        |
| Cookie       | `email`       |
| Cookie       | `token`       |

ログイン後は、ユーザーのロールに応じて遷移先を分ける。

| ロール    | 遷移先                  |
| ------ | -------------------- |
| Admin  | `/dashboards/admin`  |
| Member | `/dashboards/member` |

ログイン失敗時は、HTTPステータスに応じてエラーメッセージを表示する。

| ステータス | 表示内容              |
| ----- | ----------------- |
| 400   | 不正なリクエスト          |
| 401   | メールアドレスまたはパスワード不正 |
| 403   | 利用できないアカウント       |
| その他   | ログイン失敗            |

#### 8.3.2 会員登録画面

会員登録画面では、以下の項目を入力して会員登録APIへ送信する。

| 項目      | 内容 |
| ------- | -- |
| 氏名      | 必須 |
| メールアドレス | 必須 |
| パスワード   | 必須 |
| 郵便番号    | 任意 |
| 住所      | 任意 |
| 電話番号    | 任意 |

登録成功時は、成功メッセージを表示し、フォームをリセットする。

登録後は、バックエンド側でメール確認用メールが送信される。

#### 8.3.3 パスワード再設定依頼画面

パスワード再設定依頼画面では、登録済みメールアドレスを入力し、再設定リンク送信用APIへ送信する。

送信成功時は、再設定リンク送信済みメッセージを表示する。

#### 8.3.4 パスワード再設定画面

パスワード再設定画面では、URLクエリパラメータの `token` を取得し、新しいパスワードとともにバックエンドへ送信する。

| 条件      | 処理                   |
| ------- | -------------------- |
| tokenなし | 不正なリクエストとして表示        |
| 更新成功    | 完了メッセージ表示後、ログイン画面へ遷移 |
| 更新失敗    | エラーメッセージを表示          |

#### 8.3.5 メール確認画面

メール確認画面では、URLクエリパラメータの `token` を取得し、メール確認APIへ送信する。

画面状態は以下のとおりである。

| 状態       | 内容             |
| -------- | -------------- |
| loading  | 確認処理中          |
| success  | メール確認成功        |
| error    | トークン無効または通信エラー |
| no_token | トークン未指定        |

---

### 8.4 管理者ダッシュボード画面

管理者ダッシュボードでは、管理者が売上・未回収・入金状況を俯瞰できるようにする。

主な表示項目は以下のとおりである。

| 表示項目        | 内容            |
| ----------- | ------------- |
| 売上合計        | 対象年の請求金額合計    |
| 未入金額        | 未回収残額合計       |
| 請求書数        | 対象年の請求書件数     |
| 入金件数        | 対象年の入金件数      |
| 回収率         | 入金済み金額 / 請求金額 |
| 月別売上        | 月ごとの請求金額      |
| 未回収TOP5     | 未回収額が大きい請求書   |
| 回収率ワーストTOP5 | 回収率が低い顧客      |
| 最近の操作ログ     | 入金割当などの管理操作履歴 |

年度はURLクエリの `year` で切り替える。

```text
/dashboards/admin?year=2026
```

管理者ダッシュボードでは、バックエンドDTOを画面表示用の型へ変換し、画面で扱いやすい名称へマッピングする。

---

### 8.5 操作ログ一覧画面

管理者が請求・入金・督促などの操作履歴を確認する画面である。  
AuditLogs を作成日時の降順で取得し、操作日時、操作種別、対象、概要、操作者IDを一覧表示する。

---

### 8.6 会員ダッシュボード画面

会員ダッシュボードでは、会員本人が利用する主要機能への導線を表示する。

主な導線は以下のとおりである。

| 導線        | 内容                  |
| --------- | ------------------- |
| 自分の請求書一覧  | 請求書の金額、支払期限、入金状況を確認 |
| 入金確認（未入金） | 未入金・一部入金の請求書を確認     |
| 登録情報の確認   | 氏名、メール、住所などを確認・変更   |

会員向け画面では、管理機能を表示せず、自分に関係する情報のみ確認できるようにする。

---

### 8.7 請求書管理画面

#### 8.7.1 請求書一覧画面

請求書一覧画面では、管理者が全会員の請求書を検索・確認する。

検索条件は以下のとおりである。

| 条件       | 内容                   |
| -------- | -------------------- |
| 請求番号     | 部分一致検索               |
| 会員名      | 部分一致検索               |
| ステータス    | 未入金、一部入金、入金済み、期限超過など |
| 請求日From  | 請求日開始                |
| 請求日To    | 請求日終了                |
| Page     | ページ番号                |
| PageSize | 1ページ件数               |

一覧では、以下の情報を表示する。

| 表示項目  | 内容        |
| ----- | --------- |
| 請求番号  | 詳細画面へのリンク |
| 会員名   | 請求先       |
| 請求日   | 発行日       |
| 支払期限  | 期限        |
| ステータス | 入金状況      |
| 請求金額  | 合計金額      |

検索条件はURLクエリとして保持し、詳細画面から一覧へ戻る際に検索条件を維持する。

#### 8.7.2 請求書詳細画面

請求書詳細画面では、請求書のヘッダ情報、金額情報、入金割当、督促履歴を表示する。

主な表示項目は以下のとおりである。

| 表示項目  | 内容              |
| ----- | --------------- |
| 請求番号  | 請求書番号           |
| ステータス | 未入金、一部入金、入金済みなど |
| 会員名   | 請求先             |
| 請求日   | 発行日             |
| 支払期限  | 期限              |
| 請求金額  | 請求合計            |
| 入金済み  | 入金割当済み金額        |
| 残額    | 未回収金額           |
| 入金割当  | 紐づく入金情報         |
| 督促履歴  | 過去の督促記録         |

詳細画面からは、以下の操作へ遷移できる。

| 操作    | 遷移先                        |
| ----- | -------------------------- |
| 編集    | `/invoices/[id]/edit`      |
| 削除    | 削除処理                       |
| 督促    | `/collections/[invoiceId]` |
| 入金登録  | `/payments/new`            |
| PDF表示 | `/invoices/[id]/pdf`       |

#### 8.7.3 請求書作成画面

請求書作成画面では、請求書ヘッダ情報と明細行を入力して登録する。

入力項目は以下のとおりである。

| 項目    | 内容        |
| ----- | --------- |
| 請求番号  | 請求書番号     |
| 会員    | 請求先会員     |
| ステータス | 初期ステータス   |
| 請求日   | 発行日       |
| 支払期限  | 期限        |
| 備考    | 任意メモ      |
| 明細行   | 項目名、数量、単価 |

会員選択肢は `/api/members/options` から取得する。

明細行は動的に追加・削除でき、画面側で以下を算出する。

| 算出項目 | 内容        |
| ---- | --------- |
| 明細金額 | 数量 × 単価   |
| 小計   | 明細金額の合計   |
| 税    | Lite版では0円 |
| 合計   | 小計 + 税    |

保存時は、バックエンドの `UpdateInvoiceRequestDto` に合わせた形式へ変換して送信する。

#### 8.7.4 請求書編集画面

請求書編集画面では、既存の請求書詳細を取得し、作成画面と同じフォームコンポーネントへ初期値として渡す。

編集時は既存明細行のIDを保持し、新規行はIDなしとして扱う。

以下のステータスでは編集不可とする。

| ステータス | 画面制御 |
| ----- | ---- |
| 入金済み  | 保存不可 |
| キャンセル | 保存不可 |

編集不可の場合、入力項目と保存ボタンを無効化し、閲覧専用である旨を表示する。

---

### 8.8 入金管理画面

#### 8.8.1 入金一覧画面

入金一覧画面では、管理者が入金データを検索・確認する。

検索条件は以下のとおりである。

| 条件       | 内容           |
| -------- | ------------ |
| 年        | 対象年          |
| 月        | 対象月          |
| キーワード    | 入金名義など       |
| ステータス    | 未割当、一部割当、割当済 |
| Page     | ページ番号        |
| PageSize | 1ページ件数       |

一覧では、以下の情報を表示する。

| 表示項目   | 内容           |
| ------ | ------------ |
| 入金ID   | 表示用ID        |
| 入金日    | 入金日          |
| 入金名義   | 振込名義         |
| 入金額    | 入金金額         |
| 割当済み金額 | 請求書へ割当済みの金額  |
| 紐づく請求書 | 割当先請求書       |
| ステータス  | 未割当、一部割当、割当済 |

サマリーとして以下を表示する。

| サマリー   | 内容           |
| ------ | ------------ |
| 入金合計   | 検索条件内の入金金額合計 |
| 割当済み合計 | 割当済み金額合計     |
| 未割当合計  | 未割当金額合計      |

#### 8.8.2 入金登録画面

入金登録画面では、入金情報を手動で登録する。

入力項目は以下のとおりである。

| 項目   | 内容                 |
| ---- | ------------------ |
| 会員   | 入金元会員              |
| 入金日  | 入金日                |
| 入金名義 | 振込名義               |
| 入金額  | 入金金額               |
| 入金方法 | 振込、現金、クレジットカード、その他 |

会員一覧は有効な一般会員を対象に取得する。

会員選択時、入金名義には会員名を初期値として自動入力する。ただし、振込名義が異なる場合を考慮し、手動編集を許可する。

登録前には以下を検証する。

| 検証   | 内容              |
| ---- | --------------- |
| 会員   | 必須              |
| 入金日  | 必須、YYYY-MM-DD形式 |
| 入金名義 | 必須              |
| 入金額  | 1以上の数値          |

登録成功後は、作成された入金IDをもとに入金詳細・割当画面へ遷移する。

#### 8.8.3 入金詳細・割当画面

入金詳細・割当画面では、1件の入金を複数の請求書へ割り当てる。

画面上部には以下のサマリーを表示する。

| 表示項目  | 内容           |
| ----- | ------------ |
| 入金ID  | 入金ID         |
| ステータス | 未割当、一部割当、割当済 |
| 入金額   | 入金金額         |
| 割当合計  | 現在の割当済み金額    |
| 入力中合計 | 画面で入力中の割当合計  |
| 未割当   | 未割当金額        |
| 入金日   | 入金日          |
| 入金名義  | 振込名義         |

割当入力では、請求書番号または会員名で請求書を検索し、候補から選択する。

検索は入力中に即時実行せず、短い待機時間を置いてから実行することで、API呼び出し回数を抑制する。

割当保存前には以下を検証する。

| 検証    | 内容               |
| ----- | ---------------- |
| 請求書選択 | 各行で請求書が選択されていること |
| 割当金額  | 1以上の数値           |
| 重複請求書 | 同じ請求書を複数行で選択しない  |
| 割当合計  | 入金額を超えない         |

保存時は、割当行をAPIへ送信する。

保存成功後は入金詳細を再取得し、画面表示を最新状態へ更新する。

---

### 8.9 会員管理画面

#### 8.9.1 会員一覧画面

会員一覧画面では、管理者が会員情報を検索・確認する。

検索条件は以下のとおりである。

| 条件       | 内容           |
| -------- | ------------ |
| キーワード    | 名前またはメールアドレス |
| ロール      | 管理者、一般会員、退会  |
| 有効状態     | 有効、無効        |
| Page     | ページ番号        |
| PageSize | 1ページ件数       |

一覧では、以下の情報を表示する。

| 表示項目    | 内容          |
| ------- | ----------- |
| 会員ID    | 会員ID        |
| 氏名      | 会員名         |
| メールアドレス | ログインID      |
| ロール     | 管理者、一般会員、退会 |
| 有効状態    | 有効または無効     |

会員一覧から会員詳細画面へ遷移し、管理者が会員情報を編集できる。

#### 8.9.2 会員詳細・編集画面

会員詳細・編集画面では、管理者が会員情報を確認・編集する。

主な項目は以下のとおりである。

| 項目      | 内容          |
| ------- | ----------- |
| 氏名      | 会員名         |
| メールアドレス | ログインID      |
| 郵便番号    | 任意          |
| 住所      | 任意          |
| 電話番号    | 任意          |
| ロール     | 管理者、一般会員、退会 |
| 有効状態    | 有効、無効       |

管理者画面では、メール認証処理は行わず、管理者による直接編集として扱う。

---

### 8.10 売上管理画面

#### 8.10.1 売上一覧画面

売上一覧画面では、請求書単位で売上、入金済み金額、未回収金額を確認する。

検索条件は以下のとおりである。

| 条件       | 内容           |
| -------- | ------------ |
| 年        | 対象年          |
| 月        | 対象月          |
| ステータス    | 未入金、一部入金、入金済 |
| キーワード    | 請求番号、顧客名など   |
| 会員ID     | 顧客別集計からの絞り込み |
| Page     | ページ番号        |
| PageSize | 1ページ件数       |

サマリーとして以下を表示する。

| サマリー   | 内容            |
| ------ | ------------- |
| 請求合計   | 請求金額合計        |
| 入金済み合計 | 入金済み金額合計      |
| 残額     | 未回収金額         |
| 回収率    | 入金済み金額 / 請求合計 |

一覧では、以下の情報を表示する。

| 表示項目  | 内容           |
| ----- | ------------ |
| 請求書   | 請求書番号        |
| 顧客    | 会員名          |
| 発行日   | 請求日          |
| 期限    | 支払期限         |
| ステータス | 未入金、一部入金、入金済 |
| 請求    | 請求金額         |
| 入金済   | 入金済み金額       |
| 残額    | 未回収金額        |
| 最終入金  | 最終入金日        |

PC表示ではテーブル、スマートフォン表示ではカード形式で表示する。

#### 8.10.2 顧客別売上集計画面

顧客別売上集計画面では、会員単位で請求合計、入金済み、未回収、回収率を表示する。

表示項目は以下のとおりである。

| 表示項目 | 内容         |
| ---- | ---------- |
| 顧客   | 会員名        |
| 請求合計 | 会員別の請求金額合計 |
| 入金済  | 会員別の入金済み金額 |
| 未回収  | 会員別の残額     |
| 回収率  | 会員別の回収率    |

顧客名をクリックすると、対象会員IDで絞り込んだ売上一覧画面へ遷移する。

```text
/sales?memberId={memberId}
```

#### 8.10.3 売上検索フィルタ

売上一覧では、`SalesFiltersClient` により、年、月、ステータス、キーワード検索を提供する。

フィルタ変更時は、ページ番号を1に戻す。

| 操作      | 処理         |
| ------- | ---------- |
| 年変更     | URLクエリを更新  |
| 月変更     | URLクエリを更新  |
| ステータス変更 | URLクエリを更新  |
| キーワード検索 | URLクエリを更新  |
| リセット    | 年以外の条件を初期化 |

---

### 8.11 CSV出力画面部品

CSV出力は、`CsvExportButton` により実行する。

CSV出力ボタンでは、現在のURLクエリからCSV出力条件を組み立てる。

引き継ぐ条件は以下のとおりである。

| 条件       | 内容    |
| -------- | ----- |
| year     | 対象年   |
| month    | 対象月   |
| status   | ステータス |
| q        | キーワード |
| memberId | 会員ID  |

CSV出力前には確認モーダルを表示する。

確認モーダルでは以下を表示する。

| 表示項目  | 内容                   |
| ----- | -------------------- |
| ファイル名 | 出力予定ファイル名            |
| 対象件数  | 推定件数                 |
| 顧客名   | 会員IDで絞り込み中の場合の顧客名    |
| 注意文   | 画面のページングに関係なく全件出力する旨 |

CSVはBlobとして取得し、ブラウザ上でダウンロードする。

バックエンドが `Content-Disposition` にファイル名を返した場合はそれを優先し、存在しない場合はフロント側で組み立てたファイル名を使用する。

---

### 8.12 督促管理画面

督促管理画面では、未回収請求書に対する督促文面作成、履歴登録、次回対応日管理を行う。

#### 8.12.1 初期表示

初期表示時に以下のAPIを呼び出す。

| API                                     | 内容            |
| --------------------------------------- | ------------- |
| `/api/collections/{invoiceId}/snapshot` | 請求書スナップショット取得 |
| `/api/collections/{invoiceId}/logs`     | 督促履歴取得        |

スナップショットには、請求書番号、顧客名、メールアドレス、請求日、支払期限、請求合計、入金済み金額を含む。

未回収残額は、画面側で以下により算出する。

```text
未回収残額 = 請求合計 - 入金済み金額
```

#### 8.12.2 督促設定

督促設定では、以下を選択できる。

| 項目       | 選択肢                    |
| -------- | ---------------------- |
| チャネル     | EMAIL / PHONE / LETTER |
| トーン      | SOFT / NORMAL / STRONG |
| 次回アクション日 | 日付入力                   |

チャネルとトーンは画面上のボタンで選択する。

#### 8.12.3 文面テンプレート

督促文面は、選択したトーンに応じて自動生成する。

| トーン    | 内容            |
| ------ | ------------- |
| SOFT   | 確認依頼に近い柔らかい文面 |
| NORMAL | 標準的な支払い依頼     |
| STRONG | 至急確認を促す文面     |

文面には以下の情報を埋め込む。

| 項目    | 内容    |
| ----- | ----- |
| 顧客名   | 宛名    |
| 請求書番号 | 対象請求書 |
| 支払期限  | 支払期限  |
| 未回収残額 | 残額    |

ブラウザがクリップボードAPIに対応している場合、本文コピー機能を表示する。

#### 8.12.4 督促履歴登録

「送信完了として記録」ボタン押下時に確認ダイアログを表示し、確認後に督促履歴登録APIへ送信する。

登録内容は以下のとおりである。

| 項目             | 内容           |
| -------------- | ------------ |
| channel        | 督促チャネル       |
| tone           | 督促トーン        |
| title          | 初回督促またはN回目督促 |
| memo           | メモ           |
| nextActionDate | 次回対応日        |
| subject        | 件名           |
| bodyText       | 本文           |

登録成功後は督促履歴を再取得し、画面を更新する。

未回収残額が0円の場合は、督促履歴登録を行わない。

---

### 8.13 会員向けマイページ画面

#### 8.13.1 プロフィール画面

プロフィール画面では、会員本人が登録情報を確認・変更する。

表示・編集項目は以下のとおりである。

| 項目      | 内容 |
| ------- | -- |
| 氏名      | 必須 |
| メールアドレス | 必須 |
| 電話番号    | 任意 |
| 郵便番号    | 任意 |
| 住所      | 任意 |

保存前には、確認モーダルを表示する。

メールアドレスが変更された場合は、変更前と変更後を確認モーダル内に表示する。

Lite版では、メール変更後の確認メール再送信UIは省略し、注意メッセージのみ表示する。

退会ボタン押下時は確認ダイアログを表示し、退会APIを呼び出す。

退会成功後はログアウト処理を行い、ログイン画面へ遷移する。

#### 8.13.2 自分の請求書一覧画面

自分の請求書一覧画面では、ログイン会員本人に紐づく請求書のみ表示する。

検索条件は以下のとおりである。

| 条件       | 内容           |
| -------- | ------------ |
| 年        | 対象年          |
| 月        | 対象月          |
| ステータス    | 未入金、一部入金、入金済 |
| キーワード    | 請求書番号など      |
| Page     | ページ番号        |
| PageSize | 1ページ件数       |

表示項目は以下のとおりである。

| 表示項目  | 内容     |
| ----- | ------ |
| 請求書番号 | 請求書番号  |
| 請求日   | 発行日    |
| 支払期限  | 期限     |
| 請求金額  | 請求合計   |
| ステータス | 入金状況   |
| 操作    | 詳細、PDF |

期限超過かつ未入金の場合は、期限超過ラベルを表示する。

PDFは別タブで表示する。

#### 8.13.3 自分の請求書詳細画面

自分の請求書詳細画面では、以下の情報を表示する。

| 表示項目  | 内容     |
| ----- | ------ |
| 請求書番号 | 請求番号   |
| ステータス | 入金状況   |
| 請求日   | 発行日    |
| 支払期限  | 期限     |
| 請求金額  | 請求合計   |
| 入金済み  | 入金済み金額 |
| 残額    | 未回収金額  |
| 備考    | 任意メモ   |

支払期限を過ぎており、残額がある場合は期限超過ラベルを表示する。

#### 8.13.4 未払い状況画面

未払い状況画面では、未入金および一部入金の請求書をまとめて表示する。

画面では以下を集計する。

| 集計項目    | 内容            |
| ------- | ------------- |
| 未払い件数   | 未入金・一部入金の件数   |
| 未払い残額合計 | 残額合計          |
| 期限超過件数  | 支払期限を過ぎた未払い件数 |

一覧は支払期限が近い順に並べる。

PDF表示と詳細画面への導線を提供する。

---

### 8.14 PDF表示設計

管理者向け請求書PDF表示では、Next.js Route Handlerを利用してバックエンドPDF APIへ中継する。

処理手順は以下のとおりである。

1. CookieからJWTを取得する
2. JWTが存在しない場合は401を返す
3. バックエンドの `/api/invoices/{id}/pdf` を呼び出す
4. AuthorizationヘッダにBearerトークンを設定する
5. PDFバイト列を受け取る
6. `application/pdf` としてブラウザへ返却する
7. キャッシュを無効化する

返却ヘッダは以下のとおりである。

| ヘッダ                 | 内容                                                  |
| ------------------- | --------------------------------------------------- |
| Content-Type        | application/pdf                                     |
| Content-Disposition | バックエンド返却値、または `inline; filename="invoice-{id}.pdf"` |
| Cache-Control       | no-store                                            |

---

### 8.15 API通信設計

#### 8.15.1 サーバー側API通信

Server ComponentからバックエンドAPIを呼び出す場合は、`apiGetServer` を使用する。

`apiGetServer` はCookieからJWTを取得し、Authorizationヘッダへ設定する。

主な利用画面は以下のとおりである。

| 画面         | 用途       |
| ---------- | -------- |
| 管理者ダッシュボード | サマリー取得   |
| 請求書一覧      | 請求書検索    |
| 請求書詳細      | 請求書詳細取得  |
| 入金一覧       | 入金検索     |
| 会員一覧       | 会員検索     |
| 売上一覧       | 売上検索     |
| 会員向け請求書一覧  | 自分の請求書検索 |

#### 8.15.2 クライアント側API通信

Client ComponentからAPIを呼び出す場合は、`api.client.ts` の共通関数を使用する。

主な関数は以下のとおりである。

| 関数               | 用途                 |
| ---------------- | ------------------ |
| apiGetClient     | GET                |
| apiPostClient    | POST               |
| apiPutClient     | PUT                |
| apiDeleteClient  | DELETE             |
| apiGetBlobClient | Blob取得、CSVダウンロードなど |

`api.client.ts` では、localStorageの `currentUser` からJWTを取得し、Authorizationヘッダへ設定する。

#### 8.15.3 API通信の補足

一部の画面では、Next.js側のRoute Handlerを経由するため、`fetch("/api/...")` を直接使用している。

今後の保守性向上のため、API通信方式は以下の方針で整理する余地がある。

| 場所                      | 推奨方針                    |
| ----------------------- | ----------------------- |
| Server Component        | `apiGetServer` に統一      |
| Client Component        | `api.client.ts` に統一     |
| Next.js Route Handler経由 | `fetch("/api/...")` を使用 |

---

### 8.16 ログアウト確認ダイアログ

ログアウト操作時に確認ダイアログを表示し、誤操作によるログアウトを防止する。  
確定時は認証情報を破棄し、ログイン画面へ遷移する。

---

### 8.17 画面認可制御

画面認可は `proxy.ts` で制御する。

未ログインユーザーが保護対象画面へアクセスした場合は、ログイン画面へリダイレクトする。

#### 8.17.1 管理者専用画面

以下のパスは管理者専用とする。

| パス                  | 内容         |
| ------------------- | ---------- |
| `/dashboards/admin` | 管理者ダッシュボード |
| `/invoices`         | 請求書管理      |
| `/members`          | 会員管理       |
| `/payments`         | 入金管理       |
| `/sales`            | 売上管理       |
| `/collections`      | 督促管理       |

ロールが `Member` の場合は、会員ダッシュボードへリダイレクトする。

#### 8.17.2 会員専用画面

以下のパスは会員専用とする。

| パス                   | 内容        |
| -------------------- | --------- |
| `/dashboards/member` | 会員ダッシュボード |
| `/account`           | 会員マイページ   |

ロールが `Admin` の場合は、管理者ダッシュボードへリダイレクトする。

#### 8.17.3 ログイン不要画面

以下はログイン不要とする。

| パス            | 内容                |
| ------------- | ----------------- |
| `/auth/login` | ログイン              |
| `/`           | トップ               |
| `/_next`      | Next.js内部リソース     |
| `/favicon`    | favicon           |
| `/api`        | Next.js API Route |

---

### 8.18 型定義設計

フロントエンドでは、バックエンドDTOに対応するTypeScript型を定義する。

主な型定義は以下のとおりである。

| 型                           | 用途          |
| --------------------------- | ----------- |
| InvoiceDto                  | 請求書一覧       |
| InvoiceDetailDto            | 請求書詳細       |
| InvoiceLineDto              | 請求明細        |
| InvoicePaymentAllocationDto | 請求書に紐づく入金割当 |
| InvoiceReminderHistoryDto   | 督促履歴        |
| MemberListItemDto           | 会員一覧        |
| AccountInvoiceListDto       | 会員向け請求書一覧   |
| DashboardSummary            | ダッシュボードサマリー |
| CurrentUser                 | ログインユーザー情報  |

一部の型定義には、開発途中の互換用または旧画面用の型が残っている。

今後は以下の整理を行う余地がある。

| 整理対象             | 内容                              |
| ---------------- | ------------------------------- |
| InvoiceDetailDto | 重複定義の統合                         |
| Invoice型         | 古い簡易モデルと現行DTOの整理                |
| APIレスポンス型        | バックエンドDTOとの命名統一                 |
| Role型            | Admin / Member とバックエンドRole表現の整合 |

---

### 8.19 フロントエンド側入力チェック

フロントエンドでは、ユーザー操作性向上のため、保存前に簡易的な入力チェックを行う。

ただし、最終的な業務ルール・整合性チェックはバックエンド側でも行う。

#### 8.19.1 請求書フォーム

| チェック  | 内容   |
| ----- | ---- |
| 請求番号  | 必須   |
| 会員    | 必須   |
| 明細項目名 | 必須   |
| 数量    | 1以上  |
| 単価    | 0以上  |
| 入金済み  | 編集不可 |
| キャンセル | 編集不可 |

#### 8.19.2 入金登録フォーム

| チェック | 内容              |
| ---- | --------------- |
| 会員   | 必須              |
| 入金日  | 必須、YYYY-MM-DD形式 |
| 入金名義 | 必須              |
| 入金額  | 1以上             |

#### 8.19.3 入金割当フォーム

| チェック  | 内容            |
| ----- | ------------- |
| 請求書   | 候補から選択必須      |
| 割当金額  | 1以上           |
| 重複請求書 | 同一請求書の複数行指定不可 |
| 割当合計  | 入金額を超えない      |

#### 8.19.4 プロフィールフォーム

| チェック    | 内容       |
| ------- | -------- |
| 氏名      | 必須       |
| メールアドレス | 必須       |
| メール形式   | 簡易形式チェック |

---

### 8.20 画面設計上の補足

本システムのフロントエンドは、Lite版として必要な機能を優先して実装している。

今後の拡張余地は以下のとおりである。

| 拡張案               | 内容                              |
| ----------------- | ------------------------------- |
| API通信の統一          | `fetch` 直接呼び出し箇所を共通API関数へ統一     |
| 型定義の整理            | 重複型、旧型の削除                       |
| 入力バリデーション強化       | Zodなどによるスキーマベース検証               |
| エラーハンドリング共通化      | Toastや共通ErrorBoundaryの導入        |
| ローディングUI共通化       | Skeletonや共通Loading部品の導入         |
| 画面テスト追加           | React Testing Libraryによる主要画面テスト |
| CSV/PDF操作ログ       | フロント操作から監査ログ連携                  |
| 会員プロフィールのメール再認証UI | メール変更時の確認メール再送信導線               |
| 入金CSV取込           | 手動入金登録に加え、CSV取込を追加              |
| 督促自動化             | 次回対応日に基づく通知・リマインド               |

## 9. テスト・CI設計

### 9.1 テスト設計方針

本システムでは、バックエンドAPI、業務ロジック、フロントエンド画面、CI実行を対象にテストを実施する。

テストの目的は、以下のとおりである。

| 観点        | 目的                                           |
| --------- | -------------------------------------------- |
| 認証・認可     | ロールによるアクセス制御が正しく機能することを確認する                  |
| 異常系       | 存在しないデータ、権限不足などに対して適切なHTTPステータスを返すことを確認する    |
| 業務ロジック    | 入金割当による請求ステータス再計算が正しく行われることを確認する             |
| フロントエンド画面 | 主要画面の表示、ユーザー操作、エラー表示、遷移が正しく動作することを確認する       |
| CI        | push / pull request 時にビルド・テストが自動実行されることを確認する |

テストは、以下の分類で実施する。

| 分類            | 対象                       |
| ------------- | ------------------------ |
| バックエンド統合テスト   | API、認証、認可、HTTPステータス      |
| バックエンドサービステスト | 業務ロジック、DB更新、ステータス再計算     |
| フロントエンドテスト    | Reactコンポーネント、画面表示、ユーザー操作 |
| CIテスト         | GitHub Actionsによる自動実行    |

---

### 9.2 バックエンド統合テスト設計

バックエンド統合テストでは、`WebApplicationFactory<Program>` を使用して ASP.NET Core アプリケーションをテスト環境で起動し、HTTPクライアント経由でAPIエンドポイントを検証する。

単体テストがサービスクラス単体の業務ロジックを確認するのに対し、統合テストでは、認証・認可、ルーティング、HTTPステータス、JWT、DBアクセス、エンドポイントとサービス層の接続が正しく機能することを確認する。

#### 9.2.1 統合テスト対象

| テストクラス | 主な対象 | 主な確認内容 |
| --- | --- | --- |
| AdminOnly_ForbiddenTests | 管理者専用API | 401、403、Admin権限での200 |
| AdminOperationLogsEndpointTests | 操作ログAPI | 未認証401、Member403、Admin200、recent取得 |
| AuthEndpointTests | 認証API | ログイン成功、パスワード不一致401、メール未確認400 |
| CollectionEndpointTests | 督促API | snapshot取得、履歴取得、履歴登録、AdminOnly制御 |
| Invoice_NotFoundTests | 請求書詳細API | 存在しない請求書IDの404 |
| InvoiceEndpointAuthorizationTests | 請求書API | 未認証401、所有者Member200、他Member403、Admin検索200 |
| MemberEndpointTests | 会員API | 未認証401、Member403、Admin200、NotFound404 |
| MyAccountEndpointTests | 自分の会員情報API | 未認証401、Admin403、Member200 |
| PaymentEndpointTests | 入金API | 未認証401、Member403、Admin200、登録正常系201、バリデーション400 |
| SalesEndpointTests | 売上API | 未認証401、Member403、Admin200、CSV出力 |

---

### 9.3 AdminOnly認可テスト

本節では、バックエンド統合テストの代表例として、管理者専用APIに対する認可制御テストを示す。

#### 9.3.1 テスト目的

管理者専用APIに対して、一般会員ロールのJWTでアクセスした場合に、`403 Forbidden` が返却されることを確認する。

#### 9.3.2 対象API

| API                            | メソッド | 認可        |
| ------------------------------ | ---- | --------- |
| `/api/admin/summary?year=2026` | GET  | AdminOnly |

#### 9.3.3 テスト内容

テストでは、ログインAPIを使用せず、Memberロールを持つJWTをテスト側で生成する。

生成したJWTを `Authorization: Bearer {token}` として設定し、管理者専用APIへアクセスする。

期待結果は以下のとおりである。

| 条件                           | 期待結果          |
| ---------------------------- | ------------- |
| MemberロールでAdminOnly APIへアクセス | 403 Forbidden |

#### 9.3.4 テスト設定

テスト用JWTでは、以下の設定をインメモリで上書きする。

| 設定キー               | 内容           |
| ------------------ | ------------ |
| Jwt:Key            | テスト用署名キー     |
| Jwt:Issuer         | テスト用Issuer   |
| Jwt:Audience       | テスト用Audience |
| Jwt:ExpiresMinutes | 有効期限         |

`TestingFactory` では `ConfigureAppConfiguration` により、DB接続文字列とJWT設定をテスト用に差し替える。

---

### 9.4 請求書NotFoundテスト

#### 9.4.1 テスト目的

本節では、バックエンド統合テストの代表例として、存在しない請求書IDを指定した場合の NotFound 応答テストを示す。

存在しない請求書IDを指定して請求書詳細APIへアクセスした場合に、`404 NotFound` が返却されることを確認する。

#### 9.4.2 対象API

| API                  | メソッド | 認可     |
| -------------------- | ---- | ------ |
| `/api/invoices/{id}` | GET  | ログイン必須 |

#### 9.4.3 テスト内容

テストでは、管理者アカウントでログインし、ログインレスポンスからJWTを取得する。

取得したJWTをAuthorizationヘッダに設定し、存在しない請求書IDを指定してAPIを呼び出す。

期待結果は以下のとおりである。

| 条件            | 期待結果         |
| ------------- | ------------ |
| 存在しない請求書IDを取得 | 404 NotFound |

#### 9.4.4 トークン取得処理

ログインAPIのレスポンス形式に差異があっても対応できるよう、以下のキーからJWTを探索する。

| 探索キー         |
| ------------ |
| accessToken  |
| AccessToken  |
| token        |
| Token        |
| jwt          |
| Jwt          |
| access_token |

また、レスポンス直下だけでなく、`data`、`result`、`payload` 配下も探索する。

これにより、ログインレスポンス形式の軽微な変更に対してテストが壊れにくくなる。

---

### 9.5 バックエンド単体テスト設計

バックエンドサービステストでは、API経由ではなくサービスクラスを直接呼び出し、業務ロジックの正しさを確認する。

本システムでは、PaymentService だけでなく、請求、入金、会員、認証、売上集計、督促、監査ログ、非同期ジョブ処理などのサービスクラスを対象に単体テストを実施する。

API経由ではなくサービスクラスを直接呼び出すことで、HTTP層に依存せず、業務ロジック、DB更新、集計処理、ステータス再計算、外部依存の差し替えが正しく機能することを確認する。

#### 9.5.1 単体テスト対象

| テスト対象 | 主な確認内容 |
| --- | --- |
| AdminSummaryService | 年次サマリー、月別集計、未回収金額、回収率、未回収TOP5 |
| AdminOperationLogService | 操作ログの降順取得、取得件数制限、ページング、検索条件 |
| AuditLogger | 監査ログ作成、Actor必須、DataJson、操作主体情報 |
| PasswordResetService | 再設定トークン作成、メール送信、期限切れ、使用済みトークン |
| MemberRegistrationService | 会員登録、メール確認トークン、重複メール、ロール設定 |
| MemberService | 会員登録、検索、詳細取得、更新、無効化 |
| InvoiceService | 請求書作成、明細行正規化、合計金額再計算、更新 |
| PaymentService | 入金登録、入金割当、割当置換、請求ステータス再計算 |
| CollectionService | 督促対象スナップショット、督促履歴取得、督促履歴登録 |
| ReminderJobProcessor | Pendingジョブ処理、メール送信成功、失敗時リトライ、Failed化 |
| ReminderJobWorker | BackgroundService起動、例外時の継続動作 |
| SalesService | 売上一覧、会員別集計、入金済額、未回収額、CSV出力 |

#### 9.5.2 テストDB

サービステストでは、SQLite in-memory を使用する。

| 項目        | 内容                            |
| --------- | ----------------------------- |
| DB        | SQLite in-memory              |
| 接続文字列     | `DataSource=:memory:`         |
| DbContext | AppDbContext                  |
| DB初期化     | `Database.EnsureCreated()`    |
| 接続維持      | テスト中はSqliteConnectionを開いたまま保持 |

SQLite in-memory を利用することで、実DBに依存せず高速に業務ロジックを検証する。

#### 9.5.3 外部依存の差し替え

単体テストでは、メール送信、監査ログ、非同期ジョブ処理などの外部依存を Fake / Noop / Spy 実装に差し替える。

これにより、外部サービスや実際のメール送信に依存せず、サービス単体の業務ロジックと副作用を検証する。

---

### 9.6 入金割当ステータス再計算テスト

本節では、バックエンド単体テストの代表例として、PaymentService における入金割当と請求ステータス再計算のテスト内容を示す。

#### 9.6.1 UNPAID → PARTIAL → PAID テスト

##### テスト目的

入金割当を登録・置換した場合に、請求書ステータスが入金状況に応じて再計算されることを確認する。

##### テストデータ

| データ | 内容                              |
| --- | ------------------------------- |
| 会員  | 有効なCustomer会員                   |
| 請求書 | TotalAmount = 1000、StatusId = 1 |
| 入金  | Amount = 1000                   |

##### テスト手順

1. 請求書を未入金状態で作成する
2. 入金1000円を作成する
3. 200円を請求書へ割り当てる
4. 請求書ステータスが `PARTIAL` になることを確認する
5. 割当を1000円に置き換える
6. 請求書ステータスが `PAID` になることを確認する

##### 期待結果

| 割当状態    | 期待ステータス |
| ------- | ------- |
| 200円割当  | PARTIAL |
| 1000円割当 | PAID    |

---

#### 9.6.2 期限超過時のOVERDUEテスト

##### テスト目的

支払期限を過ぎた未入金請求書について、割当がなくなった場合に `OVERDUE` として再計算されることを確認する。

##### テストデータ

| データ  | 内容                              |
| ---- | ------------------------------- |
| 会員   | 有効な会員                           |
| 請求書  | TotalAmount = 1000、StatusId = 1 |
| 支払期限 | 現在日時より過去                        |
| 入金   | Amount = 1000                   |

##### テスト手順

1. 支払期限を過ぎた未入金請求書を作成する
2. 一度100円を割り当てる
3. 割当を空にして保存する
4. 請求書ステータスが `OVERDUE` になることを確認する

##### 期待結果

| 条件           | 期待ステータス |
| ------------ | ------- |
| 支払期限超過、かつ未入金 | OVERDUE |

---

### 9.7 フロントエンドテスト設計

フロントエンドテストでは、Jest と React Testing Library を使用する。

画面表示、ユーザー操作、API呼び出し、エラー表示、ページ遷移を検証する。

#### 9.7.1 Jest設定

JestはNext.js向け設定として `next/jest` を利用する。

| 設定                 | 内容                                          |
| ------------------ | ------------------------------------------- |
| testEnvironment    | jest-environment-jsdom                      |
| setupFilesAfterEnv | `tests/setup/jest.setup.ts`                 |
| testMatch          | `tests/**/*.test.ts`, `tests/**/*.test.tsx` |
| moduleNameMapper   | `@/` を `src/` へマッピング                        |

#### 9.7.2 テスト初期設定

`jest.setup.ts` では、以下を設定する。

| 設定                          | 内容              |
| --------------------------- | --------------- |
| jest-dom                    | DOM拡張Matcherを利用 |
| NEXT_PUBLIC_API_BASE_URL    | テスト用APIベースURL   |
| NEXT_PUBLIC_SHOW_DEMO_LOGIN | デモログイン表示制御      |

---

### 9.8 フロントエンド共通部品テスト

#### 9.8.1 CurrentUserBadgeテスト

`CurrentUserBadge` では、ログイン中ユーザーのロールと名前の表示を検証する。

| ケース        | 期待結果               |
| ---------- | ------------------ |
| userがnull  | 何も表示しない            |
| Adminユーザー  | `ロール: 管理者` と名前を表示  |
| Memberユーザー | `ロール: 一般会員` と名前を表示 |

`useCurrentUser` はモック化し、画面表示のみを検証する。

#### 9.8.2 useCurrentUserテスト

`useCurrentUser` では、localStorageからログインユーザー情報を取得できることを検証する。

| ケース              | 期待結果                      |
| ---------------- | ------------------------- |
| currentUserなし    | nullを返す                   |
| 正常なcurrentUserあり | ユーザー情報を返す                 |
| 壊れたJSON          | localStorageから削除し、nullのまま |
| Memberロール        | Memberユーザーを返す             |
| tokenなし          | tokenなしでもユーザー情報を返す        |
| shape不足          | JSONとして読める場合はそのまま返す       |

#### 9.8.3 LogoutButtonテスト

`LogoutButton` では、ログアウト時のクライアント側状態削除と画面遷移を検証する。

| ケース      | 期待結果                           |
| -------- | ------------------------------ |
| ログアウト押下  | localStorageのcurrentUserを削除    |
| ログアウト押下  | `/api/auth/logout` をPOST       |
| ログアウト押下  | `/auth/login` へ遷移              |
| Cookie削除 | `isLoggedIn` と `role` を期限切れにする |

---

### 9.9 認証画面テスト

#### 9.9.1 ログイン画面テスト

ログイン画面では、以下を検証する。

| ケース           | 期待結果                        |
| ------------- | --------------------------- |
| 初期表示          | 見出し、メール、パスワード、ログインボタンを表示    |
| パスワード表示切替     | password / text が切り替わる      |
| Adminログイン成功   | `/dashboards/admin` へ遷移     |
| Memberログイン成功  | `/dashboards/member` へ遷移    |
| 401           | 認証エラーメッセージを表示               |
| 403           | アカウント利用不可メッセージを表示           |
| 400           | 不正リクエストメッセージを表示             |
| API messageあり | APIのmessageを優先表示            |
| 500           | 既定の失敗メッセージを表示               |
| 通信エラー         | 通信エラーメッセージを表示               |
| 送信中           | ログインボタンを無効化                 |
| 管理者デモログイン     | 管理者デモ情報でログイン                |
| 一般会員デモログイン    | 一般会員デモ情報でログイン               |
| 新規会員登録ボタン     | `/auth/register` へ遷移        |
| パスワード再設定ボタン   | `/auth/forgot-password` へ遷移 |

ログイン成功時には、ロールに応じた遷移先を検証する。

#### 9.9.2 パスワード再設定依頼画面テスト

パスワード再設定依頼画面では、以下を検証する。

| ケース   | 期待結果                 |
| ----- | -------------------- |
| 初期表示  | 見出し、説明文、入力欄、送信ボタンを表示 |
| 送信成功  | 完了メッセージを表示           |
| API失敗 | エラーメッセージを表示          |
| 通信エラー | 通信エラーメッセージを表示        |
| 再送信   | 前回エラーをクリアする          |

---

### 9.10 管理者ダッシュボードテスト

管理者ダッシュボードでは、サマリー、未回収、操作ログ、年度切替を検証する。

| ケース         | 期待結果                    |
| ----------- | ----------------------- |
| 正常表示        | 売上、未入金、請求書数、入金件数、回収率を表示 |
| 未回収TOP5あり   | 請求番号、顧客名、未回収額を表示        |
| 回収率ワースト顧客あり | 顧客名と回収率を表示              |
| 操作ログあり      | 操作内容、対象、サマリーを表示         |
| 未回収なし       | 空状態メッセージを表示             |
| ワースト顧客なし    | 空状態メッセージを表示             |
| 操作ログなし      | 空状態メッセージを表示             |
| 年切替         | 前年・翌年リンクを表示             |
| year未指定     | 当年でAPIを呼ぶ               |
| year不正値     | 当年でAPIを呼ぶ               |
| DTO一部欠損     | フォールバックして表示する           |

操作ログでは、Actionコードを画面表示用ラベルへ変換する分岐も検証する。

| Action                       | 表示        |
| ---------------------------- | --------- |
| PAYMENT_ALLOCATION_ADDED     | 割当追加      |
| PAYMENT_ALLOCATION_DELETED   | 割当削除      |
| PAYMENT_ALLOCATIONS_REPLACED | 割当置換      |
| PAYMENT_ALLOCATIONS_CLEARED  | 割当クリア     |
| UNKNOWN_ACTION               | フォールバック表示 |

---

### 9.11 請求書画面テスト

#### 9.11.1 請求書一覧画面テスト

請求書一覧では、検索条件、一覧表示、ページング、リンクを検証する。

| ケース     | 期待結果                 |
| ------- | -------------------- |
| 一覧取得    | 請求番号、会員名、金額、ステータスを表示 |
| 検索条件あり  | APIへ検索条件を渡す          |
| 詳細リンク   | 請求書詳細へ遷移できる          |
| 督促リンク   | 督促画面へ遷移できる           |
| 新規作成リンク | `/invoices/new` へ遷移  |
| 0件      | 空状態と0件表示を出す          |
| 前へ・次へ   | 検索条件を引き継ぐ            |

#### 9.11.2 請求書詳細画面テスト

請求書詳細では、請求情報、入金履歴、督促履歴、リンクを検証する。

| ケース             | 期待結果                    |
| --------------- | ----------------------- |
| 正常表示            | 請求番号、ステータス、金額、日付、会員名を表示 |
| 備考あり            | 備考を表示                   |
| 督促履歴あり          | 督促日時、方法、メモを表示           |
| 入金履歴あり          | 入金日、方法、名義、取込ID、金額を表示    |
| 督促履歴なし          | 空状態メッセージを表示             |
| 入金履歴なし          | 空状態メッセージを表示             |
| fromあり          | 一覧、編集、入金登録リンクに検索条件を引き継ぐ |
| ステータス分岐         | 未入金、一部入金、入金済みを表示        |
| 入金方法分岐          | 銀行振込、現金、カード、その他を表示      |
| payerNameなし     | `名義：—` を表示              |
| importBatchIdなし | 表示が崩れない                 |

---

### 9.12 入金画面テスト

#### 9.12.1 入金一覧画面テスト

入金一覧では、入金データ、サマリー、ページング、認証トークンを検証する。

| ケース            | 期待結果                |
| -------------- | ------------------- |
| 正常表示           | 入金一覧、サマリー、請求書リンクを表示 |
| 入金ステータス        | 未割当、一部割当、割当済を表示     |
| 0件             | 空状態メッセージを表示         |
| ページ表示          | 現在ページ / 総ページを表示     |
| 前へ・次へ          | 検索条件を引き継ぐ           |
| month=all      | APIクエリにmonthを付けない   |
| month指定あり      | APIクエリにmonthを付ける    |
| payerName=null | `null` を表示しない       |
| tokenなし        | エラーになりAPIを呼ばない      |

#### 9.12.2 入金登録画面テスト

入金登録画面では、Pageコンポーネントとして以下を検証する。

| ケース      | 期待結果                             |
| -------- | -------------------------------- |
| 初期表示     | 見出し、説明文、パンくずを表示                  |
| 共通部品     | CurrentUserBadge、LogoutButtonを表示 |
| 入金登録フォーム | PaymentNewClientを表示              |

#### 9.12.3 入金詳細画面テスト

入金詳細画面では、Pageコンポーネントとして以下を検証する。

| ケース       | 期待結果                             |
| --------- | -------------------------------- |
| 初期表示      | 見出し、説明文、パンくずを表示                  |
| 共通部品      | CurrentUserBadge、LogoutButtonを表示 |
| 入金詳細フォーム  | PaymentDetailClientを表示           |
| params.id | デコードしてPaymentDetailClientへ渡す     |

---

### 9.13 会員画面テスト

会員一覧画面では、会員検索、ロール表示、有効状態、ページングを検証する。

| ケース        | 期待結果                 |
| ---------- | -------------------- |
| 正常表示       | 会員名、メール、ロール、有効状態を表示  |
| 管理者        | 退会ボタンを表示しない          |
| 一般会員       | 退会ボタンを表示する           |
| 退会済み       | 退会済みとして表示            |
| 0件         | 空状態と0件表示を出す          |
| 前へ・次へ      | 検索条件を引き継ぐ            |
| role分岐     | 管理者、一般会員、退会、不明ロールを表示 |
| isActive分岐 | 有効、無効を表示             |
| page未指定    | 1ページ目としてAPIを呼ぶ       |

---

### 9.14 督促画面テスト

#### 9.14.1 CollectionsPageテスト

`CollectionsPage` では、URLパラメータを `CollectionsClient` へ渡せることを検証する。

| ケース              | 期待結果                 |
| ---------------- | -------------------- |
| 初期表示             | CollectionsClientを表示 |
| params.invoiceId | CollectionsClientへ渡す |

#### 9.14.2 CollectionsClientテスト

督促画面では、スナップショット取得、履歴表示、文面生成、履歴登録を検証する。

| ケース                   | 期待結果                  |
| --------------------- | --------------------- |
| 初期ロード成功               | 見出し、スナップショット、履歴を表示    |
| fromあり                | 戻り先リンクに検索条件を反映        |
| 履歴0件                  | 空状態メッセージを表示           |
| 最新ログにnextActionDateあり | 次回アクション日に初期表示         |
| 初期ロード失敗               | エラー表示                 |
| clipboardなし           | 本文コピーボタンを表示しない        |
| 本文コピー                 | クリップボードへ本文をコピー        |
| トーン変更                 | 件名プレビューが切り替わる         |
| 未回収残額0円               | 記録せずalertを表示          |
| confirmキャンセル          | POSTしない               |
| 記録成功                  | POST後、履歴を再取得して成功メッセージ |
| チャネル変更                | PHONEなどに変更して登録        |
| トーン変更                 | STRONGなどに変更して登録       |
| 次回アクション日変更            | 変更値をPOSTする            |

督促文面は、トーンに応じて以下のように切り替わる。

| トーン    | 件名           |
| ------ | ------------ |
| SOFT   | ご確認系の件名      |
| NORMAL | 重要・支払い依頼系の件名 |
| STRONG | 至急・支払い依頼系の件名 |

---

### 9.15 テストで使用するモック

フロントエンドテストでは、外部依存を以下のようにモック化する。

| 対象                  | モック内容                  |
| ------------------- | ---------------------- |
| fetch               | APIレスポンス、通信エラー         |
| apiGetServer        | Server ComponentのAPI取得 |
| apiGetClient        | Client ComponentのAPI取得 |
| apiPostClient       | POST処理                 |
| next/navigation     | push、replaceなどのルーティング  |
| next/link           | aタグとして簡易表示             |
| next/headers        | cookies取得              |
| localStorage        | currentUser保存・削除       |
| document.cookie     | Cookie削除確認             |
| navigator.clipboard | 本文コピー                  |
| window.alert        | 完了・エラー通知               |
| window.confirm      | 確認ダイアログ                |

これにより、実APIやブラウザ環境に依存せず、画面ロジックを安定して検証する。

---

### 9.16 CI設計

本システムでは、GitHub ActionsによりフロントエンドとバックエンドのCIを実行する。

CIは以下のタイミングで実行する。

| トリガー              | 対象 |
| ----------------- | -- |
| mainへのpush        | 実行 |
| feature/** へのpush | 実行 |
| fix/** へのpush     | 実行 |
| developへのpush     | 実行 |
| pull_request      | 実行 |

---

### 9.17 フロントエンドCI

フロントエンドCIでは、Node.js 20を使用する。

実行内容は以下のとおりである。

| 順序 | コマンド          | 内容              |
| -- | ------------- | --------------- |
| 1  | npm ci        | 依存関係をクリーンインストール |
| 2  | npm run lint  | lint実行          |
| 3  | npm test      | Jestテスト実行       |
| 4  | npm run build | Next.jsビルド      |

`npm run lint` は `continue-on-error: true` としており、lintエラーがあっても後続のテスト・ビルドを継続する。

---

### 9.18 バックエンドCI

バックエンドCIでは、.NET 8 と PostgreSQL 16 を使用する。
`dotnet test` では、サービス単体テストとバックエンド統合テストの両方を実行する。
統合テストでは、Testing環境の設定、JWT設定、PostgreSQLテストDB、テストデータSeedを使用してAPIの疎通を確認する。

#### 9.18.1 PostgreSQLサービス

CI上では、PostgreSQL 16コンテナをサービスとして起動する。

| 項目                | 値             |
| ----------------- | ------------- |
| image             | postgres:16   |
| POSTGRES_USER     | postgres      |
| POSTGRES_PASSWORD | postgres      |
| POSTGRES_DB       | invoices_test |
| port              | 5432          |

ヘルスチェックでは、`pg_isready` を使用してPostgreSQLの起動完了を待機する。

#### 9.18.2 環境変数

バックエンドCIでは、以下の環境変数を設定する。

| 環境変数                                 | 値                    |
| ------------------------------------ | -------------------- |
| ASPNETCORE_ENVIRONMENT               | Testing              |
| SEED_DEMO_DATA                       | false                |
| ConnectionStrings__DefaultConnection | PostgreSQLテストDB接続文字列 |

#### 9.18.3 実行コマンド

バックエンドCIでは、以下を実行する。

| 順序 | コマンド           | 内容           |
| -- | -------------- | ------------ |
| 1  | dotnet restore | テストプロジェクトの復元 |
| 2  | dotnet build   | Releaseビルド   |
| 3  | dotnet test    | テスト実行        |

対象プロジェクトは以下である。

```text
backend/InvoiceSystem.Tests/InvoiceSystem.Tests.csproj
```

---

### 9.19 テスト対象の整理

本章で扱うテスト対象を整理すると以下のとおりである。

| 層                        | テスト対象                             | 主な確認内容                            |
| ------------------------ | --------------------------------- | --------------------------------- |
| Backend API | Auth、Admin、OperationLogs、Invoices、Members、MyAccount、Payments、Sales、Collections | 401、403、404、200、201、400、JWT認証、ロール認可、所有者判定、CSVレスポンス |
| Backend Service          | 各種Serviceクラス | 業務ロジック、DB更新、集計、検索、ステータス再計算、監査ログ、メール送信指示、非同期ジョブ処理 |
| Frontend Hook            | useCurrentUser                    | localStorage読込                    |
| Frontend Component       | CurrentUserBadge、LogoutButton     | 表示、ログアウト処理                        |
| Frontend Auth Page       | Login、ForgotPassword              | 認証操作、エラー表示                        |
| Frontend Admin Page      | AdminDashboard                    | サマリー、操作ログ、空状態                     |
| Frontend Invoice Page    | Invoices、InvoiceDetail            | 一覧、詳細、リンク、空状態                     |
| Frontend Payment Page    | Payments、PaymentNew、PaymentDetail | 入金一覧、ページング、認証トークン                 |
| Frontend Member Page     | Members                           | 会員一覧、ロール、有効状態                     |
| Frontend Collection Page | Collections                       | 督促文面、履歴、コピー、登録                    |
| CI                       | GitHub Actions                    | restore、build、test、frontend build |

---

### 9.20 今後のテスト拡張余地

現在のテストでは、主要な認証・認可、画面表示、業務ロジック、CI実行を確認している。

今後の拡張余地は以下のとおりである。

| 拡張案               | 内容                                          |
| ----------------- | ------------------------------------------- |
| API正常系テスト追加       | 請求書作成、入金登録、会員更新などの正常系APIを追加                 |
| API異常系テスト追加       | バリデーションエラー、409 Conflict、401 Unauthorizedを追加 |
| 認可テスト拡張           | Admin / Member / Disabled のアクセス境界を追加        |
| フロントフォームテスト追加     | 請求書作成、入金登録、入金割当フォームの詳細操作を追加                 |
| E2Eテスト導入          | Playwright等でログインから請求書確認までの一連操作を確認           |
| テストデータ初期化         | テストごとにDB状態を初期化する仕組みを強化                      |
| CIカバレッジ出力         | Jest / dotnet test のカバレッジレポートを追加            |
| Lintの厳格化          | `continue-on-error` を解除し、Lint失敗時にCIを失敗させる   |
| Docker Composeテスト | ローカルとCIのDB環境差異をさらに縮小                        |
| PDF出力テスト | Content-Type、ファイル名、PDF取得、権限制御を追加 |
| CSV出力テスト拡張 | BOM、ヘッダー、明細行、検索条件反映を追加 |
| サービス単体テスト拡張 | 請求書削除、会員無効化、入金登録正常系、監査ログ出力内容などのサービス単体テストを追加 |
| 境界値テスト追加 | 金額0円、負数、割当超過、期限当日、ページサイズ上限などの境界値を追加 |

## 10. 今後の拡張余地・改善方針

### 10.1 概要

本システムは、請求書管理、入金管理、入金割当、売上集計、督促管理、会員向け請求書確認を扱う Lite版の業務アプリケーションとして設計・実装している。

現時点では、ポートフォリオとして主要な業務フローを確認できることを優先しており、実運用で必要となる一部機能は将来的な拡張対象としている。

本章では、今後の拡張余地および改善方針を整理する。

---

### 10.2 機能面の拡張

#### 10.2.1 請求書機能の拡張

請求書機能については、現在のLite版では請求ヘッダ、明細、PDF出力を中心に実装している。

今後の拡張案は以下のとおりである。

| 拡張案         | 内容                         |
| ----------- | -------------------------- |
| 請求書番号の自動採番  | 年月や連番ルールに基づいて請求書番号を自動生成する  |
| 消費税計算       | 税率、税抜金額、消費税額、税込金額を分離して管理する |
| 複数税率対応      | 標準税率、軽減税率など、明細単位の税率を管理する   |
| 適格請求書対応     | 登録番号、税率別合計、消費税額の表示に対応する    |
| 請求書テンプレート管理 | 会社ロゴ、振込先、備考文言などをテンプレート化する  |
| 請求書再発行履歴    | PDF出力日時、出力者、再発行理由を記録する     |
| 請求書複製       | 既存請求書をコピーして新規請求書を作成する      |
| 下書きステータス    | 作成途中の請求書を下書きとして保存する        |

#### 10.2.2 入金管理機能の拡張

入金管理機能については、現在は手動登録と請求書への割当を中心に実装している。

今後の拡張案は以下のとおりである。

| 拡張案      | 内容                         |
| -------- | -------------------------- |
| 入金CSV取込  | 銀行明細や会計ソフトから出力したCSVを取り込む   |
| 銀行明細連携   | 銀行APIや明細ファイルから入金データを取り込む   |
| 入金自動照合   | 入金名義、金額、請求番号から候補請求書を自動推定する |
| 過入金管理    | 請求額を超える入金を預り金や返金対象として管理する  |
| 不足入金管理   | 一部入金状態を明確にし、残額の追跡を強化する     |
| 入金取消     | 誤登録した入金を取消・訂正できるようにする      |
| 入金方法マスタ化 | 振込、現金、カードなどをマスタ管理する        |

#### 10.2.3 売上集計機能の拡張

売上集計機能については、現在は請求書ベースの売上一覧と会員別集計を提供している。

今後の拡張案は以下のとおりである。

| 拡張案       | 内容                          |
| --------- | --------------------------- |
| 月次推移グラフ強化 | 売上、入金、未回収を月次で比較表示する         |
| 顧客別ランキング  | 売上上位、未回収上位、回収率ワーストをランキング化する |
| 期間指定集計    | 年月だけでなく任意の日付範囲で集計する         |
| ステータス別集計  | 未入金、一部入金、入金済み、期限超過ごとに集計する   |
| CSV項目追加   | 会員ID、税額、入金方法、最終督促日などを出力する   |
| Excel出力   | CSVに加え、Excel形式での出力に対応する     |

---

### 10.3 督促・回収業務の拡張

現在の督促機能では、請求書スナップショット、督促テンプレート、督促履歴、次回アクション日を管理している。

今後の拡張案は以下のとおりである。

| 拡張案        | 内容                         |
| ---------- | -------------------------- |
| メール送信連携    | 督促文面を実際にメール送信する            |
| 自動リマインド    | 支払期限超過後、一定日数で自動的に督促候補を表示する |
| 次回対応日通知    | 次回アクション日が近い督促対象を通知する       |
| 督促テンプレート管理 | 文面テンプレートを管理画面から編集できるようにする  |
| 督促段階管理     | 初回、2回目、最終通知など段階別に管理する      |
| 督促除外設定     | 特定顧客や特定請求書を督促対象外にする        |
| 対応履歴の詳細化   | 電話内容、担当者、対応結果などを記録する       |

---

### 10.4 会員向け機能の拡張

現在の会員向け機能では、会員本人が自分の請求書、未払い状況、プロフィールを確認できる。

今後の拡張案は以下のとおりである。

| 拡張案         | 内容                      |
| ----------- | ----------------------- |
| メール変更時の再認証  | メールアドレス変更後に確認メールを送信する   |
| プロフィール変更履歴  | 会員情報の変更履歴を記録する          |
| 支払予定日登録     | 会員が支払予定日を入力できるようにする     |
| 問い合わせ機能     | 請求書に対する問い合わせを送信できるようにする |
| PDF一括ダウンロード | 複数請求書PDFをまとめて取得する       |
| 会員通知        | 請求書発行時や督促時に通知する         |
| 支払方法案内      | 振込先、支払方法、注意事項を会員画面に表示する |

---

### 10.5 セキュリティ面の改善

現在のシステムでは、JWT認証、ロール認可、所有者チェック、メール確認、パスワードハッシュ化などを実装している。

今後の改善案は以下のとおりである。

| 改善案             | 内容                                  |
| --------------- | ----------------------------------- |
| Refresh Token対応 | アクセストークンの短寿命化と更新トークン導入              |
| Cookie属性強化      | HttpOnly、Secure、SameSite属性の整理       |
| CSRF対策          | Cookie認証を強化する場合にCSRF対策を追加           |
| パスワードポリシー強化     | 文字数、複雑性、使い回し制限を追加                   |
| ログイン試行制限        | 連続失敗時のロックやレート制限を追加                  |
| 監査ログ強化          | ログイン、PDF出力、CSV出力、会員情報変更も記録          |
| 個人情報マスキング       | メールアドレスや電話番号の表示制御を追加                |
| 権限粒度の細分化        | Admin / Member だけでなく、経理担当、閲覧専用などを追加 |

---

### 10.6 テスト・品質面の改善

現在は、バックエンドの認可・異常系・業務ロジックテスト、フロントエンドの主要画面テスト、GitHub ActionsによるCIを実装している。

今後の改善案は以下のとおりである。

| 改善案         | 内容                                        |
| ----------- | ----------------------------------------- |
| API正常系テスト追加 | 請求書作成、入金登録、会員更新などの正常系を追加                  |
| API異常系テスト追加 | 400、401、403、404、409のパターンを追加               |
| フォーム詳細テスト追加 | 請求書フォーム、入金割当フォーム、プロフィールフォームを強化            |
| E2Eテスト導入    | Playwright等でログインから請求書確認までの一連操作を検証         |
| PDF出力テスト    | Content-Type、ファイル名、PDF生成結果を検証             |
| CSV出力テスト    | BOM、ヘッダ、ファイル名、エスケープを検証                    |
| カバレッジ出力     | Jest、dotnet testのカバレッジをCIで出力              |
| テストデータ初期化   | テストごとにDB状態を安定化する仕組みを強化                    |
| Lintの厳格化    | `continue-on-error` を解除し、Lint失敗時にCIを失敗させる |

---

### 10.7 フロントエンド面の改善

現在のフロントエンドでは、Next.js App Router、Server Component、Client Componentを組み合わせ、管理者画面・会員画面を構成している。

今後の改善案は以下のとおりである。

| 改善案            | 内容                             |
| -------------- | ------------------------------ |
| API通信処理の統一     | `fetch` 直接呼び出し箇所を共通API関数へ統一する  |
| 型定義整理          | 重複しているDTO型、旧型を整理する             |
| フォームバリデーション共通化 | Zodなどを利用して入力検証を共通化する           |
| エラー表示共通化       | Alert、Toast、ErrorBoundaryを整備する |
| ローディングUI共通化    | Skeletonや共通Loading部品を導入する      |
| ページング部品共通化     | 一覧画面で共通ページャを利用する               |
| 検索フォーム共通化      | 年月、ステータス、キーワード検索を共通化する         |
| UIアクセシビリティ向上   | aria属性、キーボード操作、フォーカス制御を改善する    |

---

### 10.8 インフラ・運用面の改善

現在は、開発・CI環境でPostgreSQLやテストDBを利用し、フロントエンドとバックエンドを分離した構成としている。

今後の改善案は以下のとおりである。

| 改善案              | 内容                                                   |
| ---------------- | ---------------------------------------------------- |
| Docker Compose整備 | フロント、バックエンド、DBを一括起動できるようにする                          |
| 本番環境構築           | Azure App Service、Azure Static Web Appsなどへの本番配置を整備する |
| DBバックアップ         | PostgreSQLのバックアップ・リストア手順を用意する                        |
| マイグレーション運用       | EF Core Migrationの適用手順を明確化する                         |
| ログ監視             | アプリケーションログ、エラーログを監視できるようにする                          |
| ヘルスチェック強化        | DB接続、外部メール送信、PDF生成などを確認する                            |
| 環境変数管理           | JWTキー、DB接続文字列、SMTP情報などを安全に管理する                       |
| リリース手順整備         | CI通過後のデプロイ、動作確認、ロールバック手順を定義する                        |

---

### 10.9 パフォーマンス面の改善

現在のLite版では、扱うデータ量を比較的小規模に想定している。

今後、データ量が増加した場合に備え、以下の改善を検討する。

| 改善案        | 内容                                 |
| ---------- | ---------------------------------- |
| DBインデックス追加 | 請求番号、会員ID、請求日、支払期限、ステータスにインデックスを追加 |
| ページング最適化   | 大量データでも安定して一覧表示できるようにする            |
| 集計クエリ最適化   | 売上集計、未回収集計、回収率計算のSQLを最適化する         |
| CSV非同期出力   | 大量データCSVをバックグラウンド生成にする             |
| PDF生成負荷対策  | 一括PDF生成や再発行時の負荷を抑制する               |
| キャッシュ検討    | ダッシュボード集計などの一部結果をキャッシュする           |

---

### 10.10 データ設計面の改善

現在のデータ設計では、Lite版として必要な主要エンティティを定義している。

今後の改善案は以下のとおりである。

| 改善案        | 内容                         |
| ---------- | -------------------------- |
| マスタデータ拡張   | 入金方法、督促チャネル、税率などをマスタ化する    |
| 論理削除の整理    | 請求書、入金、会員の削除・無効化方針を統一する    |
| 変更履歴テーブル追加 | 請求書、会員、入金割当の変更履歴を保存する      |
| 楽観ロック導入    | 同時編集時の競合を検出する              |
| 金額精度の再確認   | decimal精度、丸め処理、税計算ルールを整理する |
| ステータス遷移管理  | 請求書ステータスの変更ルールを明確化する       |
| 通知履歴テーブル追加 | メール送信、督促通知、会員通知を履歴化する      |

---

### 10.11 ポートフォリオとしての改善方針

本システムは、ASP.NET Core、EF Core、PostgreSQL、JWT認証、Next.js、TypeScript、CI、テストを組み合わせた業務アプリケーションとして構成している。

ポートフォリオとしてさらに見せやすくするため、以下の改善を行う余地がある。

| 改善案       | 内容                                          |
| --------- | ------------------------------------------- |
| README強化  | 画面キャプチャ、機能一覧、技術構成、設計意図を整理する                 |
| デモ導線整理    | 管理者・会員それぞれの確認手順を明確化する                       |
| 設計資料整理    | 基本設計、詳細設計、ER図、画面遷移図をREADMEから参照できるようにする      |
| テスト結果の可視化 | CIバッジ、テスト件数、カバレッジをREADMEに掲載する               |
| デプロイ構成図   | フロント、バックエンド、DB、CIの関係を図示する                   |
| 業務シナリオ追加  | 請求発行から入金割当、督促、会員確認までの流れを説明する                |
| 技術選定理由追加  | ASP.NET Core、Next.js、PostgreSQLを採用した理由を記載する |
| 制限事項の明記   | Lite版で未対応の範囲を明確化する                          |

---

### 10.12 まとめ

本システムは、Lite版として請求・入金・売上・督促・会員向け確認の主要業務フローを実装している。

今後は、実運用を想定した機能拡張、セキュリティ強化、テスト強化、運用設計の整備を行うことで、より実務に近い業務システムへ発展させることができる。

特に、以下の改善は優先度が高い。

| 優先度 | 改善内容             | 理由                    |
| --- | ---------------- | --------------------- |
| 高   | 入金CSV取込・自動照合     | 入金管理業務の効率化に直結するため     |
| 高   | PDF/CSV出力テスト     | 帳票・外部出力は業務上重要なため      |
| 高   | API通信・型定義の整理     | 保守性向上に直結するため          |
| 中   | 督促メール送信連携        | 回収業務の実運用に近づくため        |
| 中   | E2Eテスト導入         | 主要業務フローの品質を担保できるため    |
| 中   | Docker Compose整備 | ローカル再現性と引き継ぎ性を高めるため   |
| 低   | 高度なダッシュボード分析     | 基本業務フロー安定後の拡張として有効なため |

以上の拡張により、現在のLite版から、より実務的な請求・入金管理システムへ発展させることが可能である。
