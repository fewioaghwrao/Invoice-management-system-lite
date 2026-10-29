# Invoice System Client (Avalonia)

[![AvaloniaTest](https://github.com/fewioaghwrao/Invoice-management-system-lite/actions/workflows/avalonia-tests.yml/badge.svg)](https://github.com/fewioaghwrao/Invoice-management-system-lite/actions/workflows/avalonia-tests.yml)

C# / Avalonia UI で開発した、**Invoice Management System (Lite)** 向けのデスクトップクライアントです。  
既存の **ASP.NET Core Web API** を利用し、**請求・入金・会員管理・売上確認** をデスクトップアプリとして操作できます。

同リポジトリの [WPF クライアント](../wpfclient/README.md)（`wpfclient/InvoiceSystem.Wpf`）を**基に Avalonia へ移植**したもので、画面構成・配色・ViewModel・API 通信は WPF 版と同等です。

---

## 概要

本アプリは、既存の **Invoice Management System (Lite)** の API を利用する **Avalonia クライアント**です。  
JWT ベースの認証、Admin / Member ロール別の画面遷移、請求書・入金・会員・売上管理機能を実装しています。

主な特徴は以下の通りです。

- JWT ベースのログイン認証
- Admin / Member ロール別の画面遷移
- 請求書一覧・詳細確認 / 作成・編集
- 入金確認・入金登録・割当管理
- 会員情報確認・編集
- 売上一覧・顧客別集計
- PDF 表示 / CSV 出力
- Web 版（Next.js）・WPF 版とデザインを揃えたダークテーマの業務向け UI
- サイドバー式のシングルウィンドウ構成（画面遷移で別ウィンドウを開かない）

接続先のバックエンド（ASP.NET Core Web API）やシステム全体の構成は、
[リポジトリのルート README](../README.md) と [バックエンド API の README](../backend/README.md) を参照してください。

---

## 背景

元システムは、請求・入金管理業務を題材にした業務向けアプリです。  
管理者と会員で利用できる機能を分離し、請求・入金・集計を扱う構成になっています。

WPF 版クライアントは Windows 専用（`net10.0-windows`）のため、同じ業務画面を **クロスプラットフォームの UI フレームワークである Avalonia** で作り直しました。  
現在は **Desktop 版のみ** を対象としています（Android / iOS / Browser はソリューションから外しています）。

---

## WPF 版からの主な変更点

ViewModel / Service / Model は WPF 版をほぼそのまま移植し、UI 層を Avalonia 向けに置き換えています。

| 項目 | WPF 版 | Avalonia 版 |
|------|--------|-------------|
| 画面定義 | `.xaml` | `.axaml` |
| スタイル | `Themes/*.xaml`（ResourceDictionary）、`Style="{StaticResource 名前}"` | `Themes/AppTheme.axaml` に統合、`Classes="名前"` で指定 |
| 表示切り替え | `Visibility` / `DataTrigger` | `IsVisible`（bool）/ コンバーター（`Helpers/Converters.cs`） |
| グラフ | OxyPlot | 自作の折れ線グラフ（`Controls/MonthlyLineChart.cs`） |
| ダイアログ | `MessageBox` / `ShowDialog()`（同期） | `DialogService`（非同期・`await`） |
| ファイル保存 | `SaveFileDialog` | `StorageProvider.SaveFilePickerAsync` |
| 色 | `Brush` | `IBrush` |
| 設定読み込み | Microsoft.Extensions.Configuration | `System.Text.Json` で `appsettings.json` を直接読み込み |
| テスト | xUnit v2 + `[StaFact]` | xUnit v3 + Avalonia.Headless（ヘッドレス UI スレッドで実行） |

---

## 主な実装機能

### 共通

- ログイン / ログアウト
- 新規会員登録 / パスワード再設定（ログイン画面と同じウィンドウ内で切り替え）
- 利用規約 / プライバシーポリシーは既定のブラウザで Web 版のページを表示
- JWT を用いた API 認証
- ロールに応じた画面遷移
- `appsettings.json` による API ベース URL 管理
- ダークテーマベースの UI
- 共通スタイル（`Themes/AppTheme.axaml`）によるカード / ボタン / 入力欄 / テーブル表現の共通化

### 会員向け機能

- 会員ダッシュボード
- 自分の請求書一覧
- 請求書詳細確認
- PDF 表示
- 未入金状況確認
- プロフィール確認 / 編集

### 管理者向け機能

- 管理者ダッシュボード
- 請求書一覧 / 詳細
- 請求書新規作成 / 編集
- 会員一覧 / 詳細
- 売上一覧
- 顧客別売上集計
- 入金一覧 / 詳細
- 入金登録
- 入金割当管理
- 催促管理
- CSV 出力
- PDF 出力

---

## 画面一覧

### 認証

- ログイン画面
- 新規会員登録画面
- パスワード再設定画面
- ログアウト確認ダイアログ

### 会員画面

- 会員ダッシュボード
- 会員プロフィール
- 会員用請求書一覧
- 会員用請求書詳細
- 会員用入金確認

### 管理者画面

- 管理者ダッシュボード
- 管理者用会員一覧
- 管理者用会員詳細
- 管理者用請求書一覧
- 管理者用請求書詳細
- 管理者用請求書作成・編集
- 管理者用売上一覧
- 顧客別売上一覧
- 催促画面
- 入金一覧
- 入金詳細
- 入金登録

### 出力

- 請求書 PDF 出力
- CSV 出力

---

## 画面構成

ウィンドウは以下の 4 つのみで、各画面はウィンドウ内のページとして切り替えます（WPF 版と同じ構成）。

| ウィンドウ | 内容 |
|------------|------|
| `LoginWindow` | ログイン / 新規会員登録 / パスワード再設定 |
| `MemberShellWindow` | 会員向けの全画面（ダッシュボード・請求書一覧・未入金確認・登録情報・請求書詳細） |
| `AdminShellWindow` | 管理者向けの全画面（左サイドバー + ヘッダー + コンテンツ領域） |
| `ConfirmDialogWindow` | ログアウト・退会などの確認ダイアログ / 完了・エラーのお知らせ |

- 管理者画面は左サイドバー（ダッシュボード / 請求書 / 入金 / 会員 / 売上一覧 / 顧客別集計）から各ページへ移動します。
- ページ間の遷移は `IAdminNavigator`（`AdminShellWindow` が実装）経由で行い、ViewModel には画面遷移用の Action を渡します。
- 一覧ページはインスタンスを保持しているため、詳細から戻っても検索条件やページ位置が維持されます。

---

## スクリーンショット

### ログイン / 共通

#### ログイン画面

![ログイン画面](docs/images/ログイン画面.png)

#### 新規会員登録画面

ログイン画面と同じウィンドウ内で表示する会員登録画面です。登録後はログイン画面に戻り、完了メッセージを表示します。

![新規会員登録画面](docs/images/新規会員登録画面.png)

#### パスワード再設定画面

登録したメールアドレス宛てに再設定リンクを送信する画面です。新しいパスワードはメール内のリンクからブラウザで登録します。

![パスワード再設定画面](docs/images/パスワード再設定画面.png)

#### ログアウト確認

![ログアウト確認](docs/images/ログアウト確認.png)

### 会員向け画面

#### 会員ダッシュボード

ログイン中ユーザー情報、請求書一覧、未入金確認、登録情報確認への導線をまとめた会員向けトップ画面です。

![会員ダッシュボード](docs/images/会員ダッシュボード.png)

#### 会員プロフィール

会員自身の氏名・メールアドレス・電話番号・郵便番号・住所を確認、更新する画面です。

![会員プロフィール](docs/images/会員プロフィール.png)

#### 会員用請求書一覧

年・月・ステータス・キーワードで請求書を絞り込み、一覧で確認できる画面です。

![会員用請求書一覧](docs/images/会員用請求書一覧.png)

#### 会員用請求書詳細

請求番号、請求日、支払期限、請求金額、入金済み金額、残額、備考などを確認できる画面です。

![会員用請求書詳細](docs/images/会員用請求書詳細.png)

#### 会員用入金確認

未払い件数、未払い残額合計、期限超過件数を確認し、未入金・一部入金の請求書を一覧表示する画面です。

![会員用入金確認](docs/images/会員用入金確認.png)

### 管理者向け画面

#### 管理者ダッシュボード

年度切り替えに対応し、売上合計・未入金額・請求書数・入金数・回収率の KPI、月別売上グラフ（四半期で絞り込み可）、未入金一覧（Top5）、回収率ワースト顧客 TOP5 をまとめて確認できる管理画面です。  
月別売上グラフは WPF 版の OxyPlot に代えて、Avalonia の描画 API で自作しています（マウスを乗せると金額を表示）。

![管理者ダッシュボード](docs/images/管理者ダッシュボード.png)

#### 管理者用会員一覧

会員の名前・メール・ロール・状態を検索し、詳細確認や退会操作へ進める画面です。

![管理者用会員一覧](docs/images/管理者用会員一覧.png)

#### 管理者用会員詳細

会員情報の編集、保存、退会（無効化）操作を行う画面です。

![管理者用会員詳細](docs/images/管理者用会員詳細.png)

#### 管理者用請求書一覧

請求番号、会員名、ステータス、請求日などで請求書を検索し、一覧管理できる画面です。

![管理者用請求書一覧](docs/images/管理者用請求書一覧.png)

#### 管理者用請求書詳細

請求基本情報、請求明細、入金履歴、催促履歴を一画面で確認できる画面です。

![管理者用請求書詳細](docs/images/管理者用請求書詳細.png)

#### 管理者用請求書作成

会員・請求日・支払期限・ステータス・備考と明細行（追加 / 削除 / 並び替え、小計・税・合計を自動計算）を入力し、請求書を新規作成する画面です。

![管理者用請求書作成](docs/images/管理者用請求書作成.png)

#### 管理者用請求書編集

既存の請求書の内容・明細を変更して更新する画面です。保存後は請求書詳細へ戻ります。

![管理者用請求書編集](docs/images/管理者用請求書編集.png)

#### 管理者用売上一覧

請求ベースで売上、入金済み、残額、回収率を確認できる一覧画面です。

![管理者用売上一覧](docs/images/管理者用売上一覧.png)

#### 顧客別売上一覧

顧客ごとの請求合計、入金済み、未回収、回収率を集計表示する画面です。

![顧客別売上一覧](docs/images/顧客別売上一覧.png)

#### 催促画面

請求情報、未回収残額、催促設定、メール文面プレビュー、催促履歴をまとめて管理できる画面です。

![催促画面](docs/images/催促画面.png)

#### 入金一覧

入金条件で絞り込み、入金合計、割当済合計、未割当合計とともに一覧で確認できる画面です。

![入金一覧](docs/images/入金一覧.png)

#### 入金詳細

入金金額、割当合計、未割当、入金日、入金名義、入金方法、割当行の編集を行う画面です。

![入金詳細](docs/images/入金詳細.png)

#### 入金登録

会員、入金日、入金名義、入金額、入金方法を指定して手動登録する画面です。

![入金登録](docs/images/入金登録.png)

---

## 出力機能

### 請求書 PDF 出力例

請求書を PDF として取得し、既定のアプリで表示します。明細や振込先情報を確認できます。

![請求書PDF出力例](docs/images/請求書PDF出力例.png)

### CSV 出力例

売上一覧や顧客別集計を CSV として出力し、表計算ソフトで確認できます（保存先は OS のファイル保存ダイアログで選択）。

![CSV出力例](docs/images/CSV出力例.png)

---

## 技術スタック

- C# / .NET 10
- Avalonia UI 12（`net10.0`、Fluent テーマ）
- MVVM ベース構成（CommunityToolkit.Mvvm の `ObservableObject` を基底に使用）
- HttpClient
- JSON ベース設定ファイル（`appsettings.json`）
- xUnit v3 / Avalonia.Headless.XUnit
- GitHub Actions

---

## 設計方針

UI と API 通信責務を分離するため、以下のような構成を意識しています（WPF 版と同じ）。

| レイヤー | 役割 |
|----------|------|
| View | 画面表示・イベント受け取り（シェルウィンドウ + ページ UserControl） |
| ViewModel | 入力値、画面状態、画面遷移、コマンド管理 |
| Service | API 通信 |
| Model | DTO / Request / Response / 画面表示用データ保持 |

Avalonia 版では、WPF 版のロジック（ViewModel / Service / Model）をできるだけ変えずに移植し、  
UI フレームワーク固有の部分（スタイル・ダイアログ・グラフ・ファイル保存）だけを置き換える方針としています。

---

## 使用 API

既存バックエンドの ASP.NET Core Web API を利用しています。  
主に以下の機能群と連携しています。

- 認証 API
- 会員 API
- 請求書 API
- 入金 API
- 売上 API
- 集計 API
- 催促管理 API

ログインでは `/auth/login` を使用しています。

### Request

```json
{
  "email": "admin@example.com",
  "password": "Admin1234!"
}
```

### Response

```json
{
  "id": 1,
  "name": "管理者ユーザー",
  "email": "admin@example.com",
  "role": "Admin",
  "token": "..."
}
```

### ロール

| ロール | 説明 |
|--------|------|
| Admin | 管理者 |
| Member | 会員 |

---

## ディレクトリ構成

```
Invoice-management-system-lite/        # リポジトリ直下
├─ .github/workflows/
│  └─ avalonia-tests.yml               # GitHub Actions（Avalonia クライアントのビルド・テスト）
├─ backend/                            # ASP.NET Core Web API（接続先）
├─ frontend/                           # Next.js Web クライアント
├─ wpfclient/                          # WPF デスクトップクライアント（移植元）
└─ avaloniaClient/                     # ★ Avalonia デスクトップクライアント（本 README）
   ├─ InvoiceAvaloniaClient.slnx
   ├─ InvoiceAvaloniaClient/
   │  ├─ Directory.Packages.props      # NuGet パッケージのバージョン一括管理
   │  ├─ InvoiceAvaloniaClient/        # 共通プロジェクト（画面・ロジック）
   │  │  ├─ Models/
   │  │  ├─ Services/
   │  │  ├─ ViewModels/
   │  │  ├─ Views/
   │  │  ├─ Commands/
   │  │  ├─ Configuration/             # ApiSettings / AppServices（設定読み込み・サービス生成）
   │  │  ├─ Controls/                  # MonthlyLineChart（月別売上グラフ）
   │  │  ├─ Helpers/                   # コンバーター / DialogService / 外部リンク
   │  │  ├─ Infrastructure/
   │  │  ├─ Themes/                    # AppTheme.axaml（共通スタイル）
   │  │  ├─ Assets/
   │  │  └─ App.axaml
   │  ├─ InvoiceAvaloniaClient.Desktop/ # Desktop 版の起動プロジェクト
   │  │  ├─ Program.cs
   │  │  └─ appsettings.json
   │  └─ InvoiceAvaloniaClient.Tests/  # ViewModel の単体テスト
   │     ├─ Fakes/
   │     ├─ ViewModels/
   │     └─ TestAppBuilder.cs
   ├─ docs/
   │  └─ images/
   ├─ .gitignore
   └─ README.md
```

---

## 設定ファイル

API ベース URL は `avaloniaClient/InvoiceAvaloniaClient/InvoiceAvaloniaClient.Desktop/appsettings.json` で管理しています（WPF 版の `wpfclient/InvoiceSystem.Wpf/appsettings.json` と同じ形式）。

```json
{
  "ApiSettings": {
    "BaseUrl": "https://your-api-base-url/"
  }
}
```

`appsettings.json` は Desktop プロジェクトの csproj で **出力ディレクトリにコピー（新しい場合はコピーする）** に設定済みです。  
起動時に `Configuration/AppServices.cs` が実行フォルダの `appsettings.json` を読み込み、`HttpClient` の接続先を設定します。

---

## 動作確認手順

1. リポジトリを取得し、`avaloniaClient` フォルダへ移動

   ```bash
   git clone https://github.com/fewioaghwrao/Invoice-management-system-lite.git
   cd Invoice-management-system-lite/avaloniaClient
   ```

2. `appsettings.json` に API のベース URL を設定
3. バックエンド API を起動（公開中の `https://api.oybusin.com` を使う場合は不要）
4. Desktop 版を起動

   ```bash
   dotnet run --project InvoiceAvaloniaClient/InvoiceAvaloniaClient.Desktop
   ```

5. ログイン画面から認証
6. ロールに応じて会員画面 / 管理者画面へ遷移
7. 請求書・入金・会員・売上機能を確認

---

## デモアカウント

| 区分 | メールアドレス | パスワード |
|------|---------------|-----------|
| 管理者 | admin@example.com | Admin1234! |
| 会員 | member@example.com | Member1234! |

---

## テスト

WPF 版の `InvoiceSystem.Wpf.Tests` を移植し、主要な ViewModel に対して xUnit による単体テストを用意しています。

UI そのものではなく、画面状態・入力値・検索条件・ページング・集計値・バリデーション・Service 呼び出し内容を中心にテストしています。  
外部 API への依存を避けるため、`IAuthService` / `IAccountService` / `IMemberService` / `IInvoiceService` / `IPaymentService` / `ISalesService` などの interface を用意し、テストでは Fake Service を差し込む構成にしています。

Avalonia の ViewModel は UI スレッド（`Dispatcher.UIThread`）経由で変更通知を行うため、テストは **Avalonia.Headless** のヘッドレス UI スレッド上で実行します。  
`TestAppBuilder.cs` の `[assembly: AvaloniaTestFramework]` で全テストに適用しているため、各テストは通常の `[Fact]` / `[Theory]` で記述しています（WPF 版の `[StaFact]` に相当）。

### テスト対象

| 分類 | テスト対象 |
|------|------------|
| 認証 | `LoginViewModel`, `RegisterViewModel` |
| 会員画面 | `MemberDashboardViewModel`, `MemberProfileViewModel`, `MemberInvoiceListViewModel`, `MemberInvoiceDetailViewModel`, `MemberPaymentStatusViewModel` |
| 会員管理 | `MemberListViewModel`, `MemberDetailViewModel` |
| 請求管理 | `InvoiceListViewModel`, `AdminInvoiceDetailViewModel`, `AdminInvoiceEditViewModel` |
| 入金管理 | `PaymentListViewModel`, `PaymentCreateViewModel`, `PaymentDetailViewModel` |
| 売上管理 | `SalesListViewModel` |
| 管理者画面 | `AdminDashboardViewModel`, `AdminCollectionViewModel` |

### 主なテスト観点

- 初期表示時のプロパティ設定
- 検索条件・ページング条件の組み立て
- API 取得結果の ViewModel / 表示用プロパティへの反映
- 金額・日付・ステータス表示の変換
- 入力バリデーション
- 保存・更新・登録時の Request DTO 組み立て
- エラー発生時の `HasError` / `ErrorMessage` 設定
- 画面遷移用 Action / Command の呼び出し
- ダイアログ依存箇所の Fake 化（確認ダイアログは `Task<bool>` を返す関数として差し替え）

### WPF 版テストからの主な変更

- `Visibility.Visible` / `Collapsed` の検証を `Assert.True` / `Assert.False` に変更
- 確認ダイアログの差し替えを `_ => true` から `_ => Task.FromResult(true)` に変更
- 月別売上グラフの検証を、OxyPlot の `PlotModel` からグラフ表示データ（`MonthlyChartData`）に変更
- 色の検証は `#AARRGGBB`（大文字）形式に変換して WPF 版と同じ期待値で比較

### 実行方法

`avaloniaClient` フォルダで実行します。

```bash
cd avaloniaClient
dotnet test InvoiceAvaloniaClient/InvoiceAvaloniaClient.Tests
```

または、ソリューションファイルを指定して実行します。

```bash
dotnet test InvoiceAvaloniaClient.slnx
```

### CI

リポジトリ直下の GitHub Actions（[`.github/workflows/avalonia-tests.yml`](../.github/workflows/avalonia-tests.yml)）により、Windows 環境で .NET 10 SDK を使用したビルドとテストを自動実行します。

- 実行タイミング：`main` / `feature/**` への push、`main` への pull request
- 対象パス：`avaloniaClient/**` と ワークフローファイル自身（WPF / Web / API のみの変更では実行されません）
- 作業ディレクトリ：`avaloniaClient`

テスト結果は `.trx` として保存し、Artifact（`avalonia-test-results`）からダウンロードできます。

---

## 今後の改善案

- Android / iOS / Browser 版への対応（共通プロジェクトの再利用）
- macOS / Linux での動作確認と CI 追加
- `SalesByMemberViewModel` などテスト未作成の ViewModel へのテスト追加
- Avalonia.Headless を使った画面（View）レベルのテスト追加
- 入力バリデーション改善
- エラーハンドリング強化
- Service / Dialog / File 出力まわりの抽象化強化
- UI コンポーネントの再利用性向上

---

## 関連システム

本クライアントは、請求・入金・会員管理を扱う **Invoice Management System (Lite)** リポジトリの一部です。  
同じバックエンド API を、Web / WPF / Avalonia の 3 種類のクライアントから利用しています。

- [システム全体（ルート README）](../README.md)
- [バックエンド API](../backend/README.md)
- [Web フロントエンド（Next.js）](../frontend/README.md)
- [WPF デスクトップクライアント（移植元）](../wpfclient/README.md)

---

## ライセンス

このリポジトリは [CC BY 4.0 (Creative Commons Attribution 4.0 International)](https://creativecommons.org/licenses/by/4.0/) のもとで公開しています。

テンプレートや資料を利用・改変・再配布する場合は、ライセンス条件に従って出典表示を行ってください。
