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
using TX.StorageTools;
using Windows.Storage;
using Windows.Storage.AccessCache;

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
            if (State == DownloadState.Disposed) return;
            if(client != null) client.Dispose();
            client = null;
            Message = null;
        }

        public override void Pause()
        {
            if (State != DownloadState.Downloading) return;
            //临时文件统一删除，由于无法获得文件大小，每次下载都是一次性的
            Message.TempFilePath = "";
            if(client != null)
            {
                client.CancelAsync();
                client.Dispose();
                client = null;
            }
            
            if(speedHelper != null)
            {
                speedHelper.Dispose();
                speedHelper = null;
            }

            _prog_.AverageSpeed = _prog_.Speed = _prog_.CurrentValue = 0;
            DownloadProgressChanged(_prog_);
            State = DownloadState.Pause;
        }

        public override void Refresh()
        {
            if (State != DownloadState.Pause 
                && State != DownloadState.Downloading 
                && State != DownloadState.Error) return;
            State = DownloadState.Downloading;
            Pause();
            Start();
        }

        public override void SetDownloaderFromBreakpoint(DownloaderMessage Message)
        {
            if (State != DownloadState.Uninitialized) return;
            this.Message = Message;
            State = DownloadState.Prepared;
            if (Message.IsDone) State = DownloadState.Done;
        }

        public override void SetDownloader(DownloaderSettings settings)
        {
            if (State != DownloadState.Uninitialized) return;

            Message = new DownloaderMessage();
            Message.DownloaderType = Type;
            Message.DownloadSize = 0;
            Message.FileName = Path.GetFileNameWithoutExtension(settings.FileName);
            Message.Extention = Path.GetExtension(settings.FileName);
            Message.FileSize = null;
            Message.TempFilePath = Path.Combine(
                Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path, 
                StorageTools.StorageManager.GetTemporaryName() + settings.FileName);
            Message.URL = settings.Url;
            Message.FolderToken = settings.FolderToken;
            State = DownloadState.Prepared;
            return;
        }

        public override void Start()
        {
            if (State != DownloadState.Prepared
                && State != DownloadState.Pause) return;
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
                if (o != client) return;
                speedHelper.CurrentValue = e.BytesReceived;
                _prog_.Speed = speedHelper.Speed;
                _prog_.AverageSpeed = speedHelper.AverageSpeed;
                _prog_.TargetValue = null;
                _prog_.CurrentValue = speedHelper.CurrentValue;
                DownloadProgressChanged?.Invoke(_prog_);
            };
            client.DownloadFileAsync(new Uri(Message.URL), Message.TempFilePath);
        }

        public override bool NeedTemporaryFilePath { get { return false; } }

        public override DownloaderType Type { get { return DownloaderType.HttpSystemDownloader; } }

        /// <summary>
        /// 下载完成事件的回调函数
        /// </summary>
        private async void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (sender != client) return;
            try
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(Message.TempFilePath);
                StorageFolder folder = await StorageManager.TryGetFolderAsync(Message.FolderToken);

                if (folder == null)
                {
                    folder = await StorageManager.TryGetFolderAsync(Settings.Instance.DownloadsFolderToken);
                    Message.FolderToken = Settings.Instance.DownloadsFolderToken;
                    if (folder == null)
                    {
                        folder = ApplicationData.Current.LocalCacheFolder;
                        Message.FolderToken = StorageApplicationPermissions.FutureAccessList
                            .Add(ApplicationData.Current.LocalCacheFolder);
                    }
                    Toasts.ToastManager.ShowSimpleToast(Strings.AppResources.GetString("DownloadFolderPathIllegal"),
                        Strings.AppResources.GetString("DownloadFolderPathIllegalMessage"));
                }

                await file.MoveAsync(folder, Message.FileName + Message.Extention, NameCollisionOption.GenerateUniqueName);

                Message.DownloadSize = (long)(await file.GetBasicPropertiesAsync()).Size;

                Message.IsDone = true;
                
                //触发事件
                State = DownloadState.Done;
                DownloadComplete?.Invoke(Message);
            }
            catch (Exception ex)
            {
                //若用户把下载文件夹设置在奇怪的地方，这里会导致无法访问，触发异常
                Debug.WriteLine(ex.ToString());
                DownloadError?.Invoke(ex);
            }
        }

        private readonly Progress _prog_ = new Progress();
    }
}
