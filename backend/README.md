# Invoice Management System (Lite) – Backend API / Batch

本ディレクトリは **Invoice Management System (Lite)** のバックエンド実装です。  
ASP.NET Core Web APIに加えて、外部ジョブ管理から起動できるReminder CLI Batchを含みます。

請求・入金管理業務で発生しやすい「一部入金」「複数請求への割当」「状態管理」に加え、
督促処理では **非同期処理・再実行・排他・外部スケジューラ連携** を意識した構成としています。

---

## 技術スタック

- ASP.NET Core / .NET 10
- .NET 10 Console Batch
- Entity Framework Core 10
- PostgreSQL 16 / Npgsql
- JWT認証 / Admin・Memberロール認可
- QuestPDF（請求書PDF生成）
- Serilog
- xUnit
- Docker
- ConoHa VPS / Ubuntu 24.04 LTS
- systemd service / timer
- nginx / Let's Encrypt

---

## プロジェクト構成

```text
backend/
├─ InvoiceSystem.Api/                # ASP.NET Core Web API
├─ InvoiceSystem.Application/        # DTO / Interface / Application Service
├─ InvoiceSystem.Batch/              # Reminder CLI Batch
├─ InvoiceSystem.Domain/             # Entity / Domain Model
├─ InvoiceSystem.Infrastructure/     # EF Core / Service / Email / Worker
├─ InvoiceSystem.Tests/              # Unit / Integration Test
├─ InvoiceSystem.Api.slnx
└─ README.md
```

主なReminder関連ファイル：

```text
InvoiceSystem.Application/
├─ Services/IReminderJobProcessor.cs
└─ Services/ReminderJobProcessResult.cs

InvoiceSystem.Domain/
└─ Entities/ReminderJob.cs

InvoiceSystem.Infrastructure/
├─ Email/MailtrapEmailSender.cs
└─ Services/
   ├─ ReminderJobProcessor.cs
   └─ ReminderJobWorker.cs

InvoiceSystem.Batch/
├─ Program.cs
├─ Dockerfile
└─ InvoiceSystem.Batch.csproj
```

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
- 認証・認可（JWT / Admin / Member）
- メール確認・パスワード再設定
- 管理者操作ログ（AuditLogs）
- ReminderJobによる督促メール非同期処理
- 外部起動可能なReminder CLI Batch
- ヘルスチェック

---

## API 設計方針

- REST APIベース
- フロントエンドと分離
- 業務ロジックはApplication / Domain / Infrastructureへ責務分離
- Endpointは薄く保つ
- 認証・認可をAPIレベルで完結
- DBアクセスはEF Core / PostgreSQL
- 定期処理はAPI内Workerと外部Batchを切り替え可能

---

## ドメイン設計のポイント

- 入金割当の追加・削除・置換時に、割当済み金額から請求ステータスを再計算
- 一部入金・複数請求への割当を考慮し `PaymentAllocations` を中間テーブルとして設計
- 催促履歴を独立テーブルとして保持
- `ReminderJobs` では処理状態・RetryCount・StartedAt・CompletedAtを保持し、非同期処理の再実行性を管理

※ ER図・状態遷移図は `../docs/` 配下を参照してください。

---

## Reminder CLI Batch

### 目的

督促メール処理をAPIプロセス内のBackgroundServiceだけに依存させず、
外部ジョブスケジューラから明示的に起動・監視できるCLI Batchとして分離しています。

実行例：

```bash
dotnet InvoiceSystem.Batch.dll reminder --job-id REMINDER-20260923-001
```

`--job-id` は外部ジョブ実行を識別するための `ExternalJobId` としてログへ出力します。
DB上の `ReminderJob.Id` とは別の識別子です。

### 終了コード

| ExitCode | 意味 | systemd上の扱い |
|---:|---|---|
| 0 | 正常終了 | Success |
| 10 | 処理対象なし | Success |
| 20 | コマンド / 必須引数不正 | Failure |
| 50 | DB / SMTP等の処理失敗 | Failure |
| 99 | 想定外エラー | Failure |

