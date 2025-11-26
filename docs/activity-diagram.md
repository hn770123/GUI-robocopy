---
layout: default
title: アクティビティ図
---

[← ホームに戻る](index.md)

# アクティビティ図

Robocopy GUIアプリケーションの処理フローを示します。

## メイン処理フロー

アプリケーション全体の処理フローです。

```mermaid
flowchart TD
    Start([アプリケーション起動]) --> Init[初期化処理]
    Init --> Wait{ユーザー操作待ち}
    
    Wait -->|コピー元参照| BrowseSource[コピー元フォルダ選択]
    BrowseSource --> UpdateSource[SourcePath更新]
    UpdateSource --> UpdateCmd1[コマンド更新]
    UpdateCmd1 --> Wait
    
    Wait -->|コピー先参照| BrowseDest[コピー先フォルダ選択]
    BrowseDest --> UpdateDest[DestinationPath更新]
    UpdateDest --> UpdateCmd2[コマンド更新]
    UpdateCmd2 --> Wait
    
    Wait -->|オプション変更| OptionChange[オプション選択状態変更]
    OptionChange --> UpdateCmd3[コマンド更新]
    UpdateCmd3 --> Wait
    
    Wait -->|確認| ConfirmProcess[確認処理]
    ConfirmProcess --> Wait
    
    Wait -->|実行| ExecuteProcess[実行処理]
    ExecuteProcess --> Wait
    
    Wait -->|終了| End([アプリケーション終了])
```

## 確認処理フロー

robocopyの`/L`オプションを使用したプレビュー実行の詳細フローです。
コピーオプションを反映した、実際にコピーされるファイルの一覧を取得します。

```mermaid
flowchart TD
    Start([確認ボタンクリック]) --> CheckSource{ソースパス<br/>が有効?}
    
    CheckSource -->|No| Error1[エラーメッセージ表示]
    Error1 --> End1([処理終了])
    
    CheckSource -->|Yes| CheckDest{デスティネーション<br/>パスが有効?}
    CheckDest -->|No| Error2[エラーメッセージ表示]
    Error2 --> End1
    
    CheckDest -->|Yes| SetRunning[IsRunning = true]
    SetRunning --> UpdateStatus1[ステータス更新:<br/>robocopyでコピー対象を確認中]
    UpdateStatus1 --> CreateToken[CancellationTokenSource作成]
    CreateToken --> GetOptions[選択オプション取得]
    GetOptions --> BuildArgs[コマンドライン引数構築<br/>/L /Vオプション付き]
    BuildArgs --> StartRobocopy[robocopyプロセス起動]
    
    StartRobocopy --> WaitLoop{プロセス<br/>終了?}
    
    WaitLoop -->|No| CheckCancel{キャンセル<br/>要求?}
    CheckCancel -->|Yes| KillProcess[プロセス強制終了]
    KillProcess --> Cancelled[キャンセル処理]
    Cancelled --> SetRunning2
    
    CheckCancel -->|No| ReadOutput[標準出力読み取り]
    ReadOutput --> WaitDelay[100ms待機]
    WaitDelay --> WaitLoop
    
    WaitLoop -->|Yes| ParseOutput[出力解析]
    ParseOutput --> CreateFileItems[FileItem作成<br/>CopyReason含む]
    CreateFileItems --> ClearList[既存リストをクリア]
    ClearList --> AddAll[全ファイル追加]
    AddAll --> CalcTotal[合計サイズ計算]
    CalcTotal --> UpdateCount[ファイル数更新]
    UpdateCount --> UpdateSize[サイズ表示更新]
    UpdateSize --> UpdateStatus2[ステータス更新:<br/>確認完了]
    UpdateStatus2 --> SetProgress[進捗 = 100%]
    SetProgress --> SetRunning2[IsRunning = false]
    SetRunning2 --> End2([処理終了])
```

## 実行処理フロー

robocopy実行の詳細フローです。

