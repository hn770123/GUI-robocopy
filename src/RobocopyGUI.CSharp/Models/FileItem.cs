using System;

namespace RobocopyGUI.Models
{
    /// <summary>
    /// コピー対象ファイルの情報を保持するクラス
    /// ファイル一覧のDataGrid表示用
    /// </summary>
    public class FileItem
    {
        /// <summary>
        /// ファイル名
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 相対パス（コピー元からの相対パス）
        /// </summary>
        public string RelativePath { get; set; }

        /// <summary>
        /// ファイルサイズ（バイト）
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 最終更新日時
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// ファイルサイズの表示用文字列
        /// </summary>
        public string FileSizeDisplay
        {
            get
            {
                // ファイルサイズを人間が読める形式に変換
                string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
                int suffixIndex = 0;
                double size = FileSize;
                
                while (size >= 1024 && suffixIndex < suffixes.Length - 1)
                {
                    size /= 1024;
                    suffixIndex++;
                }
                
                return $"{size:F2} {suffixes[suffixIndex]}";
            }
        }
    }
}
