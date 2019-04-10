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
using TX.NetWork;
using Windows.Storage;

namespace TX.Downloaders
{
    /// <summary>
    /// 直接封装WebClient的一个下载器，不支持断点续传
    /// 在HttpDownloader无法处理的时候所幸用系统最原始的
    /// </summary>
    class HttpSystemDownloader : AbstractDownloader
    {
        public override event Action<Progress> DownloadProgressChanged;
        public override event Action<DownloaderMessage> DownloadComplete;
        public override event Action<Exception> DownloadError;

        private WebClient client = null;
        private SpeedCalculator speedHelper;

        public override void Dispose()
        {
            if(client != null) client.Dispose();
            client = null;
            Message = null;
        }

        public override void Pause()
        {
            //临时文件统一删除，由于无法获得文件大小，每次下载都是一次性的
            //表面暂停 嘻嘻嘻嘻
            Message.TempFilePath = "";
            client.Dispose();
            client = null;
            speedHelper.Dispose();
            speedHelper = null;
            State = DownloadState.Pause;
        }

        public override void Refresh()
        {
            State = DownloadState.Downloading;
            Pause();
            Start();
        }

        public override void SetDownloaderFromBreakpoint(DownloaderMessage Message)
        {
            this.Message = Message;
            State = DownloadState.Prepared;
        }

        public override void SetDownloader(InitializeMessage iMessage)
        {
            Message = new DownloaderMessage();
            Message.DownloadSize = 0;
            Message.FileName = Path.GetFileNameWithoutExtension(iMessage.FileName);
            Message.Extention = Path.GetExtension(iMessage.FileName);
            Message.FileSize = null;
            Message.TempFilePath = Windows.Storage.ApplicationData.Current.TemporaryFolder.Path + @"\" + iMessage.FileName;
            Message.URL = iMessage.Url;
            State = DownloadState.Prepared;
            return;
        }

        public override void Start()
        {
            State = DownloadState.Downloading;
            //每次重新开始
            
            Message.TempFilePath = ApplicationData.Current.LocalCacheFolder.Path + @"\" + StorageTools.StorageManager.GetTemporaryName() + ".tmp";
            speedHelper = new SpeedCalculator();
            speedHelper.IsEnabled = true;

            client = new WebClient();
            client.Credentials = CredentialCache.DefaultCredentials;
            client.DownloadFileCompleted += Client_DownloadFileCompleted;
            client.DownloadProgressChanged += (o,e) => 
            {
                speedHelper.CurrentValue = e.BytesReceived;
                _prog_.Speed = speedHelper.Speed;
                _prog_.AverageSpeed = speedHelper.AverageSpeed;
                _prog_.TargetValue = null;
                _prog_.ProgressValue = speedHelper.CurrentValue;
                DownloadProgressChanged?.Invoke(_prog_);
            };
            client.DownloadFileAsync(new Uri(Message.URL), Message.TempFilePath);
        }

        /// <summary>
        /// 下载完成事件的回调函数
        /// </summary>
        private async void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            try
            {
                string path = StorageTools.Settings.DownloadFolderPath;
                StorageFile file = await StorageFile.GetFileFromPathAsync(Message.TempFilePath);
                await file.MoveAsync(await StorageFolder.GetFolderFromPathAsync(StorageTools.Settings.DownloadFolderPath), Message.FileName + Message.Extention, NameCollisionOption.GenerateUniqueName);
                //播放一个通知
                Toasts.ToastManager.ShowDownloadCompleteToastAsync(Strings.AppResources.GetString("DownloadCompleted"), Message.FileName + ": " +
                    Converters.StringConverter.GetPrintSize(_prog_.ProgressValue), file.Path);
                //触发事件
                DownloadComplete?.Invoke(Message);
            }
            catch (Exception ex)
            {
                //若用户把下载文件夹设置在奇怪的地方，这里会导致无法访问，触发异常
                Debug.WriteLine(ex.ToString());
                DownloadError?.Invoke(ex);
            }
        }

        private Progress _prog_ = new Progress();
    }
}
