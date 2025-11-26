Imports System.Windows.Input

Namespace ViewModels

    ''' <summary>
    ''' ICommandインターフェースの汎用実装
    ''' MVVMパターンでViewからViewModelへのコマンドバインディングを実現する
    ''' </summary>
    Public Class RelayCommand
        Implements ICommand

        ' 実行するアクション
        Private ReadOnly _execute As Action(Of Object)
        ' 実行可能かどうかを判定する関数
        Private ReadOnly _canExecute As Func(Of Object, Boolean)

        ''' <summary>
        ''' RelayCommandを初期化
        ''' </summary>
        ''' <param name="execute">実行するアクション</param>
        ''' <param name="canExecute">実行可能かどうかを判定する関数（省略可）</param>
        Public Sub New(execute As Action(Of Object), Optional canExecute As Func(Of Object, Boolean) = Nothing)
            If execute Is Nothing Then
                Throw New ArgumentNullException(NameOf(execute))
            End If
            _execute = execute
            _canExecute = canExecute
        End Sub

        ''' <summary>
        ''' 実行可能状態が変化したときに発生するイベント
        ''' </summary>
        Public Custom Event CanExecuteChanged As EventHandler Implements ICommand.CanExecuteChanged
            AddHandler(value As EventHandler)
                AddHandler CommandManager.RequerySuggested, value
            End AddHandler
            RemoveHandler(value As EventHandler)
                RemoveHandler CommandManager.RequerySuggested, value
            End RemoveHandler
            RaiseEvent(sender As Object, e As EventArgs)
            End RaiseEvent
        End Event

        ''' <summary>
        ''' コマンドが実行可能かどうかを判定
        ''' </summary>
        ''' <param name="parameter">コマンドパラメータ</param>
        ''' <returns>実行可能ならTrue</returns>
        Public Function CanExecute(parameter As Object) As Boolean Implements ICommand.CanExecute
            Return _canExecute Is Nothing OrElse _canExecute(parameter)
        End Function

        ''' <summary>
        ''' コマンドを実行
        ''' </summary>
        ''' <param name="parameter">コマンドパラメータ</param>
        Public Sub Execute(parameter As Object) Implements ICommand.Execute
            _execute(parameter)
        End Sub

    End Class

End Namespace
