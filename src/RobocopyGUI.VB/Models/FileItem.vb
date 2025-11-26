Namespace Models

    ''' <summary>
    ''' コピー対象ファイルの情報を保持するクラス
    ''' ファイル一覧のDataGrid表示用
    ''' </summary>
    Public Class FileItem

        ''' <summary>
        ''' ファイル名
        ''' </summary>
        Public Property FileName As String

        ''' <summary>
        ''' 相対パス（コピー元からの相対パス）
        ''' </summary>
        Public Property RelativePath As String

        ''' <summary>
        ''' ファイルサイズ（バイト）
        ''' </summary>
        Public Property FileSize As Long

        ''' <summary>
        ''' 最終更新日時
        ''' </summary>
        Public Property LastModified As DateTime

        ''' <summary>
        ''' コピー理由（新しいファイル、変更など）
        ''' </summary>
        Public Property CopyReason As String

        ''' <summary>
        ''' ファイルサイズの表示用文字列
        ''' </summary>
        Public ReadOnly Property FileSizeDisplay As String
            Get
                ' ファイルサイズを人間が読める形式に変換
                Dim suffixes() As String = {"B", "KB", "MB", "GB", "TB"}
                Dim suffixIndex As Integer = 0
                Dim size As Double = FileSize

                While size >= 1024 AndAlso suffixIndex < suffixes.Length - 1
                    size /= 1024
                    suffixIndex += 1
                End While

                Return $"{size:F2} {suffixes(suffixIndex)}"
            End Get
        End Property

    End Class

End Namespace
