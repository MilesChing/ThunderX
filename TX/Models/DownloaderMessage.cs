using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            TypeName = null;
            DownloadSize = 0;
            FileSize = 0;
            Threads = new ThreadMessage();
            DownloadTime = new TimeSpan(0, 0, 0);
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
        public string TypeName;

        /// <summary>
        /// 已下载的字节数
        /// </summary>
        public long DownloadSize;

        /// <summary>
        /// 文件大小
        /// </summary>
        public long FileSize;

        /// <summary>
        /// 线程信息
        /// </summary>
        public ThreadMessage Threads;

        /// <summary>
        /// 下载时间的总长
        /// </summary>
        public TimeSpan DownloadTime;
    }
}
