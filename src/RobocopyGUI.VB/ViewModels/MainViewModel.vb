Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.IO
Imports System.Threading
Imports System.Windows
Imports System.Windows.Input
Imports RobocopyGUI_VB.Models
Imports RobocopyGUI_VB.Services

Namespace ViewModels

    ''' <summary>
    ''' メインウィンドウのViewModel
    ''' UI要素とロジックのバインディングを管理する
    ''' </summary>
    Public Class MainViewModel
        Implements INotifyPropertyChanged

        ' robocopyサービスのインスタンス
        Private ReadOnly _robocopyService As RobocopyService
        ' キャンセルトークンソース
        Private _cancellationTokenSource As CancellationTokenSource

#Region "プロパティ"

        ' コピー元パス
        Private _sourcePath As String = String.Empty
        ''' <summary>
        ''' コピー元フォルダのパス
        ''' </summary>
        Public Property SourcePath As String
            Get
                Return _sourcePath
            End Get
            Set(value As String)
                If _sourcePath <> value Then
                    _sourcePath = value
                    OnPropertyChanged("SourcePath")
                    UpdateGeneratedCommand()
                End If
            End Set
        End Property

        ' コピー先パス
        Private _destinationPath As String = String.Empty
        ''' <summary>
        ''' コピー先フォルダのパス
        ''' </summary>
        Public Property DestinationPath As String
            Get
                Return _destinationPath
            End Get
            Set(value As String)
                If _destinationPath <> value Then
                    _destinationPath = value
                    OnPropertyChanged("DestinationPath")
                    UpdateGeneratedCommand()
                End If
            End Set
        End Property

        ' コピーオプション一覧
        Private _copyOptions As ObservableCollection(Of CopyOptionItem)
        ''' <summary>
        ''' コピーオプションの一覧
        ''' </summary>
        Public Property CopyOptions As ObservableCollection(Of CopyOptionItem)
            Get
                Return _copyOptions
            End Get
            Set(value As ObservableCollection(Of CopyOptionItem))
                _copyOptions = value
                OnPropertyChanged("CopyOptions")
            End Set
        End Property

        ' ファイル一覧
        Private _fileList As ObservableCollection(Of FileItem)
        ''' <summary>
        ''' コピー対象ファイルの一覧
        ''' </summary>
        Public Property FileList As ObservableCollection(Of FileItem)
            Get
                Return _fileList
            End Get
            Set(value As ObservableCollection(Of FileItem))
                _fileList = value
                OnPropertyChanged("FileList")
            End Set
        End Property

        ' 総ファイル数
        Private _totalFileCount As Integer
        ''' <summary>
        ''' コピー対象の総ファイル数
        ''' </summary>
        Public Property TotalFileCount As Integer
            Get
                Return _totalFileCount
            End Get
            Set(value As Integer)
                _totalFileCount = value
                OnPropertyChanged("TotalFileCount")
            End Set
        End Property

        ' 合計サイズ表示
        Private _totalFileSizeDisplay As String = "0 B"
        ''' <summary>
        ''' コピー対象の合計サイズ（表示用文字列）
        ''' </summary>
        Public Property TotalFileSizeDisplay As String
            Get
                Return _totalFileSizeDisplay
            End Get
            Set(value As String)
                _totalFileSizeDisplay = value
                OnPropertyChanged("TotalFileSizeDisplay")
            End Set
        End Property

        ' ステータスメッセージ
        Private _statusMessage As String = "待機中"
        ''' <summary>
        ''' 現在の状態を示すメッセージ
        ''' </summary>
        Public Property StatusMessage As String
            Get
                Return _statusMessage
            End Get
            Set(value As String)
                _statusMessage = value
                OnPropertyChanged("StatusMessage")
            End Set
        End Property

        ' 進捗率
        Private _progressPercentage As Double
        ''' <summary>
        ''' コピーの進捗率（0-100）
        ''' </summary>
        Public Property ProgressPercentage As Double
            Get
                Return _progressPercentage
            End Get
            Set(value As Double)
                _progressPercentage = value
                OnPropertyChanged("ProgressPercentage")
            End Set
        End Property

        ' 生成されたコマンド
        Private _generatedCommand As String = String.Empty
        ''' <summary>
        ''' 生成されたrobocopyコマンド
        ''' </summary>
        Public Property GeneratedCommand As String
            Get
                Return _generatedCommand
            End Get
            Set(value As String)
                _generatedCommand = value
                OnPropertyChanged("GeneratedCommand")
            End Set
        End Property

        ' 実行中フラグ
        Private _isRunning As Boolean
        ''' <summary>
        ''' 実行中かどうか
        ''' </summary>
        Public Property IsRunning As Boolean
            Get
                Return _isRunning
            End Get
            Set(value As Boolean)
                _isRunning = value
                OnPropertyChanged("IsRunning")
                OnPropertyChanged("IsNotRunning")
                OnPropertyChanged("ConfirmButtonVisibility")
                OnPropertyChanged("ExecuteButtonVisibility")
                OnPropertyChanged("CancelButtonVisibility")
            End Set
        End Property

        ''' <summary>
        ''' 実行中でないかどうか
        ''' </summary>
        Public ReadOnly Property IsNotRunning As Boolean
            Get
                Return Not _isRunning
            End Get
        End Property

        ''' <summary>
        ''' 確認ボタンの表示状態
        ''' </summary>
        Public ReadOnly Property ConfirmButtonVisibility As Visibility
            Get
                Return If(IsRunning, Visibility.Collapsed, Visibility.Visible)
            End Get
        End Property

        ''' <summary>
        ''' 実行ボタンの表示状態
        ''' </summary>
        Public ReadOnly Property ExecuteButtonVisibility As Visibility
            Get
                Return If(IsRunning, Visibility.Collapsed, Visibility.Visible)
            End Get
        End Property

        ''' <summary>
        ''' 中止ボタンの表示状態
        ''' </summary>
        Public ReadOnly Property CancelButtonVisibility As Visibility
            Get
                Return If(IsRunning, Visibility.Visible, Visibility.Collapsed)
            End Get
        End Property

