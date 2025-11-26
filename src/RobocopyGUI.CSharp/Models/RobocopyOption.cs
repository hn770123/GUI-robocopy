namespace RobocopyGUI.Models
{
    /// <summary>
    /// robocopyのオプション設定を保持するクラス
    /// 各種オプションの有効/無効とパラメータを管理する
    /// </summary>
    public class RobocopyOption
    {
        /// <summary>
        /// サブディレクトリをコピーする (/S)
        /// </summary>
        public bool CopySubdirectories { get; set; }

        /// <summary>
        /// 空のサブディレクトリも含めてコピーする (/E)
        /// </summary>
        public bool CopySubdirectoriesIncludingEmpty { get; set; }

        /// <summary>
        /// ディレクトリツリーをミラーリングする (/MIR)
        /// </summary>
        public bool Mirror { get; set; }

        /// <summary>
        /// 再開可能モードでコピーする (/Z)
        /// </summary>
        public bool RestartMode { get; set; }

        /// <summary>
        /// 全ての属性をコピーする (/COPYALL)
        /// </summary>
        public bool CopyAll { get; set; }

        /// <summary>
        /// リトライ回数 (/R:n)
        /// </summary>
        public int RetryCount { get; set; } = 3;

        /// <summary>
        /// リトライ間の待機時間（秒） (/W:n)
        /// </summary>
        public int RetryWaitTime { get; set; } = 10;

        /// <summary>
        /// マルチスレッド数 (/MT:n)
        /// </summary>
        public int MultiThreadCount { get; set; } = 1;

        /// <summary>
        /// 古いファイルを除外する (/XO)
        /// </summary>
        public bool ExcludeOlder { get; set; }

        /// <summary>
        /// 変更されたファイルを除外する (/XC)
        /// </summary>
        public bool ExcludeChanged { get; set; }

        /// <summary>
        /// 新しいファイルを除外する (/XN)
        /// </summary>
        public bool ExcludeNewer { get; set; }

        /// <summary>
        /// コピー元に存在しないファイルを削除する (/PURGE)
        /// </summary>
        public bool Purge { get; set; }

        /// <summary>
        /// ログファイルのパス (/LOG:path)
        /// </summary>
        public string LogPath { get; set; }
    }
}
