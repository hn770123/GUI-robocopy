using System.Windows;
using RobocopyGUI.ViewModels;

namespace RobocopyGUI
{
    /// <summary>
    /// メインウィンドウのコードビハインド
    /// ViewModelとのバインディングを設定する
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// メインウィンドウを初期化
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            // ViewModelをDataContextに設定
            DataContext = new MainViewModel();
        }
    }
}
