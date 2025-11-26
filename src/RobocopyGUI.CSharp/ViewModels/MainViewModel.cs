using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using RobocopyGUI.Models;
using RobocopyGUI.Services;

namespace RobocopyGUI.ViewModels
{
    /// <summary>
    /// メインウィンドウのViewModel
    /// UI要素とロジックのバインディングを管理する
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        // robocopyサービスのインスタンス
        private readonly RobocopyService _robocopyService;
        // キャンセルトークンソース
        private CancellationTokenSource _cancellationTokenSource;

        #region プロパティ

        // コピー元パス
        private string _sourcePath = string.Empty;
        /// <summary>
        /// コピー元フォルダのパス
        /// </summary>
        public string SourcePath
        {
            get => _sourcePath;
            set
            {
                if (_sourcePath != value)
                {
                    _sourcePath = value;
                    OnPropertyChanged(nameof(SourcePath));
                    UpdateGeneratedCommand();
                }
            }
        }

        // コピー先パス
        private string _destinationPath = string.Empty;
        /// <summary>
        /// コピー先フォルダのパス
        /// </summary>
        public string DestinationPath
        {
            get => _destinationPath;
            set
            {
                if (_destinationPath != value)
                {
                    _destinationPath = value;
                    OnPropertyChanged(nameof(DestinationPath));
                    UpdateGeneratedCommand();
                }
            }
        }

        // コピーオプション一覧
        private ObservableCollection<CopyOptionItem> _copyOptions;
        /// <summary>
        /// コピーオプションの一覧
        /// </summary>
        public ObservableCollection<CopyOptionItem> CopyOptions
        {
            get => _copyOptions;
            set
            {
                _copyOptions = value;
                OnPropertyChanged(nameof(CopyOptions));
            }
        }

        // ファイル一覧
        private ObservableCollection<FileItem> _fileList;
        /// <summary>
        /// コピー対象ファイルの一覧
        /// </summary>
        public ObservableCollection<FileItem> FileList
        {
            get => _fileList;
            set
            {
                _fileList = value;
                OnPropertyChanged(nameof(FileList));
            }
        }

        // 総ファイル数
        private int _totalFileCount;
        /// <summary>
        /// コピー対象の総ファイル数
        /// </summary>
        public int TotalFileCount
        {
            get => _totalFileCount;
            set
            {
                _totalFileCount = value;
                OnPropertyChanged(nameof(TotalFileCount));
            }
        }

        // 合計サイズ表示
        private string _totalFileSizeDisplay = "0 B";
        /// <summary>
        /// コピー対象の合計サイズ（表示用文字列）
        /// </summary>
        public string TotalFileSizeDisplay
        {
            get => _totalFileSizeDisplay;
            set
            {
                _totalFileSizeDisplay = value;
                OnPropertyChanged(nameof(TotalFileSizeDisplay));
            }
        }

        // ステータスメッセージ
        private string _statusMessage = "待機中";
        /// <summary>
        /// 現在の状態を示すメッセージ
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        // 進捗率
        private double _progressPercentage;
        /// <summary>
        /// コピーの進捗率（0-100）
        /// </summary>
        public double ProgressPercentage
        {
            get => _progressPercentage;
            set
            {
                _progressPercentage = value;
                OnPropertyChanged(nameof(ProgressPercentage));
            }
        }

        // 生成されたコマンド
        private string _generatedCommand = string.Empty;
        /// <summary>
        /// 生成されたrobocopyコマンド
        /// </summary>
        public string GeneratedCommand
        {
            get => _generatedCommand;
            set
            {
                _generatedCommand = value;
                OnPropertyChanged(nameof(GeneratedCommand));
            }
        }

