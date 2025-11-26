Imports System.ComponentModel

Namespace Models

    ''' <summary>
    ''' コピーオプションのUI表示用アイテムクラス
    ''' チェックボックスで選択可能なオプションを表現する
    ''' </summary>
    Public Class CopyOptionItem
        Implements INotifyPropertyChanged

        ' オプションキー（robocopyのコマンドラインオプション）
        Private _optionKey As String
        ''' <summary>
        ''' オプションキー（例: "/E", "/MIR"）
        ''' </summary>
        Public Property OptionKey As String
            Get
                Return _optionKey
            End Get
            Set(value As String)
                _optionKey = value
                OnPropertyChanged(NameOf(OptionKey))
            End Set
        End Property

        ' 表示名
        Private _displayName As String
        ''' <summary>
        ''' UI上の表示名（日本語）
        ''' </summary>
        Public Property DisplayName As String
            Get
                Return _displayName
            End Get
            Set(value As String)
                _displayName = value
                OnPropertyChanged(NameOf(DisplayName))
            End Set
        End Property

        ' 説明
        Private _description As String
        ''' <summary>
        ''' オプションの詳細説明（ツールチップ用）
        ''' </summary>
        Public Property Description As String
            Get
                Return _description
            End Get
            Set(value As String)
                _description = value
                OnPropertyChanged(NameOf(Description))
            End Set
        End Property

        ' 選択状態
        Private _isSelected As Boolean
        ''' <summary>
        ''' 選択されているかどうか
        ''' </summary>
        Public Property IsSelected As Boolean
            Get
                Return _isSelected
            End Get
            Set(value As Boolean)
                If _isSelected <> value Then
                    _isSelected = value
                    OnPropertyChanged(NameOf(IsSelected))
                End If
            End Set
        End Property

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

    End Class

End Namespace
