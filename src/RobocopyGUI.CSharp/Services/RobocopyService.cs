using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using RobocopyGUI.Models;

namespace RobocopyGUI.Services
{
    /// <summary>
    /// robocopyの進捗情報を保持するクラス
    /// </summary>
    public class RobocopyProgress
    {
        /// <summary>
        /// 進捗率（0-100）
        /// </summary>
        public double Percentage { get; set; }

        /// <summary>
        /// 現在コピー中のファイル名
        /// </summary>
        public string CurrentFile { get; set; }

        /// <summary>
        /// コピー済みファイル数
        /// </summary>
        public int FilesCopied { get; set; }

        /// <summary>
        /// 総ファイル数
        /// </summary>
        public int TotalFiles { get; set; }
    }

    /// <summary>
    /// robocopyの実行結果を保持するクラス
    /// </summary>
    public class RobocopyResult
    {
        /// <summary>
        /// 成功したかどうか
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// コピーされたファイル数
        /// </summary>
        public int FilesCopied { get; set; }

        /// <summary>
        /// エラーメッセージ
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 終了コード
        /// </summary>
        public int ExitCode { get; set; }
    }

    /// <summary>
    /// robocopyのプレビュー（確認）結果を保持するクラス
    /// /Lオプションで実行した結果
    /// </summary>
    public class RobocopyPreviewResult
    {
        /// <summary>
        /// 成功したかどうか
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// コピー対象ファイルの一覧
        /// </summary>
        public ObservableCollection<FileItem> Files { get; set; } = new ObservableCollection<FileItem>();

        /// <summary>
        /// 合計サイズ（バイト）
        /// </summary>
        public long TotalSize { get; set; }

        /// <summary>
        /// エラーメッセージ
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 終了コード
        /// </summary>
        public int ExitCode { get; set; }
    }

    /// <summary>
    /// robocopyの実行を管理するサービスクラス
    /// robocopyコマンドのラッパーとして機能する
    /// </summary>
    public class RobocopyService
    {
        /// <summary>
        /// robocopyの確認（プレビュー）を実行
        /// /Lオプションを使用して実際にはコピーせず、コピー対象ファイルの一覧を取得する
        /// </summary>
        /// <param name="source">コピー元パス</param>
        /// <param name="destination">コピー先パス</param>
        /// <param name="options">オプション設定</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        /// <returns>プレビュー結果</returns>
        public async Task<RobocopyPreviewResult> PreviewAsync(
            string source,
            string destination,
            RobocopyOption options,
            CancellationToken cancellationToken)
        {
            var result = new RobocopyPreviewResult();

            try
            {
                // コマンドライン引数を構築（/Lオプション付き）
                var arguments = BuildArguments(source, destination, options, listOnly: true);

                // Processを使用してrobocopyを実行
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "robocopy",
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.GetEncoding(932) // Shift-JIS
                };

                using (var process = new Process { StartInfo = processStartInfo })
                {
                    var outputLines = new List<string>();

                    // 出力イベントハンドラ
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            outputLines.Add(e.Data);
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();

                    // キャンセル対応の待機
                    while (!process.HasExited)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            try
                            {
                                process.Kill();
                            }
                            catch { }

                            cancellationToken.ThrowIfCancellationRequested();
                        }

                        await Task.Delay(100);
                    }

                    // robocopyの終了コード解釈
                    result.ExitCode = process.ExitCode;
                    result.Success = process.ExitCode < 8;

                    if (!result.Success)
                    {
                        result.ErrorMessage = GetExitCodeMessage(process.ExitCode);
                    }

