# シーケンス図

Robocopy GUIアプリケーションの主要な処理フローを示します。

## ファイル確認処理

ユーザーがコピー元フォルダを選択して確認ボタンをクリックした時の処理フローです。

```mermaid
sequenceDiagram
    autonumber
    participant User as ユーザー
    participant View as MainWindow
    participant VM as MainViewModel
    participant FileSystem as ファイルシステム

    User->>View: コピー元フォルダを入力
    View->>VM: SourcePathプロパティ更新
    VM->>VM: UpdateGeneratedCommand()
    VM-->>View: GeneratedCommand更新通知
    
    User->>View: 確認ボタンクリック
    View->>VM: ConfirmCommand.Execute()
    VM->>VM: CanConfirm()チェック
    
    alt ソースパスが有効
        VM->>VM: IsRunning = true
        VM-->>View: ボタン表示状態更新
        VM->>VM: StatusMessage = "ファイル一覧を取得中..."
        
        VM->>FileSystem: GetFileList(SourcePath)
        loop 各ファイル
            FileSystem-->>VM: ファイル情報
            VM->>VM: FileItemを作成
        end
        FileSystem-->>VM: ファイル一覧完了
        
        VM->>VM: FileList.Clear()
        VM->>VM: ファイル追加
        VM->>VM: TotalFileCount更新
        VM->>VM: TotalFileSizeDisplay更新
        VM->>VM: StatusMessage = "確認完了"
        VM->>VM: ProgressPercentage = 100
        VM->>VM: IsRunning = false
        VM-->>View: 表示更新通知
    else ソースパスが無効
        VM-->>View: エラーメッセージ表示
    end
```

## robocopy実行処理

ユーザーが実行ボタンをクリックした時のコピー処理フローです。

```mermaid
sequenceDiagram
    autonumber
    participant User as ユーザー
    participant View as MainWindow
    participant VM as MainViewModel
    participant Service as RobocopyService
    participant Process as robocopy.exe

    User->>View: 実行ボタンクリック
    View->>VM: ExecuteCommand.Execute()
    VM->>VM: CanExecute()チェック
    
    alt 実行可能
        VM->>VM: IsRunning = true
        VM-->>View: 中止ボタン表示に切替
        VM->>VM: StatusMessage = "コピー実行中..."
        VM->>VM: CancellationTokenSource作成
        
        VM->>VM: GetSelectedOptions()
        VM->>Service: ExecuteAsync(Source, Dest, Options, Progress, Token)
        Service->>Service: BuildArguments()
        Service->>Process: プロセス起動
        Process-->>Service: 起動完了
        
        loop コピー実行中
            Process-->>Service: 標準出力（進捗）
            Service->>Service: 進捗解析
            Service-->>VM: Progress.Report()
            VM-->>View: ProgressPercentage更新
            VM-->>View: StatusMessage更新
        end
        
        Process-->>Service: 終了コード
        Service->>Service: GetExitCodeMessage()
        Service-->>VM: RobocopyResult
        
        alt コピー成功
            VM->>VM: StatusMessage = "コピー完了"
            VM->>VM: ProgressPercentage = 100
            VM-->>View: 完了メッセージ表示
        else コピー失敗
            VM->>VM: StatusMessage = "コピー失敗"
            VM-->>View: エラーメッセージ表示
        end
        
        VM->>VM: IsRunning = false
        VM-->>View: 確認/実行ボタン表示に切替
    else 実行不可
        VM-->>View: 何もしない
    end
```

## キャンセル処理

ユーザーが中止ボタンをクリックした時の処理フローです。

```mermaid
sequenceDiagram
    autonumber
    participant User as ユーザー
    participant View as MainWindow
    participant VM as MainViewModel
    participant Token as CancellationTokenSource
    participant Service as RobocopyService
    participant Process as robocopy.exe

    Note over Process: robocopy実行中
    
    User->>View: 中止ボタンクリック
    View->>VM: CancelCommand.Execute()
    VM->>Token: Cancel()
    VM->>VM: StatusMessage = "キャンセル中..."
    
    Token-->>Service: キャンセル要求
    Service->>Service: IsCancellationRequested確認
    Service->>Process: Kill()
    Process-->>Service: プロセス終了
    
    Service-->>VM: OperationCanceledException
    VM->>VM: StatusMessage = "キャンセルされました"
    VM-->>View: キャンセルメッセージ表示
    VM->>VM: IsRunning = false
    VM-->>View: 確認/実行ボタン表示に切替
```

## フォルダ参照処理

フォルダ選択ダイアログの表示フローです。

```mermaid
sequenceDiagram
    autonumber
    participant User as ユーザー
    participant View as MainWindow
    participant VM as MainViewModel
    participant Dialog as FolderBrowserDialog

    User->>View: 参照ボタンクリック
    View->>VM: BrowseSourceCommand.Execute()
    VM->>Dialog: new FolderBrowserDialog()
    VM->>Dialog: ShowDialog()
    Dialog-->>User: フォルダ選択画面表示
    
    alt フォルダ選択
        User->>Dialog: フォルダを選択してOK
        Dialog-->>VM: DialogResult.OK
        VM->>VM: SourcePath = SelectedPath
        VM->>VM: UpdateGeneratedCommand()
        VM-->>View: SourcePath更新通知
        VM-->>View: GeneratedCommand更新通知
    else キャンセル
        User->>Dialog: キャンセル
        Dialog-->>VM: DialogResult.Cancel
        Note over VM: 何もしない
    end
```

## アプリケーション起動処理

アプリケーション起動時の初期化フローです。

```mermaid
sequenceDiagram
    autonumber
    participant App as Application
    participant Window as MainWindow
    participant VM as MainViewModel
    participant Service as RobocopyService

    App->>Window: new MainWindow()
    Window->>Window: InitializeComponent()
    Window->>VM: new MainViewModel()
    VM->>Service: new RobocopyService()
    VM->>VM: InitializeCopyOptions()
    
    loop 各オプション
        VM->>VM: CopyOptionItem作成
        VM->>VM: PropertyChangedイベント登録
    end
    
    VM->>VM: BrowseSourceCommand = new RelayCommand()
    VM->>VM: BrowseDestinationCommand = new RelayCommand()
    VM->>VM: ConfirmCommand = new RelayCommand()
    VM->>VM: ExecuteCommand = new RelayCommand()
    VM->>VM: CancelCommand = new RelayCommand()
    
    VM-->>Window: DataContext設定完了
    Window-->>App: ウィンドウ表示準備完了
    App->>Window: Show()
```
