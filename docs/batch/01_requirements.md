# Invoice Reminder Batch 要件定義書

## 1. 目的

本機能は、Invoice Management Systemに存在するリマインダー処理を、JP1/AJS、cron、systemd等の外部ジョブ管理・スケジューリング基盤から起動可能な業務バッチとして実行できるようにすることを目的とする。

既存のリマインダー業務ロジックを可能な限り再利用し、以下の設計要素を実装・検証する。

- コマンドラインからの外部起動
- 起動パラメータ
- プロセス終了コード
- 構造化ログ
- 処理結果の取得
- 再実行性
- リトライ
- 二重実行防止
- 障害発生時の復旧
- API内BackgroundServiceとの責任分離

本機能ではJP1/AJS自体は導入せず、ローカル環境ではPowerShell、VPS環境ではsystemdまたはcron等を使用して外部ジョブ管理製品からの起動を模擬する。

---

## 2. 現行機能

### 2.1 ReminderJob

現在、リマインダー送信対象はReminderJobとして管理されている。

ReminderJobは主に以下の情報を保持する。

- InvoiceId
- ToEmail
- Subject
- Body
- Status
- RetryCount
- ErrorMessage
- CreatedAt
- StartedAt
- CompletedAt

Statusの初期値は `Pending` とする。

---

### 2.2 ReminderJobProcessor

現在のReminderJobProcessorは、以下の条件を満たすReminderJobを処理対象とする。

```text
Status = Pending
AND
RetryCount < 3
```

対象はCreatedAtの昇順で取得し、1回の実行につき最大10件を処理する。

**処理開始時：**

```text
Status = Processing
StartedAt = 現在日時
ErrorMessage = null
```

**メール送信成功時：**

```text
Status = Completed
CompletedAt = 現在日時
```

**メール送信失敗時：**

```text
RetryCount = RetryCount + 1
```

- RetryCountが3未満の場合：`Status = Pending`
- RetryCountが3以上の場合：`Status = Failed`

---

### 2.3 ReminderJobWorker

現在はReminderJobWorkerがBackgroundServiceとしてAPIプロセス内で常駐している。

ReminderJobWorkerは一定間隔でIReminderJobProcessorを取得し、ReminderJobProcessorを実行する。

現在の実行間隔は30秒とする。

---

## 3. 外部バッチ化の基本方針

既存のReminderJobProcessorの業務処理をInvoiceSystem.Batchから再利用できる構成とする。

想定構成：

```text
外部ジョブ管理
JP1 / systemd / cron
        |
        | CLI起動
        v
InvoiceSystem.Batch
        |
        v
IReminderJobProcessor
        |
        v
ReminderJobProcessor
        |
        +------ PostgreSQL
        |
        +------ IEmailSender
```

ReminderJobProcessorの業務ロジックをInvoiceSystem.Batch側へコピーしてはならない。

API内のReminderJobWorkerとInvoiceSystem.Batchの双方から、共通のIReminderJobProcessorを利用する構成とする。

---

## 4. バッチ起動要件

### 4.1 起動方式

InvoiceSystem.Batchはコマンドラインから起動できること。

想定例：

```powershell
dotnet InvoiceSystem.Batch.dll reminder `
  --job-id REMINDER-20260919-001
```

開発環境では以下の形式でも実行可能とする。

```powershell
dotnet run --project backend/InvoiceSystem.Batch -- `
  reminder `
  --job-id REMINDER-20260919-001