```mermaid
flowchart TD
    Start([実行ボタンクリック]) --> CheckPaths{パスが<br/>有効?}
    
    CheckPaths -->|No| Error1[エラーメッセージ表示]
    Error1 --> End1([処理終了])
    
    CheckPaths -->|Yes| SetRunning[IsRunning = true]
    SetRunning --> UpdateStatus1[ステータス更新:<br/>コピー実行中]
    UpdateStatus1 --> CreateToken[CancellationTokenSource作成]
    CreateToken --> GetOptions[選択オプション取得]
    GetOptions --> BuildArgs[コマンドライン引数構築]
    BuildArgs --> StartProcess[robocopyプロセス起動]
    
    StartProcess --> WaitLoop{プロセス<br/>終了?}
    
    WaitLoop -->|No| CheckCancel{キャンセル<br/>要求?}
    CheckCancel -->|Yes| KillProcess[プロセス強制終了]
    KillProcess --> Cancelled[キャンセル処理]
    Cancelled --> ShowCancelMsg[キャンセルメッセージ表示]
    ShowCancelMsg --> SetRunning2
    
    CheckCancel -->|No| ReadOutput[標準出力読み取り]
    ReadOutput --> ParseProgress[進捗解析]
    ParseProgress --> UpdateProgress[進捗表示更新]
    UpdateProgress --> WaitDelay[100ms待機]
    WaitDelay --> WaitLoop
    
    WaitLoop -->|Yes| GetExitCode[終了コード取得]
    GetExitCode --> CheckCode{終了コード<br/>< 8?}
    
    CheckCode -->|Yes| Success[成功処理]
    Success --> UpdateStatus2[ステータス更新:<br/>コピー完了]
    UpdateStatus2 --> SetProgress100[進捗 = 100%]
    SetProgress100 --> ShowSuccessMsg[完了メッセージ表示]
    ShowSuccessMsg --> SetRunning2
    
    CheckCode -->|No| Failed[失敗処理]
    Failed --> UpdateStatus3[ステータス更新:<br/>コピー失敗]
    UpdateStatus3 --> ShowErrorMsg[エラーメッセージ表示]
    ShowErrorMsg --> SetRunning2[IsRunning = false]
    
    SetRunning2 --> End2([処理終了])
```

## オプション設定フロー

オプション選択からコマンド生成までのフローです。

```mermaid
flowchart TD
    Start([オプション選択変更]) --> UpdateSelected[IsSelected更新]
    UpdateSelected --> FireEvent[PropertyChanged発火]
    FireEvent --> UpdateCommand[UpdateGeneratedCommand呼び出し]
    
    UpdateCommand --> CheckPaths{ソースとデスト<br/>が設定済み?}
    
    CheckPaths -->|No| ClearCommand[GeneratedCommand = 空]
    ClearCommand --> End1([処理終了])
    
    CheckPaths -->|Yes| GetSelected[選択オプション取得]
    GetSelected --> BuildBase[基本コマンド構築:<br/>robocopy ソース デスト]
    BuildBase --> AddOptions[オプション追加]
    AddOptions --> SetCommand[GeneratedCommand設定]
    SetCommand --> NotifyChange[PropertyChanged通知]
    NotifyChange --> End2([処理終了])
```

## 初期化フロー

アプリケーション起動時の初期化フローです。

```mermaid
flowchart TD
    Start([アプリケーション起動]) --> CreateWindow[MainWindow作成]
    CreateWindow --> InitComponent[InitializeComponent]
    InitComponent --> CreateVM[MainViewModel作成]
    
    CreateVM --> CreateService[RobocopyService作成]
    CreateService --> CreateFileList[FileListコレクション作成]
    CreateFileList --> InitOptions[InitializeCopyOptions]
    
    InitOptions --> LoopOptions{オプション<br/>定義あり?}
    LoopOptions -->|Yes| CreateOption[CopyOptionItem作成]
    CreateOption --> SetDefault[デフォルト値設定]
    SetDefault --> RegisterEvent[PropertyChangedイベント登録]
    RegisterEvent --> AddToCollection[コレクションに追加]
    AddToCollection --> NextOption[次のオプション]
    NextOption --> LoopOptions
    
    LoopOptions -->|No| CreateCommands[コマンド作成]
    CreateCommands --> CreateBrowseSource[BrowseSourceCommand]
    CreateBrowseSource --> CreateBrowseDest[BrowseDestinationCommand]
    CreateBrowseDest --> CreateConfirm[ConfirmCommand]
    CreateConfirm --> CreateExecute[ExecuteCommand]
    CreateExecute --> CreateCancel[CancelCommand]
    CreateCancel --> SetDataContext[DataContext設定]
    SetDataContext --> ShowWindow[ウィンドウ表示]
    ShowWindow --> End([起動完了])
```
