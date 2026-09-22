# Reminder Batch 詳細設計

## 1. 目的

本書は、`docs/batch/01_requirements.md` および `docs/batch/02_basic-design.md` を前提として、
Invoice Management System のリマインダーメール外部Batchについて、実装単位の詳細設計を定義する。

対象は以下とする。

- `InvoiceSystem.Batch`
- `IReminderJobProcessor`
- `ReminderJobProcessor`
- `ReminderJobProcessResult`
- `ReminderJobWorker`
- `ReminderJobs` テーブル
- Mailtrapを利用した `IEmailSender`
- PostgreSQL上の排他制御
- stale Processing復旧
- ExitCode
- Console Logging

本書では、JP1/AJS本体の設定値やジョブネット定義は扱わない。
外部ジョブ管理製品から起動されるアプリケーション側Batchの詳細設計を対象とする。

---

## 2. 対象コンポーネント

| レイヤ | コンポーネント | 役割 |
|---|---|---|
| Batch | `InvoiceSystem.Batch/Program.cs` | CLI起動、DI構築、ログ設定、ExitCode決定 |
| Application | `IReminderJobProcessor` | Reminder処理の抽象I/F |
| Application | `ReminderJobProcessResult` | 処理件数・成否の返却 |
| Infrastructure | `ReminderJobProcessor` | stale復旧、Claim、メール送信、状態更新 |
| Infrastructure | `ReminderJobWorker` | API内定期実行。外部Batch運用時は無効化可能 |
| Infrastructure | `MailtrapEmailSender` | SMTPメール送信 |
| DB | `ReminderJobs` | Reminder処理キュー・状態管理 |

---

## 3. 起動インターフェース

### 3.1 コマンド形式

```bash
dotnet InvoiceSystem.Batch.dll reminder --job-id <EXTERNAL_JOB_ID>
```

開発時：

```powershell
dotnet run --project backend/InvoiceSystem.Batch -- `
  reminder `
  --job-id REMINDER-LOCAL-001
```

### 3.2 引数

| 項目 | 型 | 必須 | 内容 |
|---|---|---:|---|
| `command` | string | 必須 | `reminder` 固定 |
| `--job-id` | string | 必須 | 外部実行単位を識別するID |

内部では `--job-id` の値を `externalJobId` として扱う。

例：

```text
--job-id REMINDER-LOG-001
↓
externalJobId = "REMINDER-LOG-001"
↓
ExternalJobId=REMINDER-LOG-001
```

### 3.3 引数エラー

`command != reminder` の場合：

```text
ExitCode = 20
LogLevel = Error
```

`--job-id` が不足する場合：

```text
ExitCode = 20
LogLevel = Error
```

---

## 4. Batch起動処理

対象：

```text
backend/InvoiceSystem.Batch/Program.cs
```

処理順序：

```text
引数取得
  ↓
Bootstrap Logger生成
  ↓
command検証
  ↓
--job-id検証
  ↓
Host builder生成
  ↓
Console Logging設定
  ↓
DB接続設定取得
  ↓
DbContext登録
  ↓
Infrastructure登録
  ↓
IEmailSender登録
  ↓
Host Build
  ↓
IReminderJobProcessor取得
  ↓
ProcessPendingAsync実行
  ↓
ReminderJobProcessResult取得
  ↓
ExitCode決定
  ↓
終了
```

---

## 5. Logging詳細

### 5.1 Console Logger設定

`InvoiceSystem.Batch` では `SimpleConsole` を使用する。

```text
TimestampFormat = yyyy-MM-dd'T'HH:mm:ss.fff'Z'
UseUtcTimestamp = true
SingleLine = true
```

出力例：

```text
2026-09-22T21:25:41.021Z info: InvoiceSystem.Batch[0] Reminder batch started. ExternalJobId=REMINDER-LOG-001, BatchName=Reminder
```

### 5.2 Bootstrap Logger

Host構築前のエラーも同一形式で出力するため、
`LoggerFactory.Create(...)` によりBootstrap Loggerを生成する。

対象：

- command不正
- `--job-id` 不足
- DB接続文字列未設定
- Host構築前に発生する例外

### 5.3 ログ識別子

外部実行単位：

```text
ExternalJobId
```

DB上のReminderJob：

```text
ReminderJobId
```

Invoice識別子：

```text
InvoiceId
```

例：

```text
Reminder batch started. ExternalJobId=REMINDER-LOG-001, BatchName=Reminder
Reminder job completed. ReminderJobId=7, InvoiceId=56
```

### 5.4 Batch処理結果ログ

```text
ExternalJobId
TargetCount
CompletedCount
RetryPendingCount
FailedCount
ExitCode
```

を出力する。

---

## 6. DB接続設定

接続文字列は以下の優先順で取得する。

```text
1. DATABASE_URL
2. ConnectionStrings:DefaultConnection
```

`DATABASE_URL` が設定されている場合：

```text
PostgresConnectionStringFactory.Create(databaseUrl)
```

を使用してNpgsql用接続文字列へ変換する。

