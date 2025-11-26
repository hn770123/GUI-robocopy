Namespace Models

    ''' <summary>
    ''' robocopyのオプション設定を保持するクラス
    ''' 各種オプションの有効/無効とパラメータを管理する
    ''' </summary>
    Public Class RobocopyOption

        ''' <summary>
        ''' サブディレクトリをコピーする (/S)
        ''' </summary>
        Public Property CopySubdirectories As Boolean

        ''' <summary>
        ''' 空のサブディレクトリも含めてコピーする (/E)
        ''' </summary>
        Public Property CopySubdirectoriesIncludingEmpty As Boolean

        ''' <summary>
        ''' ディレクトリツリーをミラーリングする (/MIR)
        ''' </summary>
        Public Property Mirror As Boolean

        ''' <summary>
        ''' 再開可能モードでコピーする (/Z)
        ''' </summary>
        Public Property RestartMode As Boolean

        ''' <summary>
        ''' 全ての属性をコピーする (/COPYALL)
        ''' </summary>
        Public Property CopyAll As Boolean

        ''' <summary>
        ''' リトライ回数 (/R:n)
        ''' </summary>
        Public Property RetryCount As Integer = 3

        ''' <summary>
        ''' リトライ間の待機時間（秒） (/W:n)
        ''' </summary>
        Public Property RetryWaitTime As Integer = 10

        ''' <summary>
        ''' マルチスレッド数 (/MT:n)
        ''' </summary>
        Public Property MultiThreadCount As Integer = 1

        ''' <summary>
        ''' 古いファイルを除外する (/XO)
        ''' </summary>
        Public Property ExcludeOlder As Boolean

        ''' <summary>
        ''' 変更されたファイルを除外する (/XC)
        ''' </summary>
        Public Property ExcludeChanged As Boolean

        ''' <summary>
        ''' 新しいファイルを除外する (/XN)
        ''' </summary>
        Public Property ExcludeNewer As Boolean

        ''' <summary>
        ''' コピー元に存在しないファイルを削除する (/PURGE)
        ''' </summary>
        Public Property Purge As Boolean

        ''' <summary>
        ''' ログファイルのパス (/LOG:path)
        ''' </summary>
        Public Property LogPath As String

    End Class

End Namespace