systemd serviceでは `SuccessExitStatus=10` を設定し、
「処理対象なし」をジョブ異常として扱わない構成にしています。

### ReminderJob処理

処理の概要：

```text
Pending
  ↓ claim
Processing
  ├─ 正常 → Completed
  └─ 失敗 → Pending（Retry可能）
                ↓ Retry上限
              Failed
```

PostgreSQL環境では、対象取得時に以下の排他方式を使用します。

```sql
SELECT *
FROM "ReminderJobs"
WHERE "Status" = 'Pending'
  AND "RetryCount" < 3
ORDER BY "CreatedAt"
LIMIT 10
FOR UPDATE SKIP LOCKED
```

これによりBatchを同時起動した場合でも、同一Pendingジョブの二重claimを抑止します。

また、一定時間 `Processing` のまま残ったジョブはstaleと判断し、
再実行可能な状態へ復旧します。

> SMTP送信成功後、DBのCompleted更新前にプロセスが停止した場合は再送される可能性があります。
> このため配信保証は Exactly Once ではなく **At-Least-Once** を前提としています。

---

## API内Reminder Workerとの切替

API側のBackgroundServiceは設定で有効 / 無効を切り替えられます。

```json
{
  "ReminderWorker": {
    "Enabled": true
  }
}
```

外部ジョブ管理方式を利用するVPS環境では、例えば以下でAPI内Workerを停止できます。

```bash
ReminderWorker__Enabled=false
```

これにより、API Workerと外部Batchが同じPendingジョブを競合して処理する構成を避けられます。

---

## Docker Batch

Batch専用Dockerfile：

```text
backend/InvoiceSystem.Batch/Dockerfile
```

ビルド例：

```bash
docker build   -f backend/InvoiceSystem.Batch/Dockerfile   -t invoice-system-batch:prod   .
```

VPS上では既存のPostgreSQLと同じDocker Networkへ参加させます。

```bash
docker run --rm   --network invoice-management-system-lite_invoice-network   --env-file .env.batch.prod   invoice-system-batch:prod   reminder   --job-id REMINDER-VPS-001
```

Batch用 `.env.batch.prod` にはDB接続情報とSMTP設定を保持し、
Gitにはコミットしません。

---

## systemdによる定刻実行

VPSでは以下の構成でDocker Batchを外部起動できます。

```text
systemd timer
    ↓
invoice-reminder-batch.service
    ↓
deploy/batch/run-reminder-batch.sh
    ↓
docker run
    ↓
InvoiceSystem.Batch
    ↓
PostgreSQL / SMTP
```

管理ファイル：

```text
../deploy/batch/run-reminder-batch.sh
../deploy/systemd/invoice-reminder-batch.service
../deploy/systemd/invoice-reminder-batch.timer
```

serviceは `Type=oneshot` とし、Batch終了後は常駐しません。

検証では08:35 JSTにtimerからserviceが自動起動し、
Batchの `ExitCode=10`（対象なし）がsystemd上では正常終了となることを確認しています。

```ini
SuccessExitStatus=10
```

実行ログはjournalから確認できます。

```bash
journalctl   -u invoice-reminder-batch.service   --no-pager
```

検証終了後はtimerを停止・無効化しています。

```bash
sudo systemctl disable --now invoice-reminder-batch.timer
```

08:35は検証用の時刻であり、運用時刻を固定する仕様ではありません。

---

## ログ

BatchではUTCタイムスタンプ付きの1行Console Logを使用し、主に以下を記録します。

- ExternalJobId
- claim件数
- ReminderJobId / InvoiceId
- TargetCount
- CompletedCount
- RetryPendingCount
- FailedCount
- ExitCode

systemd経由の実行では標準出力 / 標準エラーをjournalへ記録します。