どちらも未設定の場合：

```text
ExitCode = 50
```

とする。

---

## 7. DI登録

Batchでは以下を登録する。

```text
AppDbContext
AddInfrastructureServices()
IEmailSender → MailtrapEmailSender
```

Batchでは `ReminderJobWorker` を登録しない。

`ReminderJobWorker` はAPI側のHostedServiceであり、
外部Batchとはスケジューリング責務を分離する。

---

## 8. `IReminderJobProcessor`

シグネチャ：

```csharp
Task<ReminderJobProcessResult> ProcessPendingAsync(
    CancellationToken cancellationToken);
```

責務：

```text
stale Processing復旧
↓
Pending Claim
↓
メール送信
↓
状態更新
↓
処理結果返却
```

ExitCode決定は担当しない。

---

## 9. `ReminderJobProcessResult`

構造：

```csharp
ReminderJobProcessResult(
    int TargetCount,
    int CompletedCount,
    int RetryPendingCount,
    int FailedCount)
```

派生値：

```text
ProcessedCount
HasTargets
HasFailures
```

定義：

```text
ProcessedCount =
    CompletedCount
  + RetryPendingCount
  + FailedCount
```

```text
HasTargets = TargetCount > 0
```

```text
HasFailures =
    RetryPendingCount > 0
    OR
    FailedCount > 0
```

---

## 10. `ReminderJobProcessor.ProcessPendingAsync`

処理フロー：

```text
RecoverStaleProcessingJobsAsync
        ↓
ClaimPendingJobsAsync
        ↓
foreach claimed job
        ↓
ProcessOneAsync
        ↓
Status別に件数集計
        ↓
ReminderJobProcessResult返却
```

集計対象：

```text
Completed → CompletedCount
Pending   → RetryPendingCount
Failed    → FailedCount
```

---

## 11. stale Processing復旧

対象メソッド：

```text
RecoverStaleProcessingJobsAsync
```

### 11.1 タイムアウト

```text
ProcessingTimeout = 10分
```

### 11.2 対象条件

```text
Status = Processing
AND
StartedAt IS NOT NULL
AND
StartedAt <= UtcNow - 10分
AND
RetryCount < 3
```

### 11.3 更新内容

`ExecuteUpdateAsync()` によりDB側で条件付きUPDATEする。

```text
Status = Pending
ErrorMessage = "Recovered from stale Processing state."
```

### 11.4 競合対策

複数Batchが同じstale行を同時に復旧しようとしても、
条件付きUPDATEにより先に更新したプロセスのみが対象行を更新する。

```text
Batch A → UPDATE Count=1
Batch B → 条件再評価 → UPDATE Count=0
```

### 11.5 ChangeTracker

`ExecuteUpdateAsync()` はChangeTrackerを経由しない。

復旧件数が1件以上の場合：

```csharp
_db.ChangeTracker.Clear();
```

を実行し、後続処理で古い追跡状態を使用しない。

---

## 12. Pending Claim

対象メソッド：

```text
ClaimPendingJobsAsync
```

### 12.1 PostgreSQL

PostgreSQLでは以下を使用する。

```sql
SELECT *
FROM "ReminderJobs"
WHERE "Status" = 'Pending'
  AND "RetryCount" < 3
ORDER BY "CreatedAt"
LIMIT 10
FOR UPDATE SKIP LOCKED;
```

取得後、同一トランザクション内で以下を設定する。

```text
Status = Processing
StartedAt = UtcNow
ErrorMessage = NULL
```

その後：

```text
SaveChanges
↓
COMMIT
```

### 12.2 SQLite

単体テストではSQLite In-Memoryを使用するため、
`FOR UPDATE SKIP LOCKED` は使用しない。

SQLite時は通常のLINQで最大10件を取得し、

```text
Status = Processing
StartedAt = UtcNow
ErrorMessage = NULL
```

へ更新する。

### 12.3 ロック範囲

PostgreSQLのトランザクションは以下までとする。

```text
Pending取得
↓
Processing更新
↓
COMMIT
```

SMTP送信中はDBロックを保持しない。

---

## 13. メール送信処理

対象メソッド：

```text
ProcessOneAsync
```

対象I/F：

```text
IEmailSender.SendAsync(
    ToEmail,
    Subject,
    Body)
```

正常時：

```text
Status = Completed
CompletedAt = UtcNow
```

ログ：

```text
ReminderJobId
InvoiceId
```

---

## 14. メール送信失敗

`IEmailSender.SendAsync()` で例外が発生した場合：

```text
RetryCount++
```

その後：

```text
RetryCount < 3
→ Status = Pending
```

```text
RetryCount >= 3
→ Status = Failed
```

さらに：

```text
ErrorMessage = exception.Message
```

を保存する。

### 14.1 再実行

`Pending` へ戻ったジョブは、
次回Batch実行時に再度Claim対象となる。

### 14.2 RetryCount

RetryCountはSMTP送信失敗回数として扱う。

stale Processing復旧では増加させない。

---

## 15. 状態遷移