        // 実行中フラグ
        private bool _isRunning;
        /// <summary>
        /// 実行中かどうか
        /// </summary>
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                _isRunning = value;
                OnPropertyChanged(nameof(IsRunning));
                OnPropertyChanged(nameof(IsNotRunning));
                OnPropertyChanged(nameof(ConfirmButtonVisibility));
                OnPropertyChanged(nameof(ExecuteButtonVisibility));
                OnPropertyChanged(nameof(CancelButtonVisibility));
            }
        }

        /// <summary>
        /// 実行中でないかどうか
        /// </summary>
        public bool IsNotRunning => !_isRunning;

        /// <summary>
        /// 確認ボタンの表示状態
        /// </summary>
        public Visibility ConfirmButtonVisibility => IsRunning ? Visibility.Collapsed : Visibility.Visible;

        /// <summary>
        /// 実行ボタンの表示状態
        /// </summary>
        public Visibility ExecuteButtonVisibility => IsRunning ? Visibility.Collapsed : Visibility.Visible;

        /// <summary>
        /// 中止ボタンの表示状態
        /// </summary>
        public Visibility CancelButtonVisibility => IsRunning ? Visibility.Visible : Visibility.Collapsed;

        #endregion

        #region コマンド

        /// <summary>
        /// コピー元フォルダ参照コマンド
        /// </summary>
        public ICommand BrowseSourceCommand { get; }

        /// <summary>
        /// コピー先フォルダ参照コマンド
        /// </summary>
        public ICommand BrowseDestinationCommand { get; }

        /// <summary>
        /// 確認コマンド
        /// </summary>
        public ICommand ConfirmCommand { get; }

        /// <summary>
        /// 実行コマンド
        /// </summary>
        public ICommand ExecuteCommand { get; }

        /// <summary>
        /// 中止コマンド
        /// </summary>
        public ICommand CancelCommand { get; }

        #endregion

        /// <summary>
        /// MainViewModelを初期化
        /// </summary>
        public MainViewModel()
        {
            _robocopyService = new RobocopyService();
            _fileList = new ObservableCollection<FileItem>();
            
            // コピーオプションを初期化
            InitializeCopyOptions();

            // コマンドを初期化
            BrowseSourceCommand = new RelayCommand(BrowseSource);
            BrowseDestinationCommand = new RelayCommand(BrowseDestination);
            ConfirmCommand = new RelayCommand(async _ => await ConfirmAsync(), _ => CanConfirm());
            ExecuteCommand = new RelayCommand(async _ => await ExecuteAsync(), _ => CanExecute());
            CancelCommand = new RelayCommand(Cancel);
        }

        /// <summary>
        /// コピーオプションを初期化
        /// </summary>
        private void InitializeCopyOptions()
        {
            CopyOptions = new ObservableCollection<CopyOptionItem>
            {
                new CopyOptionItem { OptionKey = "/E", DisplayName = "サブフォルダもコピー", Description = "空のサブディレクトリも含めてコピーします", IsSelected = true },
                new CopyOptionItem { OptionKey = "/MIR", DisplayName = "ミラーリング", Description = "ディレクトリツリーをミラーリングします（削除も含む）", IsSelected = false },
                new CopyOptionItem { OptionKey = "/Z", DisplayName = "再開可能モード", Description = "ネットワーク障害時に再開可能なモードでコピーします", IsSelected = false },
                new CopyOptionItem { OptionKey = "/COPYALL", DisplayName = "全ての属性をコピー", Description = "ファイルの全ての情報をコピーします", IsSelected = false },
                new CopyOptionItem { OptionKey = "/R:3", DisplayName = "リトライ回数(3回)", Description = "失敗したコピーのリトライ回数を3回に設定します", IsSelected = true },
                new CopyOptionItem { OptionKey = "/W:10", DisplayName = "待機時間(10秒)", Description = "リトライ間の待機時間を10秒に設定します", IsSelected = true },
                new CopyOptionItem { OptionKey = "/MT:8", DisplayName = "マルチスレッド(8)", Description = "8スレッドでマルチスレッドコピーを行います", IsSelected = false },
                new CopyOptionItem { OptionKey = "/XO", DisplayName = "古いファイルを除外", Description = "コピー先に存在する古いファイルを除外します", IsSelected = false },
                new CopyOptionItem { OptionKey = "/XC", DisplayName = "変更ファイルを除外", Description = "変更されたファイルを除外します", IsSelected = false },
                new CopyOptionItem { OptionKey = "/XN", DisplayName = "新しいファイルを除外", Description = "新しいファイルを除外します", IsSelected = false },
                new CopyOptionItem { OptionKey = "/PURGE", DisplayName = "余分なファイル削除", Description = "コピー元に存在しないファイルを削除します", IsSelected = false },
                new CopyOptionItem { OptionKey = "/LOG:robocopy.log", DisplayName = "ログ出力", Description = "robocopy.logファイルにログを出力します", IsSelected = false }
            };

            // オプション変更時にコマンドを更新
            foreach (var option in CopyOptions)
            {
                option.PropertyChanged += (s, e) => UpdateGeneratedCommand();
            }
        }

        /// <summary>
        /// コピー元フォルダを参照
        /// </summary>
        private void BrowseSource(object parameter)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "コピー元フォルダを選択してください"
            };
            
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SourcePath = dialog.SelectedPath;
            }
        }

        /// <summary>
        /// コピー先フォルダを参照
        /// </summary>
        private void BrowseDestination(object parameter)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "コピー先フォルダを選択してください"
            };
            
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DestinationPath = dialog.SelectedPath;
            }
        }

        /// <summary>
        /// 確認可能かどうか判定
        /// robocopyの/Lオプションを使用するため、コピー元とコピー先の両方が必要
        /// </summary>
        private bool CanConfirm()
        {
            return !IsRunning && 
                   !string.IsNullOrWhiteSpace(SourcePath) && 
                   !string.IsNullOrWhiteSpace(DestinationPath) &&
                   Directory.Exists(SourcePath);
        }

        /// <summary>
        /// 確認処理を実行（robocopyの/Lオプションを使用してコピー対象ファイル一覧を取得）
        /// </summary>
        private async Task ConfirmAsync()
        {
            if (!Directory.Exists(SourcePath))
            {
                MessageBox.Show("コピー元フォルダが存在しません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(DestinationPath))
            {
                MessageBox.Show("コピー先フォルダを指定してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            IsRunning = true;
            StatusMessage = "robocopyでコピー対象を確認中...";
            ProgressPercentage = 0;
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                // 選択されたオプションを取得
                var options = GetSelectedOptions();
                
                // robocopyの/Lオプションでプレビュー実行
                var previewResult = await _robocopyService.PreviewAsync(
                    SourcePath, 
                    DestinationPath, 
                    options, 
                    _cancellationTokenSource.Token);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    FileList.Clear();
                    
                    foreach (var file in previewResult.Files)
                    {
                        FileList.Add(file);
                    }
                    
                    TotalFileCount = FileList.Count;
                    TotalFileSizeDisplay = FormatFileSize(previewResult.TotalSize);
                    
                    if (previewResult.Success)
                    {
                        StatusMessage = $"確認完了: {TotalFileCount} ファイル（実際にコピーされるファイル数）";
                    }
                    else
                    {
                        StatusMessage = $"確認完了（警告あり）: {TotalFileCount} ファイル - {previewResult.ErrorMessage}";
                    }
                    ProgressPercentage = 100;
                });
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "確認がキャンセルされました";
            }
            catch (Exception ex)
            {
                StatusMessage = $"エラー: {ex.Message}";
                MessageBox.Show($"確認処理中にエラーが発生しました。\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsRunning = false;
            }
        }

        /// <summary>
        /// 実行可能かどうか判定
        /// </summary>
        private bool CanExecute()
        {
            return !IsRunning && 
                   !string.IsNullOrWhiteSpace(SourcePath) && 
                   !string.IsNullOrWhiteSpace(DestinationPath) &&
                   Directory.Exists(SourcePath);
        }

        /// <summary>
        /// robocopyを実行
        /// </summary>
        private async Task ExecuteAsync()
        {
            if (!Directory.Exists(SourcePath))
            {
                MessageBox.Show("コピー元フォルダが存在しません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            IsRunning = true;
            StatusMessage = "コピー実行中...";
            ProgressPercentage = 0;
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                var options = GetSelectedOptions();
                var progress = new Progress<RobocopyProgress>(p =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ProgressPercentage = p.Percentage;
                        StatusMessage = $"コピー中... {p.CurrentFile} ({p.Percentage:F1}%)";
                    });
                });

                var result = await _robocopyService.ExecuteAsync(
                    SourcePath, 
                    DestinationPath, 
                    options, 
                    progress, 
                    _cancellationTokenSource.Token);

                if (result.Success)
                {
                    StatusMessage = $"コピー完了: {result.FilesCopied} ファイルをコピーしました";
                    ProgressPercentage = 100;
                    MessageBox.Show($"コピーが完了しました。\nコピーされたファイル数: {result.FilesCopied}", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    StatusMessage = $"コピー失敗: {result.ErrorMessage}";
                    MessageBox.Show($"コピー中にエラーが発生しました。\n{result.ErrorMessage}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "コピーがキャンセルされました";
                MessageBox.Show("コピーがキャンセルされました。", "キャンセル", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                StatusMessage = $"エラー: {ex.Message}";
                MessageBox.Show($"コピー中にエラーが発生しました。\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsRunning = false;
            }
        }

        /// <summary>
        /// 処理をキャンセル
        /// </summary>
        private void Cancel(object parameter)
        {
            _cancellationTokenSource?.Cancel();
            StatusMessage = "キャンセル中...";
        }

        /// <summary>
        /// 選択されたオプションを取得
        /// </summary>
        private RobocopyOption GetSelectedOptions()
        {
            var option = new RobocopyOption();
            
            foreach (var item in CopyOptions.Where(o => o.IsSelected))
            {
                switch (item.OptionKey)
                {
                    case "/E":
                        option.CopySubdirectories = true;
                        option.CopySubdirectoriesIncludingEmpty = true;
                        break;
                    case "/MIR":
                        option.Mirror = true;
                        break;
                    case "/Z":
                        option.RestartMode = true;
                        break;
                    case "/COPYALL":
                        option.CopyAll = true;
                        break;
                    case "/R:3":
                        option.RetryCount = 3;
                        break;
                    case "/W:10":
                        option.RetryWaitTime = 10;
                        break;
                    case "/MT:8":
                        option.MultiThreadCount = 8;
                        break;
                    case "/XO":
                        option.ExcludeOlder = true;
                        break;
                    case "/XC":
                        option.ExcludeChanged = true;
                        break;
                    case "/XN":
                        option.ExcludeNewer = true;
                        break;
                    case "/PURGE":
                        option.Purge = true;
                        break;
                    case "/LOG:robocopy.log":
                        option.LogPath = "robocopy.log";
                        break;
                }
            }
            
            return option;
        }

        /// <summary>
        /// 生成されたコマンドを更新
        /// </summary>
        private void UpdateGeneratedCommand()
        {
            if (string.IsNullOrWhiteSpace(SourcePath) || string.IsNullOrWhiteSpace(DestinationPath))
            {
                GeneratedCommand = string.Empty;
                return;
            }

            var options = CopyOptions.Where(o => o.IsSelected).Select(o => o.OptionKey);
            var optionsString = string.Join(" ", options);
            
            GeneratedCommand = $"robocopy \"{SourcePath}\" \"{DestinationPath}\" {optionsString}";
        }

        /// <summary>
        /// ファイルサイズを人間が読める形式に変換
        /// </summary>
        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int suffixIndex = 0;
            double size = bytes;
            
            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }
            
            return $"{size:F2} {suffixes[suffixIndex]}";
        }

        #region INotifyPropertyChanged

        /// <summary>
        /// プロパティ変更通知イベント
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// プロパティ変更を通知
        /// </summary>
        /// <param name="propertyName">変更されたプロパティ名</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