                    // 出力からファイル情報を解析
                    ParsePreviewOutput(outputLines, result, source);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// プレビュー出力からファイル情報を解析
        /// </summary>
        private void ParsePreviewOutput(List<string> outputLines, RobocopyPreviewResult result, string sourcePath)
        {
            // robocopyの出力パターン
            // "    新しいファイル                123        path\to\file.txt"
            // "         Newer                    456        path\to\file2.txt"
            // "    New File                     789        file.txt"
            var filePatterns = new[]
            {
                @"^\s*(新しいファイル|New File|Newer|新しい|変更|Changed|Older|古い)\s+(\d+)\s+(.+)$",
                @"^\s+(\d+)\s+(.+)$"  // サイズとファイルパスのみの行
            };

            var copyReasonMap = new Dictionary<string, string>
            {
                { "新しいファイル", "新規" },
                { "New File", "新規" },
                { "Newer", "更新" },
                { "新しい", "更新" },
                { "変更", "変更" },
                { "Changed", "変更" },
                { "Older", "古い" },
                { "古い", "古い" }
            };

            foreach (var line in outputLines)
            {
                // ファイル情報を含む行を検出
                var match = Regex.Match(line, filePatterns[0], RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var reason = match.Groups[1].Value;
                    var sizeStr = match.Groups[2].Value;
                    var filePath = match.Groups[3].Value.Trim();

                    if (long.TryParse(sizeStr, out long fileSize))
                    {
                        var fileName = System.IO.Path.GetFileName(filePath);
                        var relativePath = System.IO.Path.GetDirectoryName(filePath);

                        // コピー理由を日本語に変換
                        var copyReason = "コピー";
                        foreach (var kvp in copyReasonMap)
                        {
                            if (reason.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                copyReason = kvp.Value;
                                break;
                            }
                        }

                        result.Files.Add(new FileItem
                        {
                            FileName = fileName,
                            RelativePath = string.IsNullOrEmpty(relativePath) ? "\\" : relativePath,
                            FileSize = fileSize,
                            LastModified = DateTime.Now, // robocopyの/L出力には日時が含まれないため現在時刻で代用
                            CopyReason = copyReason
                        });

                        result.TotalSize += fileSize;
                    }
                }
            }
        }

