# Invoice System Client (WPF)

[![WPFtest](https://github.com/fewioaghwrao/Invoice-management-system-lite/actions/workflows/dotnet-wpf-tests.yml/badge.svg)](https://github.com/fewioaghwrao/Invoice-management-system-lite/actions/workflows/dotnet-wpf-tests.yml)

C# / WPF で開発した、**Invoice Management System (Lite)** 向けのデスクトップクライアントです。  
既存の **ASP.NET Core Web API** を利用し、**請求・入金・会員管理・売上確認** をデスクトップアプリとして操作できるようにした WPF 版クライアントです。

Web 版の機能をベースにしつつ、WPF では **業務画面の一覧性** と **デスクトップアプリらしい操作性** を意識して再構成しています。

---

## 概要

本アプリは、既存の **Invoice Management System (Lite)** の API を利用する **WPF クライアント**です。  
JWT ベースの認証、Admin / Member ロール別の画面遷移、請求書・入金・会員・売上管理機能を実装しています。

主な特徴は以下の通りです。

- JWT ベースのログイン認証
- Admin / Member ロール別の画面遷移
- 請求書一覧・詳細確認
- 入金確認・入金登録・割当管理
- 会員情報確認・編集
- 売上一覧・顧客別集計
- PDF 出力 / CSV 出力
- ダークテーマをベースにした業務向け UI

本クライアントは、同一リポジトリ内の
ASP.NET Core Web API（`../backend/`）を利用します。

システム全体の構成については、
[ルートREADME](../README.md)を参照してください。

---

## 背景

元システムは、請求・入金管理業務を題材にした業務向けアプリです。  
管理者と会員で利用できる機能を分離し、請求・入金・集計を扱う構成になっています。

本 WPF クライアントは、その既存システムに対する **別クライアント実装** として作成しています。  
Web フロントエンドとは別に、**デスクトップ向け操作性** や **表形式データの見やすさ** を意識して構成しています。

---

## 主な実装機能

### 共通

- ログイン / ログアウト
- JWT を用いた API 認証
- ロールに応じた画面遷移
- `appsettings.json` による API ベース URL 管理
- ダークテーマベースの UI
- カードレイアウト / ボタン / データグリッドの共通表現

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

## スクリーンショット

### ログイン / 共通

#### ログイン画面

![ログイン画面](docs/images/ログイン画面.png)

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

請求・入金ステータスの概要、未入金一覧、月別売上グラフなどをまとめて確認できる管理画面です。

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

請求書を PDF 表示できるようにしており、明細や振込先情報を確認できます。

![請求書PDF出力例](docs/images/請求書PDF出力例.png)

### CSV 出力例

売上一覧や集計結果を CSV として出力し、表計算ソフトで確認できます。

![CSV出力例](docs/images/CSV出力例.png)

---

## 技術スタック

- C# / .NET 10
- WPF（`net10.0-windows`）
- MVVM ベース構成
- HttpClient
- Microsoft.Extensions.Configuration
- JSON ベース設定ファイル（`appsettings.json`）
- OxyPlot（グラフ表示）
- xUnit
- GitHub Actions

---

## 設計方針

UI と API 通信責務を分離するため、以下のような構成を意識しています。

| レイヤー | 役割 |
|----------|------|
| View | 画面表示・イベント受け取り |
| ViewModel | 入力値、画面状態、画面遷移、コマンド管理 |
| Service | API 通信 |
| Model | DTO / Request / Response / 画面表示用データ保持 |

WPF 版では、単なる Web 版の移植ではなく、以下を重視しています。

- 画面の一覧性
- 業務画面としての視認性
- 役割別の操作導線
- 表形式データの扱いやすさ
- デスクトップアプリらしい操作感

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
wpfclient/
├─ InvoiceSystem.Wpf.slnx
├─ InvoiceSystem.Wpf/
│  ├─ Models/
│  ├─ Services/
│  ├─ ViewModels/
│  ├─ Views/
│  ├─ Commands/
│  ├─ Configuration/
│  ├─ Infrastructure/
│  ├─ appsettings.json
│  └─ App.xaml
├─ InvoiceSystem.Wpf.Tests/
│  ├─ Fakes/
│  └─ ViewModels/
├─ docs/
│  └─ images/
└─ README.md
```

---

## 設定ファイル

API ベース URL は `appsettings.json` で管理しています。

```json
{
  "ApiSettings": {
    "BaseUrl": "https://your-api-base-url/"
  }
}
```

`appsettings.json` は以下の設定にしてください。

- **ビルド アクション**: Content
- **出力ディレクトリにコピー**: 新しい場合はコピーする

---

## 動作確認手順

1. `appsettings.json` に API のベース URL を設定
2. バックエンド API を起動
3. WPF アプリを起動
4. ログイン画面から認証
5. ロールに応じて会員画面 / 管理者画面へ遷移
6. 請求書・入金・会員・売上機能を確認

---

## デモアカウント

| 区分 | メールアドレス | パスワード |
|------|---------------|-----------|
| 管理者 | admin@example.com | Admin1234! |
| 会員 | member@example.com | Member1234! |

---

## テスト

本リポジトリでは、WPF クライアントの主要な ViewModel に対して xUnit による単体テストを追加しています。

UI そのものではなく、画面状態・入力値・検索条件・ページング・集計値・バリデーション・Service 呼び出し内容を中心にテストしています。  
外部 API への依存を避けるため、`IAuthService` / `IAccountService` / `IMemberService` / `IInvoiceService` / `IPaymentService` / `ISalesService` などの interface を用意し、テストでは Fake Service を差し込む構成にしています。

### テスト対象

| 分類 | テスト対象 |
|------|------------|
| 認証 | `LoginViewModel`, `RegisterViewModel` |
| 会員画面 | `MemberDashboardViewModel`, `MemberProfileViewModel`, `MemberInvoiceListViewModel`, `MemberInvoiceDetailViewModel`, `MemberPaymentStatusViewModel` |
| 会員管理 | `MemberListViewModel`, `MemberDetailViewModel` |
| 請求管理 | `InvoiceListViewModel`, `AdminInvoiceDetailViewModel`, `AdminInvoiceEditViewModel` |
| 入金管理 | `PaymentListViewModel`, `PaymentCreateViewModel`, `PaymentDetailViewModel` |
| 売上管理 | `SalesListViewModel`, `SalesByMemberViewModel` |
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
- ダイアログ依存箇所の Fake 化

### テスト例

- ログイン成功時に `CurrentUser` が設定され、`LoginSucceeded` が発火すること
- 会員プロフィール保存時に入力値が trim され、任意項目の空白が null 扱いになること
- 請求書作成・編集時に明細行の追加、削除、並び替え、合計金額が正しく反映されること
- 入金登録時に会員選択、入金額、入金方法をもとに `CreatePaymentRequestDto` が作成されること
- 入金詳細画面で割当行の追加、削除、請求書候補の適用、重複・超過チェックが行われること
- 売上一覧・顧客別売上集計で、検索条件・集計値・ページング表示が正しく反映されること
- 催促画面で請求情報、催促履歴、文面プレビュー、次回対応日が正しく表示されること

### 実行方法

```bash
dotnet test
```

または、ソリューションファイルを指定して実行します。

```bash
dotnet test InvoiceSystem.Wpf.slnx
```
### CI

GitHub Actions により、Windows 環境で .NET 10 SDK を使用したビルドとテストを自動実行します。
テスト結果は `.trx` として保存する想定です。

---

## 今後の改善案

- 共通スタイルの整理強化
- 入力バリデーション改善
- エラーハンドリング強化
- テスト対象の継続拡充
- Service / Dialog / File 出力まわりの抽象化強化
- 画面ごとの責務分離のさらなる明確化
- UI コンポーネントの再利用性向上

---

## 関連システム

本クライアントが接続する元システムは、請求・入金・会員管理を扱う **Invoice Management System (Lite)** です。  
Web 版では Next.js フロントエンドと ASP.NET Core バックエンドを分離した構成を採用しています。

---

## ライセンス

このリポジトリは [CC BY 4.0 (Creative Commons Attribution 4.0 International)](https://creativecommons.org/licenses/by/4.0/) のもとで公開しています。

テンプレートや資料を利用・改変・再配布する場合は、ライセンス条件に従って出典表示を行ってください。