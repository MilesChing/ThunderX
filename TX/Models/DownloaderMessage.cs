using System;

namespace TX.Models
{
    public class DownloaderMessage
    {
        /// <summary>
        /// 空构造函数
        /// </summary>
        public DownloaderMessage()
        {
            TempFilePath = null;
            URL = null;
            FileName = null;
            Extention = null;
            DownloadSize = 0;
            FileSize = null;
            FolderPath = null;
            Threads = new ThreadMessage();
            IsDone = false;
        }

        public DownloaderMessage(NullableAttributesDownloaderMessage message)
        {
            TempFilePath = message.TempFilePath;
            URL = message.URL;
            FileName = message.FileName;
            Extention = message.Extention;
            DownloadSize = (message.DownloadSize == null) ? 0 : (long) message.DownloadSize;
            FileSize = message.FileSize;
            Threads = message.Threads;
            DownloaderType = message.DownloaderType;
            IsDone = (message.IsDone == null) ? false : (bool) message.IsDone;
            FolderPath = message.FolderPath;
        }

        /// <summary>
        /// 缓存文件
        /// </summary>
        public string TempFilePath;

        /// <summary>
        /// 下载URL
        /// </summary>
        public string URL;

        /// <summary>
        /// 文件名（无扩展名）
        /// </summary>
        public string FileName;

        /// <summary>
        /// 文件扩展名
        /// </summary>
        public string Extention;

        /// <summary>
        /// 已下载的字节数
        /// </summary>
        public long DownloadSize;

        /// <summary>
        /// 文件大小
        /// </summary>
        public long? FileSize;

        /// <summary>
        /// 线程信息
        /// </summary>
        public ThreadMessage Threads;

        /// <summary>
        /// 下载器类型
        /// </summary>
        public Enums.DownloaderType DownloaderType;

        /// <summary>
        /// 任务是否已经完成
        /// </summary>
        public bool IsDone;

        /// <summary>
        /// 下载文件夹地址
        /// </summary>
        public string FolderPath;
    }

    public class NullableAttributesDownloaderMessage
    {
        public string TempFilePath;

        public string URL;

        public string FileName;

        public string Extention;

        public string FolderPath;

        public long? DownloadSize;

        public long? FileSize;

        public ThreadMessage Threads;

        public Enums.DownloaderType DownloaderType;

        public bool? IsDone;
    }
}
