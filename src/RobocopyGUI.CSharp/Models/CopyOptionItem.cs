using System.ComponentModel;

namespace RobocopyGUI.Models
{
    /// <summary>
    /// コピーオプションのUI表示用アイテムクラス
    /// チェックボックスで選択可能なオプションを表現する
    /// </summary>
    public class CopyOptionItem : INotifyPropertyChanged
    {
        // オプションキー（robocopyのコマンドラインオプション）
        private string _optionKey;
        /// <summary>
        /// オプションキー（例: "/E", "/MIR"）
        /// </summary>
        public string OptionKey
        {
            get => _optionKey;
            set
            {
                _optionKey = value;
                OnPropertyChanged(nameof(OptionKey));
            }
        }

        // 表示名
        private string _displayName;
        /// <summary>
        /// UI上の表示名（日本語）
        /// </summary>
        public string DisplayName
        {
            get => _displayName;
            set
            {
                _displayName = value;
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        // 説明
        private string _description;
        /// <summary>
        /// オプションの詳細説明（ツールチップ用）
        /// </summary>
        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged(nameof(Description));
            }
        }

        // 選択状態
        private bool _isSelected;
        /// <summary>
        /// 選択されているかどうか
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        /// <summary>
        /// プロパティ変更通知イベント
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// プロパティ変更を通知
        /// </summary>
        /// <param name="propertyName">変更されたプロパティ名</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