```

### 4.2 起動パラメータ

初期実装では以下を使用する。

| パラメータ | 必須 | 内容 |
|---|---|---|
| `command` | Yes | 実行するバッチ種別。初期値は `reminder` |
| `--job-id` | Yes | 外部ジョブ実行を識別するID |

`--target-date` は初期実装では使用しない。

現行ReminderJobProcessorはStatusおよびRetryCountによって処理対象を決定しており、業務日付を条件としていないためである。

業務日付による抽出が必要となった場合は、後続フェーズで追加を検討する。

---

## 5. 処理要件

InvoiceSystem.Batchは以下の順序で処理する。

1. 起動パラメータを取得する
2. 起動パラメータを検証する
3. JobIdをログへ記録する
4. DIコンテナを構築する
5. DB接続設定を読み込む
6. IReminderJobProcessorを取得する
7. Pending状態のReminderJobを処理する
8. 処理結果を取得する
9. 処理件数をログへ記録する
10. 処理結果に応じたExitCodeを返却する

---

## 6. 処理結果要件

外部ジョブ管理製品がバッチ処理結果を判断できるよう、ReminderJobProcessorは処理結果を呼び出し元へ返却できること。

最低限、以下の件数を取得可能とする。

- 対象件数
- 正常完了件数
- リトライ対象件数
- Failed件数

想定結果：

```text
TargetCount       = 10
CompletedCount    = 8
RetryPendingCount = 2
FailedCount       = 0
```

---

## 7. ExitCode要件

InvoiceSystem.Batchは処理結果をプロセス終了コードとして返却する。

| ExitCode | 分類 | 内容 |
|---:|---|---|
| 0 | 正常 | 全対象処理が正常終了 |
| 10 | 正常 | 処理対象なし |
| 20 | 業務エラー | 起動パラメータ不正等 |
| 50 | システムエラー | DB、メール送信等の処理失敗 |
| 99 | 想定外エラー | 未分類例外 |

外部ジョブ管理側はExitCodeを使用して後続ジョブの実行可否を判断できるものとする。

---

## 8. ログ要件

以下をログへ記録する。

- Timestamp
- LogLevel
- JobId
- BatchName
- 処理開始
- 処理終了
- 対象件数
- 正常件数
- Retry件数
- Failed件数
- ExitCode
- エラー内容

例：

```text
INFO
JobId=REMINDER-20260919-001
BatchName=Reminder
Event=BatchStarted
```

正常終了例：

```text
INFO
JobId=REMINDER-20260919-001
TargetCount=10
CompletedCount=10
RetryPendingCount=0
FailedCount=0
ExitCode=0
Event=BatchCompleted
```

---

## 9. 再実行要件

### 9.1 Completedジョブ

StatusがCompletedのReminderJobは再実行時に処理対象としてはならない。

### 9.2 Retry対象

メール送信等で処理に失敗し、RetryCountが3未満のReminderJobはPendingへ戻し、後続実行で再処理可能とする。

### 9.3 Failedジョブ

RetryCountが3以上となったReminderJobはFailedとし、自動再実行対象から除外する。

### 9.4 処理途中でのプロセス停止

StatusがProcessingとなった後に、以下等が発生した場合でも、ReminderJobが永久にProcessing状態として残存しない仕組みを設ける。

- アプリケーション異常終了
- Docker停止
- VPS停止
- プロセスKill

一定時間以上Processing状態であるジョブを再実行可能とする方法を後続設計で定義する。

---

## 10. 二重実行防止要件

API内のReminderJobWorkerとInvoiceSystem.Batchが同時に同一ReminderJobを処理しないこと。

外部ジョブ管理によってInvoiceSystem.Batchを使用する環境では、原則としてAPI内ReminderJobWorkerを停止可能とする。

設定例：

```text
ReminderWorker:Enabled = false
```

また、バッチ自体が多重起動された場合についても、同一ReminderJobを複数プロセスが同時処理しないための排他方式を後続設計で定義する。

---

## 11. API内Workerとの切替要件

ReminderJobWorkerは設定によって有効・無効を切り替えられること。

### 開発・既存方式

```text
ReminderWorker.Enabled = true

InvoiceSystem.Api
    |
    v
ReminderJobWorker
    |
    v
ReminderJobProcessor
```

### 外部ジョブ管理方式

```text
ReminderWorker.Enabled = false

systemd / cron / JP1
    |
    v
InvoiceSystem.Batch
    |
    v
ReminderJobProcessor
```

これにより、API内スケジューリング方式と外部ジョブ管理方式の責任を分離する。

---

## 12. ReminderHistoryとの責任分離

ReminderHistoryは、請求書に対して実施した督促内容を記録する業務履歴として扱う。

以下のようなバッチ実行そのものの管理情報とは分離する。

- JobId
- BatchName
- StartedAt
- CompletedAt
- TargetCount
- CompletedCount
- FailedCount
- ExitCode

バッチ実行履歴の永続化が必要となる場合は、ReminderHistoryを流用せず、専用の実行履歴モデルを検討する。

---

## 13. 非機能要件

### 13.1 再実行性

障害原因解消後に同一バッチを再実行可能であること。

正常完了済みジョブについては重複処理しないこと。

### 13.2 可観測性

障害発生時に、JobIdおよびReminderJob.Idから該当ログを追跡可能であること。

### 13.3 既存機能への影響

既存API、WPFクライアント、Next.jsフロントエンドの通常機能へ影響を与えないこと。

既存ReminderJobProcessorの業務ロジックを可能な限り共通利用すること。

---

## 14. 初期フェーズ対象外

以下は要件として考慮するが、InvoiceSystem.Batchの初期実装では対象外とする。

- JP1/AJS本体の導入
- JP1/AJS固有ジョブネット設定
- 複数種類の業務バッチ
- 日付指定によるReminderJob抽出
- 複雑なジョブ依存関係
- バッチ管理用Web画面
- 分散環境での高度な排他制御

---

## 15. 完了条件

本対応は以下を確認できた場合に完了とする。

- [ ] InvoiceSystem.BatchをCLIから起動できる
- [ ] JobIdを受け取れる
- [ ] 既存ReminderJobProcessorを利用できる
- [ ] Pendingジョブを処理できる
- [ ] Completedジョブを再処理しない
- [ ] 処理結果を件数として取得できる
- [ ] 処理結果に応じたExitCodeを返却できる
- [ ] 障害発生後に再実行できる
- [ ] Processing残留ジョブを復旧可能である
- [ ] API内WorkerとBatchの二重実行を防止できる
- [ ] ログからJobIdとReminderJob.Idを追跡できる
- [ ] VPS上で外部スケジューラから起動できる

---

## 16. 後続フェーズ

```text
01_requirements.md
        ↓
02_basic-design.md
        ↓
ReminderJobProcessor結果返却対応
        ↓
ReminderJobWorker有効/無効切替
        ↓
InvoiceSystem.Batch追加
        ↓
ローカルDB実行
        ↓
再実行試験
        ↓
Processing残留復旧
        ↓
多重起動対策
        ↓
Docker化
        ↓
VPS配置
        ↓
systemd / cronによる外部起動
        ↓
README・証跡
```