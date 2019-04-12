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
            Threads = new ThreadMessage();
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
    }
}
