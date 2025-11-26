Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports System.Diagnostics
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading
Imports System.Threading.Tasks
Imports RobocopyGUI_VB.Models

Namespace Services

    ''' <summary>
    ''' robocopyの進捗情報を保持するクラス
    ''' </summary>
    Public Class RobocopyProgress
        ''' <summary>
        ''' 進捗率（0-100）
        ''' </summary>
        Public Property Percentage As Double

        ''' <summary>
        ''' 現在コピー中のファイル名
        ''' </summary>
        Public Property CurrentFile As String

        ''' <summary>
        ''' コピー済みファイル数
        ''' </summary>
        Public Property FilesCopied As Integer

        ''' <summary>
        ''' 総ファイル数
        ''' </summary>
        Public Property TotalFiles As Integer
    End Class

    ''' <summary>
    ''' robocopyの実行結果を保持するクラス
    ''' </summary>
    Public Class RobocopyResult
        ''' <summary>
        ''' 成功したかどうか
        ''' </summary>
        Public Property Success As Boolean

        ''' <summary>
        ''' コピーされたファイル数
        ''' </summary>
        Public Property FilesCopied As Integer

        ''' <summary>
        ''' エラーメッセージ
        ''' </summary>
        Public Property ErrorMessage As String

        ''' <summary>
        ''' 終了コード
        ''' </summary>
        Public Property ExitCode As Integer
    End Class

    ''' <summary>
    ''' robocopyのプレビュー（確認）結果を保持するクラス
    ''' /Lオプションで実行した結果
    ''' </summary>
    Public Class RobocopyPreviewResult
        ''' <summary>
        ''' 成功したかどうか
        ''' </summary>
        Public Property Success As Boolean

        ''' <summary>
        ''' コピー対象ファイルの一覧
        ''' </summary>
        Public Property Files As New ObservableCollection(Of FileItem)

        ''' <summary>
        ''' 合計サイズ（バイト）
        ''' </summary>
        Public Property TotalSize As Long

        ''' <summary>
        ''' エラーメッセージ
        ''' </summary>
        Public Property ErrorMessage As String

        ''' <summary>
        ''' 終了コード
        ''' </summary>
        Public Property ExitCode As Integer
    End Class

    ''' <summary>
    ''' robocopyの実行を管理するサービスクラス
    ''' robocopyコマンドのラッパーとして機能する
    ''' </summary>
    Public Class RobocopyService

        ''' <summary>
        ''' robocopyの確認（プレビュー）を実行
        ''' /Lオプションを使用して実際にはコピーせず、コピー対象ファイルの一覧を取得する
        ''' </summary>
        ''' <param name="source">コピー元パス</param>
        ''' <param name="destination">コピー先パス</param>
        ''' <param name="options">オプション設定</param>
        ''' <param name="cancellationToken">キャンセルトークン</param>
        ''' <returns>プレビュー結果</returns>
        Public Async Function PreviewAsync(
            source As String,
            destination As String,
            options As RobocopyOption,
            cancellationToken As CancellationToken) As Task(Of RobocopyPreviewResult)

            Dim result As New RobocopyPreviewResult()

            Try
                ' コマンドライン引数を構築（/Lオプション付き）
                Dim arguments = BuildArguments(source, destination, options, listOnly:=True)

                ' Processを使用してrobocopyを実行
                Dim processStartInfo As New ProcessStartInfo With {
                    .FileName = "robocopy",
                    .Arguments = arguments,
                    .UseShellExecute = False,
                    .RedirectStandardOutput = True,
                    .RedirectStandardError = True,
                    .CreateNoWindow = True,
                    .StandardOutputEncoding = Encoding.GetEncoding(932) ' Shift-JIS
                }

                Using process As New Process With {.StartInfo = processStartInfo}
                    Dim outputLines As New List(Of String)

                    ' 出力イベントハンドラ
                    AddHandler process.OutputDataReceived, Sub(sender, e)
                                                               If Not String.IsNullOrEmpty(e.Data) Then
                                                                   outputLines.Add(e.Data)
                                                               End If
                                                           End Sub

                    process.Start()
                    process.BeginOutputReadLine()

                    ' キャンセル対応の待機
                    While Not process.HasExited
                        If cancellationToken.IsCancellationRequested Then
                            Try
                                process.Kill()
                            Catch
                            End Try

                            cancellationToken.ThrowIfCancellationRequested()
                        End If

                        Await Task.Delay(100)
                    End While

                    ' robocopyの終了コード解釈
                    result.ExitCode = process.ExitCode
                    result.Success = process.ExitCode < 8

                    If Not result.Success Then
                        result.ErrorMessage = GetExitCodeMessage(process.ExitCode)
                    End If

                    ' 出力からファイル情報を解析
                    ParsePreviewOutput(outputLines, result, source)
                End Using
            Catch ex As OperationCanceledException
                Throw
            Catch ex As Exception
                result.Success = False
                result.ErrorMessage = ex.Message
            End Try

            Return result
        End Function

        ''' <summary>
        ''' プレビュー出力からファイル情報を解析
        ''' </summary>
        Private Sub ParsePreviewOutput(outputLines As List(Of String), result As RobocopyPreviewResult, sourcePath As String)
            ' robocopyの出力パターン
            Dim filePattern As String = "^\s*(新しいファイル|New File|Newer|新しい|変更|Changed|Older|古い)\s+(\d+)\s+(.+)$"

            Dim copyReasonMap As New Dictionary(Of String, String) From {
                {"新しいファイル", "新規"},
                {"New File", "新規"},
                {"Newer", "更新"},
                {"新しい", "更新"},
                {"変更", "変更"},
                {"Changed", "変更"},
                {"Older", "古い"},
                {"古い", "古い"}
            }

            For Each line In outputLines
                ' ファイル情報を含む行を検出
                Dim match = Regex.Match(line, filePattern, RegexOptions.IgnoreCase)
                If match.Success Then
                    Dim reason = match.Groups(1).Value
                    Dim sizeStr = match.Groups(2).Value
                    Dim filePath = match.Groups(3).Value.Trim()

                    Dim fileSize As Long
                    If Long.TryParse(sizeStr, fileSize) Then
                        Dim fileName = IO.Path.GetFileName(filePath)
                        Dim relativePath = IO.Path.GetDirectoryName(filePath)

                        ' コピー理由を日本語に変換
                        Dim copyReason = "コピー"
                        For Each kvp In copyReasonMap
                            If reason.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0 Then
                                copyReason = kvp.Value
                                Exit For
                            End If
                        Next

                        result.Files.Add(New FileItem With {
                            .FileName = fileName,
                            .RelativePath = If(String.IsNullOrEmpty(relativePath), "\", relativePath),
                            .FileSize = fileSize,
                            .LastModified = DateTime.Now,
                            .CopyReason = copyReason
                        })

                        result.TotalSize += fileSize
                    End If
                End If
            Next
        End Sub

        ''' <summary>
        ''' robocopyを非同期で実行
        ''' </summary>
        ''' <param name="source">コピー元パス</param>
        ''' <param name="destination">コピー先パス</param>
        ''' <param name="options">オプション設定</param>
        ''' <param name="progressHandler">進捗報告用</param>
        ''' <param name="cancellationToken">キャンセルトークン</param>
        ''' <returns>実行結果</returns>
        Public Async Function ExecuteAsync(
            source As String,
            destination As String,
            options As RobocopyOption,
            progressHandler As IProgress(Of RobocopyProgress),
            cancellationToken As CancellationToken) As Task(Of RobocopyResult)

            Dim result As New RobocopyResult()

            Try
                ' コマンドライン引数を構築
                Dim arguments = BuildArguments(source, destination, options)

                ' Processを使用してrobocopyを実行
                Dim processStartInfo As New ProcessStartInfo With {
                    .FileName = "robocopy",
                    .Arguments = arguments,
                    .UseShellExecute = False,
                    .RedirectStandardOutput = True,
                    .RedirectStandardError = True,
                    .CreateNoWindow = True,
                    .StandardOutputEncoding = Encoding.GetEncoding(932) ' Shift-JIS
                }

                Using process As New Process With {.StartInfo = processStartInfo}
                    Dim outputBuilder As New StringBuilder()
                    Dim filesCopied As Integer = 0
                    Dim currentProgress As New RobocopyProgress()

                    ' 出力イベントハンドラ
                    AddHandler process.OutputDataReceived, Sub(sender, e)
                                                               If Not String.IsNullOrEmpty(e.Data) Then
                                                                   outputBuilder.AppendLine(e.Data)

                                                                   ' ファイルコピー行を検出
                                                                   If e.Data.Contains("新しいファイル") OrElse
                                                                      e.Data.Contains("New File") OrElse
                                                                      e.Data.Contains("*EXTRA File") OrElse
                                                                      e.Data.TrimStart().StartsWith("100%") Then
                                                                       filesCopied += 1
                                                                       currentProgress.FilesCopied = filesCopied
                                                                       currentProgress.CurrentFile = e.Data.Trim()
                                                                       If progressHandler IsNot Nothing Then
                                                                           progressHandler.Report(currentProgress)
                                                                       End If
                                                                   End If

                                                                   ' 進捗率を解析
                                                                   If e.Data.Contains("%") Then
                                                                       Dim percentIndex = e.Data.IndexOf("%"c)
                                                                       If percentIndex > 0 Then
                                                                           Dim startIndex = percentIndex - 1
                                                                           ' 安全なインデックスチェック
                                                                           While startIndex > 0
                                                                               Dim prevChar = e.Data(startIndex - 1)
                                                                               If Char.IsDigit(prevChar) OrElse prevChar = "."c Then
                                                                                   startIndex -= 1
                                                                               Else
                                                                                   Exit While
                                                                               End If
                                                                           End While

                                                                           Dim percentStr = e.Data.Substring(startIndex, percentIndex - startIndex)
                                                                           Dim percent As Double
                                                                           If Double.TryParse(percentStr, percent) Then
                                                                               currentProgress.Percentage = percent
                                                                               If progressHandler IsNot Nothing Then
                                                                                   progressHandler.Report(currentProgress)
                                                                               End If
                                                                           End If
                                                                       End If
                                                                   End If
                                                               End If
                                                           End Sub

                    process.Start()
                    process.BeginOutputReadLine()

                    ' キャンセル対応の待機
                    While Not process.HasExited
                        If cancellationToken.IsCancellationRequested Then
                            Try
                                process.Kill()
                            Catch
                            End Try

                            cancellationToken.ThrowIfCancellationRequested()
                        End If

                        Await Task.Delay(100)
                    End While

                    ' robocopyの終了コード解釈
                    ' 0-7: 成功（コピー完了）
                    ' 8以上: エラー
                    result.ExitCode = process.ExitCode
                    result.Success = process.ExitCode < 8
                    result.FilesCopied = filesCopied

                    If Not result.Success Then
                        result.ErrorMessage = GetExitCodeMessage(process.ExitCode)
                    End If
                End Using
            Catch ex As OperationCanceledException
                Throw
            Catch ex As Exception
                result.Success = False
                result.ErrorMessage = ex.Message
            End Try

            Return result
        End Function

        ''' <summary>
        ''' コマンドライン引数を構築
        ''' </summary>
        ''' <param name="source">コピー元パス</param>
        ''' <param name="destination">コピー先パス</param>
        ''' <param name="options">オプション設定</param>
        ''' <param name="listOnly">Trueの場合、/Lオプションを追加（実際にコピーせずリスト表示のみ）</param>
        Private Function BuildArguments(source As String, destination As String, options As RobocopyOption, Optional listOnly As Boolean = False) As String
            Dim args As New List(Of String)
            args.Add(String.Format("""{0}""", source))
            args.Add(String.Format("""{0}""", destination))

            ' オプションを追加
            If options.Mirror Then
                args.Add("/MIR")
            ElseIf options.CopySubdirectoriesIncludingEmpty Then
                args.Add("/E")
            ElseIf options.CopySubdirectories Then
                args.Add("/S")
            End If

            If options.RestartMode Then
                args.Add("/Z")
            End If

            If options.CopyAll Then
                args.Add("/COPYALL")
            End If

            If options.RetryCount > 0 Then
                args.Add(String.Format("/R:{0}", options.RetryCount))
            End If

            If options.RetryWaitTime > 0 Then
                args.Add(String.Format("/W:{0}", options.RetryWaitTime))
            End If

            If options.MultiThreadCount > 1 Then
                args.Add(String.Format("/MT:{0}", options.MultiThreadCount))
            End If

            If options.ExcludeOlder Then
                args.Add("/XO")
            End If

            If options.ExcludeChanged Then
                args.Add("/XC")
            End If

            If options.ExcludeNewer Then
                args.Add("/XN")
            End If

            If options.Purge Then
                args.Add("/PURGE")
            End If

            If Not String.IsNullOrEmpty(options.LogPath) AndAlso Not listOnly Then
                args.Add(String.Format("/LOG:""{0}""", options.LogPath))
            End If

            ' リスト表示のみの場合、/Lオプションを追加
            If listOnly Then
                args.Add("/L")  ' リスト表示のみ（実際にコピーしない）
                args.Add("/V")  ' 詳細出力
            End If

            ' 進捗表示用オプション
            args.Add("/NP") ' パーセント表示を抑制（より安定した出力のため）
            args.Add("/NDL") ' ディレクトリリストを抑制

            Return String.Join(" ", args)
        End Function

        ''' <summary>
        ''' 終了コードのメッセージを取得
        ''' </summary>
        Private Function GetExitCodeMessage(exitCode As Integer) As String
            Select Case exitCode
                Case 0
                    Return "コピーなし。ファイルは既に同期されています。"
                Case 1
                    Return "1つ以上のファイルがコピーされました。"
                Case 2
                    Return "追加のファイルまたはディレクトリが検出されました。"
                Case 3
                    Return "ファイルがコピーされ、追加のファイルが検出されました。"
                Case 4
                    Return "不一致のファイルまたはディレクトリが検出されました。"
                Case 5
                    Return "ファイルがコピーされ、不一致が検出されました。"
                Case 6
                    Return "追加のファイルと不一致が検出されました。"
                Case 7
                    Return "ファイルがコピーされ、不一致と追加のファイルが検出されました。"
                Case 8
                    Return "いくつかのファイルまたはディレクトリをコピーできませんでした。"
                Case 16
                    Return "重大なエラー。コピーは実行されませんでした。"
                Case Else
                    Return String.Format("不明なエラー（終了コード: {0}）", exitCode)
            End Select
        End Function

        ''' <summary>
        ''' CUIコマンドを生成
        ''' </summary>
        ''' <param name="source">コピー元パス</param>
        ''' <param name="destination">コピー先パス</param>
        ''' <param name="options">オプション設定</param>
        ''' <returns>生成されたコマンド文字列</returns>
        Public Function GenerateCommand(source As String, destination As String, options As RobocopyOption) As String
            Dim arguments = BuildArguments(source, destination, options)
            Return String.Format("robocopy {0}", arguments)
        End Function

    End Class

End Namespace
