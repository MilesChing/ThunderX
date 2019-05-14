using System;

namespace TX.Models
{
    /// <summary>
    /// 结构体，用于存储初始化新任务的信息
    /// </summary>
    public class DownloaderSettings
    {
        /// <summary>
        /// 任务链接
        /// </summary>
        public string Url;

        /// <summary>
        /// 指定的文件全名（含扩展名）
        /// </summary>
        public string FileName;

        /// <summary>
        /// 需要使用的线程数
        /// </summary>
        public int? Threads;

        /// <summary>
        /// 文件大小
        /// </summary>
        public long? Size;

        /// <summary>
        /// 临时文件的路径（含文件名）
        /// </summary>
        public string FilePath;

        /// <summary>
        /// 任务允许的最大自动重试次数
        /// </summary>
        public int MaximumRetries;
    }
}
