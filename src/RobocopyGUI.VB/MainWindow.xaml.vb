Imports RobocopyGUI_VB.ViewModels

''' <summary>
''' メインウィンドウのコードビハインド
''' ViewModelとのバインディングを設定する
''' </summary>
Class MainWindow

    ''' <summary>
    ''' メインウィンドウを初期化
    ''' </summary>
    Public Sub New()
        InitializeComponent()
        ' ViewModelをDataContextに設定
        DataContext = New MainViewModel()
    End Sub

End Class
