using System;
using System.Windows.Input;

namespace RobocopyGUI.ViewModels
{
    /// <summary>
    /// ICommandインターフェースの汎用実装
    /// MVVMパターンでViewからViewModelへのコマンドバインディングを実現する
    /// </summary>
    public class RelayCommand : ICommand
    {
        // 実行するアクション
        private readonly Action<object> _execute;
        // 実行可能かどうかを判定する関数
        private readonly Func<object, bool> _canExecute;

        /// <summary>
        /// RelayCommandを初期化
        /// </summary>
        /// <param name="execute">実行するアクション</param>
        /// <param name="canExecute">実行可能かどうかを判定する関数（省略可）</param>
        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// 実行可能状態が変化したときに発生するイベント
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// コマンドが実行可能かどうかを判定
        /// </summary>
        /// <param name="parameter">コマンドパラメータ</param>
        /// <returns>実行可能ならtrue</returns>
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        /// <summary>
        /// コマンドを実行
        /// </summary>
        /// <param name="parameter">コマンドパラメータ</param>
        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }
}
