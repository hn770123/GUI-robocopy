Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Text
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
    ''' robocopyの実行を管理するサービスクラス
    ''' robocopyコマンドのラッパーとして機能する
    ''' </summary>
    Public Class RobocopyService

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
                                                                       progressHandler?.Report(currentProgress)
                                                                   End If

                                                                   ' 進捗率を解析
                                                                   If e.Data.Contains("%") Then
                                                                       Dim percentIndex = e.Data.IndexOf("%"c)
                                                                       If percentIndex > 0 Then
                                                                           Dim startIndex = percentIndex - 1
                                                                           While startIndex > 0 AndAlso (Char.IsDigit(e.Data(startIndex - 1)) OrElse e.Data(startIndex - 1) = "."c)
                                                                               startIndex -= 1
                                                                           End While

                                                                           Dim percentStr = e.Data.Substring(startIndex, percentIndex - startIndex)
                                                                           Dim percent As Double
                                                                           If Double.TryParse(percentStr, percent) Then
                                                                               currentProgress.Percentage = percent
                                                                               progressHandler?.Report(currentProgress)
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

                    Await WaitForExitAsync(process)

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
        Private Function BuildArguments(source As String, destination As String, options As RobocopyOption) As String
            Dim args As New List(Of String) From {
                $"""{source}""",
                $"""{destination}"""
            }

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
                args.Add($"/R:{options.RetryCount}")
            End If

            If options.RetryWaitTime > 0 Then
                args.Add($"/W:{options.RetryWaitTime}")
            End If

            If options.MultiThreadCount > 1 Then
                args.Add($"/MT:{options.MultiThreadCount}")
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

            If Not String.IsNullOrEmpty(options.LogPath) Then
                args.Add($"/LOG:""{options.LogPath}""")
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
                    Return $"不明なエラー（終了コード: {exitCode}）"
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
            Return $"robocopy {arguments}"
        End Function

        ''' <summary>
        ''' プロセスの終了を非同期で待機
        ''' .NET Framework 4.6.1には存在しないため追加
        ''' </summary>
        Private Function WaitForExitAsync(process As Process) As Task
            Dim tcs As New TaskCompletionSource(Of Boolean)()

            process.EnableRaisingEvents = True
            AddHandler process.Exited, Sub(s, e) tcs.TrySetResult(True)

            If process.HasExited Then
                Return Task.CompletedTask
            End If

            Return tcs.Task
        End Function

    End Class

End Namespace