        /// <summary>
        /// robocopyを非同期で実行
        /// </summary>
        /// <param name="source">コピー元パス</param>
        /// <param name="destination">コピー先パス</param>
        /// <param name="options">オプション設定</param>
        /// <param name="progress">進捗報告用</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        /// <returns>実行結果</returns>
        public async Task<RobocopyResult> ExecuteAsync(
            string source, 
            string destination, 
            RobocopyOption options, 
            IProgress<RobocopyProgress> progress,
            CancellationToken cancellationToken)
        {
            var result = new RobocopyResult();
            
            try
            {
                // コマンドライン引数を構築
                var arguments = BuildArguments(source, destination, options);
                
                // Processを使用してrobocopyを実行
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "robocopy",
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.GetEncoding(932) // Shift-JIS
                };

                using (var process = new Process { StartInfo = processStartInfo })
                {
                    var outputBuilder = new StringBuilder();
                    var filesCopied = 0;
                    var currentProgress = new RobocopyProgress();

                    // 出力イベントハンドラ
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            outputBuilder.AppendLine(e.Data);
                            
                            // ファイルコピー行を検出（例: "New File   123   filename.txt"）
                            if (e.Data.Contains("新しいファイル") || 
                                e.Data.Contains("New File") ||
                                e.Data.Contains("*EXTRA File") ||
                                e.Data.TrimStart().StartsWith("100%"))
                            {
                                filesCopied++;
                                currentProgress.FilesCopied = filesCopied;
                                currentProgress.CurrentFile = e.Data.Trim();
                                progress?.Report(currentProgress);
                            }
                            
                            // 進捗率を解析
                            if (e.Data.Contains("%"))
                            {
                                var percentIndex = e.Data.IndexOf('%');
                                if (percentIndex > 0)
                                {
                                    var startIndex = percentIndex - 1;
                                    // 安全なインデックスチェック
                                    while (startIndex > 0)
                                    {
                                        var prevChar = e.Data[startIndex - 1];
                                        if (char.IsDigit(prevChar) || prevChar == '.')
                                        {
                                            startIndex--;
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                    
                                    var percentStr = e.Data.Substring(startIndex, percentIndex - startIndex);
                                    if (double.TryParse(percentStr, out double percent))
                                    {
                                        currentProgress.Percentage = percent;
                                        progress?.Report(currentProgress);
                                    }
                                }
                            }
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();

                    // キャンセル対応の待機
                    while (!process.HasExited)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            try
                            {
                                process.Kill();
                            }
                            catch { }
                            
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                        
                        await Task.Delay(100);
                    }

                    // robocopyの終了コード解釈
                    // 0-7: 成功（コピー完了）
                    // 8以上: エラー
                    result.ExitCode = process.ExitCode;
                    result.Success = process.ExitCode < 8;
                    result.FilesCopied = filesCopied;
                    
                    if (!result.Success)
                    {
                        result.ErrorMessage = GetExitCodeMessage(process.ExitCode);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// コマンドライン引数を構築
        /// </summary>
        /// <param name="source">コピー元パス</param>
        /// <param name="destination">コピー先パス</param>
        /// <param name="options">オプション設定</param>
        /// <param name="listOnly">trueの場合、/Lオプションを追加（実際にコピーせずリスト表示のみ）</param>
        private string BuildArguments(string source, string destination, RobocopyOption options, bool listOnly = false)
        {
            var args = new List<string>
            {
                $"\"{source}\"",
                $"\"{destination}\""
            };

            // オプションを追加
            if (options.Mirror)
            {
                args.Add("/MIR");
            }
            else if (options.CopySubdirectoriesIncludingEmpty)
            {
                args.Add("/E");
            }
            else if (options.CopySubdirectories)
            {
                args.Add("/S");
            }

            if (options.RestartMode)
            {
                args.Add("/Z");
            }

            if (options.CopyAll)
            {
                args.Add("/COPYALL");
            }

            if (options.RetryCount > 0)
            {
                args.Add($"/R:{options.RetryCount}");
            }

            if (options.RetryWaitTime > 0)
            {
                args.Add($"/W:{options.RetryWaitTime}");
            }

            if (options.MultiThreadCount > 1)
            {
                args.Add($"/MT:{options.MultiThreadCount}");
            }

            if (options.ExcludeOlder)
            {
                args.Add("/XO");
            }

            if (options.ExcludeChanged)
            {
                args.Add("/XC");
            }

            if (options.ExcludeNewer)
            {
                args.Add("/XN");
            }

            if (options.Purge)
            {
                args.Add("/PURGE");
            }

            if (!string.IsNullOrEmpty(options.LogPath) && !listOnly)
            {
                args.Add($"/LOG:\"{options.LogPath}\"");
            }

            // リスト表示のみの場合、/Lオプションを追加
            if (listOnly)
            {
                args.Add("/L");  // リスト表示のみ（実際にコピーしない）
                args.Add("/V");  // 詳細出力
            }

            // 進捗表示用オプション
            args.Add("/NP"); // パーセント表示を抑制（より安定した出力のため）
            args.Add("/NDL"); // ディレクトリリストを抑制

            return string.Join(" ", args);
        }

        /// <summary>
        /// 終了コードのメッセージを取得
        /// </summary>
        private string GetExitCodeMessage(int exitCode)
        {
            switch (exitCode)
            {
                case 0:
                    return "コピーなし。ファイルは既に同期されています。";
                case 1:
                    return "1つ以上のファイルがコピーされました。";
                case 2:
                    return "追加のファイルまたはディレクトリが検出されました。";
                case 3:
                    return "ファイルがコピーされ、追加のファイルが検出されました。";
                case 4:
                    return "不一致のファイルまたはディレクトリが検出されました。";
                case 5:
                    return "ファイルがコピーされ、不一致が検出されました。";
                case 6:
                    return "追加のファイルと不一致が検出されました。";
                case 7:
                    return "ファイルがコピーされ、不一致と追加のファイルが検出されました。";
                case 8:
                    return "いくつかのファイルまたはディレクトリをコピーできませんでした。";
                case 16:
                    return "重大なエラー。コピーは実行されませんでした。";
                default:
                    return $"不明なエラー（終了コード: {exitCode}）";
            }
        }

        /// <summary>
        /// CUIコマンドを生成
        /// </summary>
        /// <param name="source">コピー元パス</param>
        /// <param name="destination">コピー先パス</param>
        /// <param name="options">オプション設定</param>
        /// <returns>生成されたコマンド文字列</returns>
        public string GenerateCommand(string source, string destination, RobocopyOption options)
        {
            var arguments = BuildArguments(source, destination, options);
            return $"robocopy {arguments}";
        }
    }
}