#End Region

#Region "コマンド"

        ''' <summary>
        ''' コピー元フォルダ参照コマンド
        ''' </summary>
        Public Property BrowseSourceCommand As ICommand

        ''' <summary>
        ''' コピー先フォルダ参照コマンド
        ''' </summary>
        Public Property BrowseDestinationCommand As ICommand

        ''' <summary>
        ''' 確認コマンド
        ''' </summary>
        Public Property ConfirmCommand As ICommand

        ''' <summary>
        ''' 実行コマンド
        ''' </summary>
        Public Property ExecuteCommand As ICommand

        ''' <summary>
        ''' 中止コマンド
        ''' </summary>
        Public Property CancelCommand As ICommand

#End Region

        ''' <summary>
        ''' MainViewModelを初期化
        ''' </summary>
        Public Sub New()
            _robocopyService = New RobocopyService()
            _fileList = New ObservableCollection(Of FileItem)()

            ' コピーオプションを初期化
            InitializeCopyOptions()

            ' コマンドを初期化
            BrowseSourceCommand = New RelayCommand(AddressOf BrowseSource)
            BrowseDestinationCommand = New RelayCommand(AddressOf BrowseDestination)
            ConfirmCommand = New RelayCommand(AddressOf ConfirmExecute, AddressOf CanConfirmFunc)
            ExecuteCommand = New RelayCommand(AddressOf ExecuteRobocopyExecute, AddressOf CanExecuteRobocopyFunc)
            CancelCommand = New RelayCommand(AddressOf Cancel)
        End Sub

        ''' <summary>
        ''' 確認コマンドの実行処理（非同期ラッパー）
        ''' </summary>
        Private Sub ConfirmExecute(parameter As Object)
            Dim task = ConfirmAsync()
        End Sub

        ''' <summary>
        ''' 確認可能かどうかを判定する関数
        ''' </summary>
        Private Function CanConfirmFunc(parameter As Object) As Boolean
            Return CanConfirm()
        End Function

        ''' <summary>
        ''' 実行コマンドの実行処理（非同期ラッパー）
        ''' </summary>
        Private Sub ExecuteRobocopyExecute(parameter As Object)
            Dim task = ExecuteAsync()
        End Sub

        ''' <summary>
        ''' 実行可能かどうかを判定する関数
        ''' </summary>
        Private Function CanExecuteRobocopyFunc(parameter As Object) As Boolean
            Return CanExecuteRobocopy()
        End Function

        ''' <summary>
        ''' コピーオプションを初期化
        ''' </summary>
        Private Sub InitializeCopyOptions()
            CopyOptions = New ObservableCollection(Of CopyOptionItem) From {
                New CopyOptionItem With {.OptionKey = "/E", .DisplayName = "サブフォルダもコピー", .Description = "空のサブディレクトリも含めてコピーします", .IsSelected = True},
                New CopyOptionItem With {.OptionKey = "/MIR", .DisplayName = "ミラーリング", .Description = "ディレクトリツリーをミラーリングします（削除も含む）", .IsSelected = False},
                New CopyOptionItem With {.OptionKey = "/Z", .DisplayName = "再開可能モード", .Description = "ネットワーク障害時に再開可能なモードでコピーします", .IsSelected = False},
                New CopyOptionItem With {.OptionKey = "/COPYALL", .DisplayName = "全ての属性をコピー", .Description = "ファイルの全ての情報をコピーします", .IsSelected = False},
                New CopyOptionItem With {.OptionKey = "/R:3", .DisplayName = "リトライ回数(3回)", .Description = "失敗したコピーのリトライ回数を3回に設定します", .IsSelected = True},
                New CopyOptionItem With {.OptionKey = "/W:10", .DisplayName = "待機時間(10秒)", .Description = "リトライ間の待機時間を10秒に設定します", .IsSelected = True},
                New CopyOptionItem With {.OptionKey = "/MT:8", .DisplayName = "マルチスレッド(8)", .Description = "8スレッドでマルチスレッドコピーを行います", .IsSelected = False},
                New CopyOptionItem With {.OptionKey = "/XO", .DisplayName = "古いファイルを除外", .Description = "コピー先に存在する古いファイルを除外します", .IsSelected = False},
                New CopyOptionItem With {.OptionKey = "/XC", .DisplayName = "変更ファイルを除外", .Description = "変更されたファイルを除外します", .IsSelected = False},
                New CopyOptionItem With {.OptionKey = "/XN", .DisplayName = "新しいファイルを除外", .Description = "新しいファイルを除外します", .IsSelected = False},
                New CopyOptionItem With {.OptionKey = "/PURGE", .DisplayName = "余分なファイル削除", .Description = "コピー元に存在しないファイルを削除します", .IsSelected = False},
                New CopyOptionItem With {.OptionKey = "/LOG:robocopy.log", .DisplayName = "ログ出力", .Description = "robocopy.logファイルにログを出力します", .IsSelected = False}
            }

            ' オプション変更時にコマンドを更新
            For Each item In CopyOptions
                AddHandler item.PropertyChanged, Sub(s, e) UpdateGeneratedCommand()
            Next
        End Sub

        ''' <summary>
        ''' コピー元フォルダを参照
        ''' </summary>
        Private Sub BrowseSource(parameter As Object)
            Dim dialog As New Forms.FolderBrowserDialog With {
                .Description = "コピー元フォルダを選択してください"
            }

            If dialog.ShowDialog() = Forms.DialogResult.OK Then
                SourcePath = dialog.SelectedPath
            End If
        End Sub

        ''' <summary>
        ''' コピー先フォルダを参照
        ''' </summary>
        Private Sub BrowseDestination(parameter As Object)
            Dim dialog As New Forms.FolderBrowserDialog With {
                .Description = "コピー先フォルダを選択してください"
            }

            If dialog.ShowDialog() = Forms.DialogResult.OK Then
                DestinationPath = dialog.SelectedPath
            End If
        End Sub

        ''' <summary>
        ''' 確認可能かどうか判定
        ''' robocopyの/Lオプションを使用するため、コピー元とコピー先の両方が必要
        ''' </summary>
        Private Function CanConfirm() As Boolean
            Return Not IsRunning AndAlso
                   Not String.IsNullOrWhiteSpace(SourcePath) AndAlso
                   Not String.IsNullOrWhiteSpace(DestinationPath) AndAlso
                   Directory.Exists(SourcePath)
        End Function

        ''' <summary>
        ''' 確認処理を実行（robocopyの/Lオプションを使用してコピー対象ファイル一覧を取得）
        ''' </summary>
        Private Async Function ConfirmAsync() As Task
            If Not Directory.Exists(SourcePath) Then
                MessageBox.Show("コピー元フォルダが存在しません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error)
                Return
            End If

            If String.IsNullOrWhiteSpace(DestinationPath) Then
                MessageBox.Show("コピー先フォルダを指定してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error)
                Return
            End If

            IsRunning = True
            StatusMessage = "robocopyでコピー対象を確認中..."
            ProgressPercentage = 0
            _cancellationTokenSource = New CancellationTokenSource()

            Try
                ' 選択されたオプションを取得
                Dim options = GetSelectedOptions()

                ' robocopyの/Lオプションでプレビュー実行
                Dim previewResult = Await _robocopyService.PreviewAsync(
                    SourcePath,
                    DestinationPath,
                    options,
                    _cancellationTokenSource.Token)

                Application.Current.Dispatcher.Invoke(Sub()
                                                          FileList.Clear()

                                                          For Each file In previewResult.Files
                                                              FileList.Add(file)
                                                          Next

                                                          TotalFileCount = FileList.Count
                                                          TotalFileSizeDisplay = FormatFileSize(previewResult.TotalSize)

                                                          If previewResult.Success Then
                                                              StatusMessage = String.Format("確認完了: {0} ファイル（実際にコピーされるファイル数）", TotalFileCount)
                                                          Else
                                                              StatusMessage = String.Format("確認完了（警告あり）: {0} ファイル - {1}", TotalFileCount, previewResult.ErrorMessage)
                                                          End If
                                                          ProgressPercentage = 100
                                                      End Sub)
            Catch ex As OperationCanceledException
                StatusMessage = "確認がキャンセルされました"
            Catch ex As Exception
                StatusMessage = String.Format("エラー: {0}", ex.Message)
                MessageBox.Show(String.Format("確認処理中にエラーが発生しました。{0}{1}", vbCrLf, ex.Message), "エラー", MessageBoxButton.OK, MessageBoxImage.Error)
            Finally
                IsRunning = False
            End Try
        End Function

        ''' <summary>
        ''' 実行可能かどうか判定
        ''' </summary>
        Private Function CanExecuteRobocopy() As Boolean
            Return Not IsRunning AndAlso
                   Not String.IsNullOrWhiteSpace(SourcePath) AndAlso
                   Not String.IsNullOrWhiteSpace(DestinationPath) AndAlso
                   Directory.Exists(SourcePath)
        End Function

        ''' <summary>
        ''' robocopyを実行
        ''' </summary>
        Private Async Function ExecuteAsync() As Task
            If Not Directory.Exists(SourcePath) Then
                MessageBox.Show("コピー元フォルダが存在しません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error)
                Return
            End If

            IsRunning = True
            StatusMessage = "コピー実行中..."
            ProgressPercentage = 0
            _cancellationTokenSource = New CancellationTokenSource()

            Try
                Dim options = GetSelectedOptions()
                Dim progressHandler = New Progress(Of RobocopyProgress)(Sub(p)
                                                                             Application.Current.Dispatcher.Invoke(Sub()
                                                                                                                       ProgressPercentage = p.Percentage
                                                                                                                       StatusMessage = String.Format("コピー中... {0} ({1:F1}%)", p.CurrentFile, p.Percentage)
                                                                                                                   End Sub)
                                                                         End Sub)

                Dim result = Await _robocopyService.ExecuteAsync(
                    SourcePath,
                    DestinationPath,
                    options,
                    progressHandler,
                    _cancellationTokenSource.Token)

                If result.Success Then
                    StatusMessage = String.Format("コピー完了: {0} ファイルをコピーしました", result.FilesCopied)
                    ProgressPercentage = 100
                    MessageBox.Show(String.Format("コピーが完了しました。{0}コピーされたファイル数: {1}", vbCrLf, result.FilesCopied), "完了", MessageBoxButton.OK, MessageBoxImage.Information)
                Else
                    StatusMessage = String.Format("コピー失敗: {0}", result.ErrorMessage)
                    MessageBox.Show(String.Format("コピー中にエラーが発生しました。{0}{1}", vbCrLf, result.ErrorMessage), "エラー", MessageBoxButton.OK, MessageBoxImage.Error)
                End If
            Catch ex As OperationCanceledException
                StatusMessage = "コピーがキャンセルされました"
                MessageBox.Show("コピーがキャンセルされました。", "キャンセル", MessageBoxButton.OK, MessageBoxImage.Warning)
            Catch ex As Exception
                StatusMessage = String.Format("エラー: {0}", ex.Message)
                MessageBox.Show(String.Format("コピー中にエラーが発生しました。{0}{1}", vbCrLf, ex.Message), "エラー", MessageBoxButton.OK, MessageBoxImage.Error)
            Finally
                IsRunning = False
            End Try
        End Function

        ''' <summary>
        ''' 処理をキャンセル
        ''' </summary>
        Private Sub Cancel(parameter As Object)
            If _cancellationTokenSource IsNot Nothing Then
                _cancellationTokenSource.Cancel()
            End If
            StatusMessage = "キャンセル中..."
        End Sub

        ''' <summary>
        ''' 選択されたオプションを取得
        ''' </summary>
        Private Function GetSelectedOptions() As RobocopyOption
            Dim options As New RobocopyOption()

            For Each item In CopyOptions.Where(Function(o) o.IsSelected)
                Select Case item.OptionKey
                    Case "/E"
                        options.CopySubdirectories = True
                        options.CopySubdirectoriesIncludingEmpty = True
                    Case "/MIR"
                        options.Mirror = True
                    Case "/Z"
                        options.RestartMode = True
                    Case "/COPYALL"
                        options.CopyAll = True
                    Case "/R:3"
                        options.RetryCount = 3
                    Case "/W:10"
                        options.RetryWaitTime = 10
                    Case "/MT:8"
                        options.MultiThreadCount = 8
                    Case "/XO"
                        options.ExcludeOlder = True
                    Case "/XC"
                        options.ExcludeChanged = True
                    Case "/XN"
                        options.ExcludeNewer = True
                    Case "/PURGE"
                        options.Purge = True
                    Case "/LOG:robocopy.log"
                        options.LogPath = "robocopy.log"
                End Select
            Next

            Return options
        End Function

        ''' <summary>
        ''' 生成されたコマンドを更新
        ''' </summary>
        Private Sub UpdateGeneratedCommand()
            If String.IsNullOrWhiteSpace(SourcePath) OrElse String.IsNullOrWhiteSpace(DestinationPath) Then
                GeneratedCommand = String.Empty
                Return
            End If

            Dim selectedOptions = CopyOptions.Where(Function(o) o.IsSelected).Select(Function(o) o.OptionKey)
            Dim optionsString = String.Join(" ", selectedOptions)

            GeneratedCommand = String.Format("robocopy ""{0}"" ""{1}"" {2}", SourcePath, DestinationPath, optionsString)
        End Sub

        ''' <summary>
        ''' ファイルサイズを人間が読める形式に変換
        ''' </summary>
        Private Function FormatFileSize(bytes As Long) As String
            Dim suffixes() As String = {"B", "KB", "MB", "GB", "TB"}
            Dim suffixIndex As Integer = 0
            Dim size As Double = bytes

            While size >= 1024 AndAlso suffixIndex < suffixes.Length - 1
                size /= 1024
                suffixIndex += 1
            End While

            Return String.Format("{0:F2} {1}", size, suffixes(suffixIndex))
        End Function

#Region "INotifyPropertyChanged"

        ''' <summary>
        ''' プロパティ変更通知イベント
        ''' </summary>
        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

        ''' <summary>
        ''' プロパティ変更を通知
        ''' </summary>
        ''' <param name="propertyName">変更されたプロパティ名</param>
        Protected Overridable Sub OnPropertyChanged(propertyName As String)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
        End Sub

#End Region

    End Class

End Namespace
