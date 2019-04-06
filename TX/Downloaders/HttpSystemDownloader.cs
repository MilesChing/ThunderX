using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TX.Enums;
using TX.Models;
using Windows.Storage;

namespace TX.Downloaders
{
    /// <summary>
    /// 直接封装WebClient的一个下载器，不支持断点续传
    /// 在HttpDownloader无法处理的时候所幸用系统最原始的
    /// </summary>
    class HttpSystemDownloader : IDownloader
    {
        public event Action<long, long> DownloadProgressChanged;
        public event Action<DownloaderMessage> DownloadComplete;
        public event Action<Exception> DownloadError;
        public event Action<string> Log;
        public event Action<DownloadState> StateChanged;

        private WebClient client = null;
        private DownloaderMessage message = new DownloaderMessage();
        private DownloadState _state_ = DownloadState.Pending;
        private DownloadState state
        {
            get { return _state_; }
            set { _state_ = value; StateChanged?.Invoke(value); }
        }
        private DateTime TemporaryStartTime;

        public void Dispose()
        {
            if(client != null) client.Dispose();
            client = null;
            message = null;
        }

        public DownloaderMessage GetDownloaderMessage()
        {
            return message;
        }

        public DownloadState GetDownloadState()
        {
            return state;
        }

        public TimeSpan GetDownloadTime(DateTime NowTime)
        {
            if (state != DownloadState.Downloading)
                return message.DownloadTime;
            //临时开始时间为空代表当前状态不是正在下载
            else return message.DownloadTime + (NowTime - TemporaryStartTime);
        }

        public void Pause()
        {
            //临时文件统一删除，由于无法获得文件大小，每次下载都是一次性的
            //表面暂停 嘻嘻嘻嘻
            message.TempFilePath = "";
            client.Dispose();
            Log?.Invoke(Strings.AppResources.GetString("Pause"));
            client = null;
            state = DownloadState.Pause;
        }

        public void Refresh()
        {
            state = DownloadState.Downloading;
            Log?.Invoke(Strings.AppResources.GetString("Refreshing"));
            Pause();
            Start();
        }

        public void SetDownloader(DownloaderMessage message)
        {
            this.message = message;
            state = DownloadState.Prepared;
            Log?.Invoke(Strings.AppResources.GetString("DownloaderDone"));
        }

        public void SetDownloader(InitializeMessage imessage)
        {
            message.DownloadSize = 0;
            message.FileName = Path.GetFileName(imessage.FileName);
            message.TypeName = Path.GetExtension(imessage.FileName);
            message.FileSize = -1;
            message.TempFilePath = Windows.Storage.ApplicationData.Current.TemporaryFolder.Path + @"\" + imessage.FileName;
            message.URL = imessage.Url;
            state = DownloadState.Prepared;
            Log?.Invoke(Strings.AppResources.GetString("DownloaderDone"));
            return;
        }

        public void Start()
        {
            state = DownloadState.Downloading;
            Log?.Invoke(Strings.AppResources.GetString("Downloading"));
            //每次重新开始
            message.TempFilePath = StorageTools.StorageManager.GetTemporaryName();

            client = new WebClient();
            client.Credentials = CredentialCache.DefaultCredentials;
            client.DownloadFileCompleted += Client_DownloadFileCompleted;
            client.DownloadProgressChanged += Client_DownloadProgressChanged;
            client.DownloadFileAsync(new Uri(message.URL), message.TempFilePath);
        }

        private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            DownloadProgressChanged?.Invoke(e.BytesReceived, e.TotalBytesToReceive);
        }

        private async void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            try
            {
                Log?.Invoke(Strings.AppResources.GetString("DownloaderDone"));
                string path = StorageTools.Settings.DownloadFolderPath;
                StorageFile file = await StorageFile.GetFileFromPathAsync(message.TempFilePath);
                await file.MoveAsync(await StorageFolder.GetFolderFromPathAsync(StorageTools.Settings.DownloadFolderPath), message.FileName + message.TypeName, NameCollisionOption.GenerateUniqueName);
                //播放一个通知
                Toasts.ToastManager.ShowDownloadCompleteToastAsync(Strings.AppResources.GetString("DownloadCompleted"), message.FileName + ": " +
                    Converters.StringConverters.GetPrintSize(message.FileSize), file.Path);
                //触发事件
                DownloadComplete?.Invoke(message);
            }
            catch (Exception ex)
            {
                //若用户把下载文件夹设置在奇怪的地方，这里会导致无法访问，触发异常
                Debug.WriteLine(ex.ToString());
                DownloadError?.Invoke(ex);
            }
        }
    }
}