---

## データベース管理

- EF Core Migrationsによる差分管理
- 完成SQLではなくMigrationを正として管理
- PostgreSQL 16をDockerで稼働
- VPS環境ではDocker Volumeへデータを永続化
- `ReminderJobs` を含むスキーマもEF Core Migrationで管理

---

## 起動方法（ローカル）

### API

```bash
cd backend/InvoiceSystem.Api
dotnet restore
dotnet ef database update
dotnet run
```

### Batch

リポジトリルートから：

```bash
dotnet run   --project backend/InvoiceSystem.Batch   -- reminder   --job-id REMINDER-LOCAL-001
```

処理対象がない場合はExitCode=10になります。

---

## 環境変数例

API：

```bash
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=invoicesystem;Username=postgres;Password=postgres
Jwt__Key=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
Jwt__Issuer=InvoiceSystem
Jwt__Audience=InvoiceSystemFrontend
Jwt__ExpiresMinutes=60
ReminderWorker__Enabled=true
```

Batch：

```bash
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=invoicesystem;Username=postgres;Password=postgres
Mailtrap__Host=sandbox.smtp.mailtrap.io
Mailtrap__Port=2525
Mailtrap__UserName=<sandbox username>
Mailtrap__Password=<sandbox password>
Mailtrap__From=no-reply@example.com
```

実際の認証情報は `.env` / `.env.batch.prod` 等で管理し、リポジトリには含めません。

---

## VPS / 公開環境

バックエンドAPI / PostgreSQLは2026年8月にHerokuからConoHa VPSへ移行しています。

- OS：Ubuntu 24.04 LTS
- API：ASP.NET Core .NET 10 / Docker
- DB：PostgreSQL 16 / Docker Volume
- Reverse Proxy：nginx
- HTTPS：Let's Encrypt
- Batch：.NET 10 / Docker
- Batch Scheduler：systemd service / timer
- Batch Log：journalctl

フロントエンドとの結合確認はVercel / Azure Static Web AppsからVPS APIに対して実施しています。

---

## テスト

バックエンドではxUnitを使用しています。

- サービス層Unit Test
  - 請求書
  - 入金 / 入金割当
  - 売上集計
  - 会員
  - 認証
  - 督促
  - 監査ログ
- ReminderJobProcessor Test
  - Pending正常処理
  - メール送信失敗時のRetry
  - Retry上限到達時のFailed
  - Completed / Retry上限データの除外
  - 最大10件処理
  - stale Processingの復旧
  - staleでないProcessingの除外
- API Integration Test
  - 認証 / 認可
  - Admin / Memberの権限分離
  - 請求書、会員、入金、売上、督促、操作ログAPI

CIではGitHub Actionsから `dotnet restore`、`dotnet build`、`dotnet test` を実行します。

---

## Batch設計・証跡

設計資料：

- [Batch 要件定義](../docs/batch/01_requirements.md)
- [Batch 基本設計](../docs/batch/02_basic-design.md)
- [Batch 詳細設計](../docs/batch/03_detail-design.md)

代表的な実行証跡：

- [VPS 対象なし ExitCode=10](../docs/evidence/batch/25-vps-no-target-exit10.png)
- [VPS SMTP失敗 ExitCode=50](../docs/evidence/batch/32-vps-smtp-failure-exit50.png)
- [systemd timer 08:35 自動実行](../docs/evidence/batch/33-systemd-timer-auto-run-0835.png)

その他のRetry、stale復旧、同時実行、Docker実行の証跡も
`../docs/evidence/batch/` に整理しています。

---

## 補足

- Lite版のため、締め処理や高度な権限制御などは対象外です。
- Reminder Batchは特定製品（JP1/AJS等）への依存実装ではなく、外部ジョブ管理製品から起動・終了判定しやすいCLI設計を目的としています。
- systemd timerはVPS上での外部スケジューラ相当の実証として使用しています。