```text
          ┌───────────────────────┐
          │                       │
          │  stale timeout        │
          │                       ↓
Pending → Processing ─────────→ Pending
  │          │
  │          ├─ SMTP成功 ─────→ Completed
  │          │
  │          └─ SMTP失敗
  │                 ↓
  │            RetryCount++
  │                 ↓
  │          ┌──────┴──────┐
  │          │             │
  └──────────┘          Failed
 RetryCount < 3      RetryCount >= 3
```

---

## 16. ExitCode決定

ExitCodeは `InvoiceSystem.Batch` が決定する。

| ExitCode | 条件 |
|---:|---|
| `0` | 対象あり、全件正常完了 |
| `10` | `TargetCount == 0` |
| `20` | command不正、必須パラメータ不足 |
| `50` | DBエラー、SMTP等の処理失敗 |
| `99` | 想定外例外 |

判定順：

```text
!HasTargets
→ 10

HasFailures
→ 50

上記以外
→ 0
```

---

## 17. 例外処理

Batch側で以下を捕捉する。

### 17.1 `DbUpdateException`

```text
LogLevel = Error
ExitCode = 50
```

### 17.2 `DbException`

```text
LogLevel = Error
ExitCode = 50
```

### 17.3 その他 `Exception`

```text
LogLevel = Error
ExitCode = 99
```

例外ログには可能な範囲で以下を含める。

```text
Timestamp
ExternalJobId
ExitCode
Exception
```

---

## 18. API Workerとの責務分離

API側では設定によりWorkerを有効・無効化する。

```text
ReminderWorker:Enabled
```

環境変数：

```text
ReminderWorker__Enabled=false
```

外部Batch運用時は原則として `false` とする。

Batch側では `AddReminderJobWorker()` を呼び出さない。

---

## 19. SMTP設定

`MailtrapEmailSender` は以下の設定を使用する。

```text
Mailtrap:Host
Mailtrap:Port
Mailtrap:UserName
Mailtrap:Password
Mailtrap:From
```

PowerShell環境変数例：

```text
Mailtrap__Host
Mailtrap__Port
Mailtrap__UserName
Mailtrap__Password
Mailtrap__From
```

認証情報はリポジトリへ保存しない。

---

## 20. テスト設計

### 20.1 単体テスト

対象：

```text
ReminderJobProcessorTests
```

確認項目：

```text
Pending → Completed
SMTP失敗1回目 → Pending / RetryCount=1
SMTP失敗3回目 → Failed / RetryCount=3
Completedは対象外
RetryCount>=3は対象外
最大10件処理
stale Processingは復旧してCompleted
最近のProcessingは復旧しない
```

現時点：

```text
114 tests
114 passed
0 failed
```

### 20.2 PostgreSQL実機確認

確認済み：

```text
正常処理
対象なし
SMTP失敗
障害復旧後再実行
Completed再処理防止
stale Processing復旧
Pending同時Claim
stale Processing同時復旧
UTC Timestampログ
引数エラー時ILogger出力
```

---

## 21. 実機証跡

証跡格納先：

```text
docs/evidence/batch/
```

主な対応：

```text
01～02 : 正常処理
03～07 : SMTP失敗・再実行
08～10 : stale Processing復旧
11～15 : Pending同時Claim
16～20 : stale Processing同時復旧
```

ログ設計については、最新の実行ログを追加証跡として保存する場合、
以下の命名を推奨する。

```text
21-logging-timestamp.png
22-logging-invalid-command.png
23-logging-missing-job-id.png
```

---

## 22. At-Least-Once制約

SMTP送信とDB更新は同一トランザクションにできない。

以下のケース：

```text
SMTP送信成功
↓
プロセス停止
↓
Completed更新前
```

では、DB上は `Processing` が残る可能性がある。

その後stale recoveryにより再処理された場合、
同一メールが再送される可能性がある。

したがって現行設計は：

```text
Exactly Once
```

ではなく、

```text
At-Least-Once
```

の特性を持つ。

完全な重複送信防止が必要な場合は以下を別途検討する。

```text
Outbox Pattern
Idempotency Key
Message Queue
送信履歴による重複判定
```

---

## 23. 今後の拡張候補

- 10件超の連続チャンク処理
- Batch実行履歴テーブル
- ExternalJobIdのDB保存
- JSON Console Formatter
- DockerでのBatch実行
- VPS上でのsystemd / cron実行
- JP1/AJS本体との連携
- Outbox Pattern
- Idempotency Key

---

## 24. 詳細設計上の完了条件

以下を満たした時点で、ローカル外部Batchの詳細設計を完了とする。

```text
CLI起動可能
ExitCode返却可能
ExternalJobId / ReminderJobIdを識別可能
UTC Timestampログあり
DB状態遷移を定義済み
SMTP失敗時に再実行可能
stale Processing復旧可能
Pending同時Claimの排他あり
stale復旧競合対策あり
単体テスト成功
PostgreSQL実機確認済み
```

VPS / systemd / cron / JP1連携は後続フェーズとする。
