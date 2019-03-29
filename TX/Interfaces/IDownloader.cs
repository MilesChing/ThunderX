using System;
using System.Threading.Tasks;
using TX.Models;

namespace TX.Downloaders
{
    public interface IDownloader:System.IDisposable
    {
        /// <summary>
        /// 事件，下载进度变化，第一个参数为已下载字节数，第二个参数为总字节数
        /// </summary>
        event Action<long, long> OnDownloadProgressChange;

        /// <summary>
        /// 事件，指示下载已结束
        /// </summary>
        event Action<DownloaderMessage> DownloadComplete;

        /// <summary>
        /// 事件，指示下载发生错误
        /// </summary>
        event Action<Exception> DownloadError;

        /// <summary>
        /// 事件，指示外部控件信息已完成填充，可以开始
        /// </summary>
        event Action<DownloaderMessage> MessageComplete;

        /// <summary>
        /// 用于告知下载器当前状态，例如速度等等
        /// </summary>
        event Action<string> Log; 

        /// <summary>
        /// 暂停
        /// </summary>
        void Pause();

        /// <summary>
        /// 开始
        /// </summary>
        void Start();

        /// <summary>
        /// 重置
        /// </summary>
        void Refresh();

        /// <summary>
        /// 得到当前下载状态
        /// </summary>
        Enums.DownloadState GetDownloadState();

        /// <summary>
        /// 得到当前下载器信息
        /// </summary>
        Models.DownloaderMessage GetDownloaderMessage();

        /// <summary>
        /// 使用链接重置下载器
        /// </summary>
        Task SetDownloaderAsync(InitializeMessage imessage);

        /// <summary>
        /// 使用Message重置下载器，用于还原
        /// </summary>
        void SetDownloader(DownloaderMessage message);
    }
}
