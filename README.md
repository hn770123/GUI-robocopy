# Robocopy GUI ツール

Windows標準のrobocopyコマンドをGUIで操作するためのアプリケーションです。
C#版とVB.NET版の両方を提供しています。

## 機能

- **robocopyの実行**: GUIからrobocopyコマンドを簡単に実行
- **進捗表示**: コピー進捗をプログレスバーで表示
- **ファイル一覧化**: コピー対象ファイルの一覧と総量を表示
- **CUIコマンド生成**: 設定に基づいたrobocopyコマンドを自動生成

## 画面構成

```mermaid
graph TD
    A[メインウィンドウ] --> B[コピー元フォルダ選択]
    A --> C[コピー先フォルダ選択]
    A --> D[コピーオプション設定]
    A --> E[ファイル一覧表示]
    A --> F[進捗表示]
    A --> G[操作ボタン]
    A --> H[CUIコマンド出力]
    
    G --> G1[確認ボタン]
    G --> G2[実行ボタン]
    G --> G3[中止ボタン]
```

## 処理フロー

```mermaid
sequenceDiagram
    participant User as ユーザー
    participant GUI as GUI画面
    participant VM as ViewModel
    participant Service as RobocopyService
    participant Robocopy as robocopy.exe

    User->>GUI: コピー元/先フォルダを指定
    User->>GUI: オプションを選択
    GUI->>VM: 設定を更新
    VM-->>GUI: CUIコマンドを表示

    User->>GUI: 確認ボタンをクリック
    GUI->>VM: ConfirmCommand実行
    VM->>VM: ファイル一覧を取得
    VM-->>GUI: ファイル一覧と総量を表示

    User->>GUI: 実行ボタンをクリック
    GUI->>VM: ExecuteCommand実行
    VM->>Service: robocopyを実行
    Service->>Robocopy: プロセス起動
    
    loop コピー中
        Robocopy-->>Service: 進捗情報
        Service-->>VM: 進捗を報告
        VM-->>GUI: プログレスバー更新
    end

    Robocopy-->>Service: 完了
    Service-->>VM: 結果を返す
    VM-->>GUI: 完了メッセージ表示
```

## コピーオプション

| オプション | 説明 |
|-----------|------|
| サブフォルダもコピー (/E) | 空のサブディレクトリも含めてコピー |
| ミラーリング (/MIR) | ディレクトリツリーをミラーリング（削除も含む） |
| 再開可能モード (/Z) | ネットワーク障害時に再開可能なモードでコピー |
| 全ての属性をコピー (/COPYALL) | ファイルの全ての情報をコピー |
| リトライ回数(3回) (/R:3) | 失敗したコピーのリトライ回数 |
| 待機時間(10秒) (/W:10) | リトライ間の待機時間 |
| マルチスレッド(8) (/MT:8) | 8スレッドでマルチスレッドコピー |
| 古いファイルを除外 (/XO) | コピー先に存在する古いファイルを除外 |
| 変更ファイルを除外 (/XC) | 変更されたファイルを除外 |
| 新しいファイルを除外 (/XN) | 新しいファイルを除外 |
| 余分なファイル削除 (/PURGE) | コピー元に存在しないファイルを削除 |
| ログ出力 (/LOG:robocopy.log) | robocopy.logファイルにログを出力 |

## 技術仕様

- **フレームワーク**: .NET Framework 4.6.1
- **UI**: WPF (Windows Presentation Foundation)
- **アーキテクチャ**: MVVM (Model-View-ViewModel)
- **言語**: C# および VB.NET

## プロジェクト構成

```
├── RobocopyGUI.sln           # ソリューションファイル
└── src/
    ├── RobocopyGUI.CSharp/   # C#版プロジェクト
    │   ├── App.xaml          # アプリケーション定義
    │   ├── MainWindow.xaml   # メインウィンドウUI
    │   ├── ViewModels/       # ViewModelクラス
    │   ├── Models/           # モデルクラス
    │   └── Services/         # サービスクラス
    └── RobocopyGUI.VB/       # VB.NET版プロジェクト
        ├── Application.xaml  # アプリケーション定義
        ├── MainWindow.xaml   # メインウィンドウUI
        ├── ViewModels/       # ViewModelクラス
        ├── Models/           # モデルクラス
        └── Services/         # サービスクラス
```

## ビルド方法

### 前提条件

- Visual Studio 2019 以降
- .NET Framework 4.6.1 開発ツール

### ビルド手順

1. Visual Studioで `RobocopyGUI.sln` を開く
2. ソリューションをビルド（Ctrl+Shift+B）
3. 出力先に実行ファイルが生成される

または、コマンドラインから:

```bash
msbuild RobocopyGUI.sln /p:Configuration=Release
```

## 使用方法

1. アプリケーションを起動
2. 「参照...」ボタンでコピー元とコピー先フォルダを選択
3. 必要なコピーオプションにチェック
4. 「確認」ボタンでコピー対象ファイルを一覧表示
5. 内容を確認後、「実行」ボタンでコピー開始
6. コピー中は「中止」ボタンでキャンセル可能

## 注意事項

- robocopyはWindows標準コマンドのため、Windowsでのみ動作
- 管理者権限が必要な場合があります（アクセス権限のコピー時など）
- `/MIR`オプション使用時は、コピー先の既存ファイルが削除される可能性があります

## ライセンス

MIT License
