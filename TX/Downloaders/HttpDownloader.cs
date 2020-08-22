using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TX.Enums;
using TX.Models;
using TX.NetWork;
using TX.StorageTools;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace TX.Downloaders
{
    class HttpDownloader : AbstractDownloader
    {
        //locks
        private object threadLock = new object();
        private object downloadSizeLock = new object();
        private object speedHelperLock = new object();

        private readonly Progress _prog_ = new Progress();
        private SpeedCalculator speedHelper = new SpeedCalculator();
        public override event Action<Progress> DownloadProgressChanged;
        public override event Action<DownloaderMessage> DownloadComplete;
        public override event Action<Exception> DownloadError;

        public HttpDownloader()
        {
            speedHelper.Updated += (h) =>
            {
                if (State != DownloadState.Downloading) return;
                lock (speedHelper)
                {
                    _prog_.AverageSpeed = speedHelper.AverageSpeed;
                    _prog_.CurrentValue = speedHelper.CurrentValue;
                    _prog_.Speed = speedHelper.Speed;
                    _prog_.TargetValue = Message.FileSize;
                }
                DownloadProgressChanged(_prog_);
            };
        }

        public override void SetDownloader(DownloaderSettings settings)
        {
            if (State != DownloadState.Uninitialized) return;
            try
            {
                Message = new DownloaderMessage();
                Message.DownloaderType = Type;
                //设置文件信息
                Message.FileSize = settings.Size;
                Message.URL = settings.Url;
                Message.FileName = Path.GetFileNameWithoutExtension(settings.FileName);
                Message.Extention = Path.GetExtension(settings.FileName);
                //安排线程
                Message.Threads.ArrangeThreads((long)Message.FileSize, settings.Threads <= 0 ? 
                    StorageTools.Settings.Instance.ThreadNumber : ((int)settings.Threads));
                //申请临时文件
                Message.TempFilePath = settings.FilePath;
                Message.FolderToken = settings.FolderToken;
                State = DownloadState.Prepared;
            }
            catch (Exception e) { HandleError(e, CurrentOperationCode); }
        }

        public override void Dispose()
        {
            if (State == DownloadState.Disposed) return;
            DisposeThreads();
            DisposeSpeedHelper();
            State = DownloadState.Disposed;
        }

        public override void Pause()
        {
            if (State != DownloadState.Downloading) return;
            DisposeThreads();
            speedHelper.IsEnabled = false;
            State = DownloadState.Pause;
        }

        public override void Refresh()
        {
            retryCount = 0;
            AutoRefresh();
        }

        public override void Start()
        {
            if (State != DownloadState.Prepared
                && State != DownloadState.Pause) return;
            State = DownloadState.Downloading;
            speedHelper.IsEnabled = true;
            _ = SetThreadsAsync(CurrentOperationCode);
            Debug.WriteLine("Start end");
        }

        public override bool NeedTemporaryFilePath { get { return true; } }

        public override void SetDownloaderFromBreakpoint(DownloaderMessage mes)
        {
            if (State != DownloadState.Uninitialized) return;
            //触发事件指示控件加载已完成
            Message = mes;
            speedHelper.LastValue = speedHelper.CurrentValue = mes.DownloadSize;
            State = DownloadState.Prepared;
            if (Message.IsDone)
            {
                State = DownloadState.Done;
                DisposeSpeedHelper();
            }
        }

        public override DownloaderType Type { get { return DownloaderType.HttpDownloader; } }

        /// <summary>
        /// 根据Message中的线程信息设置线程（直接开始），用于开始和继续下载
        /// 必须设置Message.Threads，必须设置Message.TempFile
        /// </summary>
        private async Task SetThreadsAsync(int startCode)
        {
            if (Message.Threads.ThreadNum <= 0)
                throw new Exception("任务 " + Message.URL + " 线程大小未计算");

            //枚举每个线程，由Message中的信息设置线程
            try
            {
                for (int threadIndex = 0; threadIndex < Message.Threads.ThreadNum; threadIndex++)
                {
                    if (startCode != CurrentOperationCode) return;  //已经不是这个操作码了，继续开线程没有意义

                    long _offset = Message.Threads.ThreadOffset[threadIndex];
                    long _size = 0;
                    long _targetSize = Message.Threads.ThreadTargetSize[threadIndex];

                    lock (Message.Threads) { _size = Message.Threads.ThreadSize[threadIndex]; }

                    //线程是否已完成
                    if (_size == _targetSize) continue;
                    //每个线程建立单独的流，在线程结束时由线程负责释放
                    //获取网络流
                    Stream source = await NetWork.HttpNetWorkMethods.GetResponseStreamAsync(Message.URL,
                        _offset + _size,
                        _offset + _targetSize);

                    //获取文件流
                    FileStream sink = new FileStream(Message.TempFilePath,
                        FileMode.Open,
                        FileAccess.Write,
                        FileShare.Write);

                    //设置文件流的头
                    sink.Position = _offset + _size;

                    //启动下载线程
                    StartDownload(source, sink, _targetSize - _size, threadIndex, startCode); 
                }
            }
            catch (Exception e) { HandleError(e, startCode); }
        }

        /// <summary>
        /// 当前正在工作的线程组编号，
        /// 用于指示线程听从调度，
        /// 当线程检测到currentOperationCode变化时将自动停止工作。
        /// </summary>
        private int CurrentOperationCode
        {
            get { return __OptCode; }
            set { __OptCode = value % 1025; }
        }
        private int __OptCode = 0;

        /// <summary>
        /// 开始下载线程
        /// </summary>
        /// <param name="downloadStream">网络数据流，线程会从流的开始读threadSize个字节</param>
        /// <param name="fileStream">文件流，线程会把targetSize个字节送入流的开始</param>
        /// <param name="targetSize">目标大小</param>
        /// <param name="threadIndex">线程编号，用于更新Message中的Threads信息</param>
        /// <param name="operationCode">操作码，用于确定线程是否处于当前操作批次</param>
        private void StartDownload(Stream downloadStream, FileStream fileStream,
            long targetSize, int threadIndex, int operationCode)
        {
            Task.Run(() =>
            {
                Debug.WriteLine(threadIndex + " of " + operationCode + " Start");
                long remain = targetSize;
                int maximumBufferSize = Settings.Instance.MaximumDynamicBufferSize * 1024;

                //下载数据缓存数组，初始为64kB
                byte[] responseBytes = new byte[64 * 1024];
                int pieceLength = 0;
                //剩余字节为0时停止下载
                while (remain > 0 && State == DownloadState.Downloading && operationCode == CurrentOperationCode)
                {
                    try
                    {
                        //下载数据
                        pieceLength = downloadStream.Read(responseBytes, 0, (int)(Math.Min(responseBytes.Length, remain)));
                        //写入文件
                        fileStream.Write(responseBytes, 0, pieceLength);
                    }
                    catch (Exception e)
                    {
                        if (downloadStream != null) downloadStream.Dispose();
                        if (fileStream != null) fileStream.Dispose();
                        HandleError(e, operationCode);
                        GC.Collect();
                        return;
                    }

                    //动态调整缓冲区大小
                    if (pieceLength == responseBytes.Length && responseBytes.Length < maximumBufferSize)
                        responseBytes = new byte[2 * responseBytes.Length];

                    remain -= pieceLength;

                    Message.Threads.ThreadSize[threadIndex] += pieceLength;
                    lock (downloadSizeLock) Message.DownloadSize += pieceLength;
                    lock (speedHelperLock) { 
                        if (speedHelper != null)
                            speedHelper.CurrentValue += pieceLength; 
                    }
                }

                //释放资源
                if (downloadStream != null) downloadStream.Dispose();
                if (fileStream != null) fileStream.Dispose();

                if (remain <= 0) _ = CheckIsDownloadDoneAsync(operationCode);
                Debug.WriteLine(threadIndex + " of " + operationCode + " End");
            });
        }

        private async Task CheckIsDownloadDoneAsync(int operationCode)
        {
            if (operationCode != CurrentOperationCode || State != DownloadState.Downloading) return;

            lock (threadLock)
            {
                if (operationCode != CurrentOperationCode || State != DownloadState.Downloading) return;
                if (Message.DownloadSize >= Message.FileSize)
                {
                    DisposeThreads();
                }
                else return;
            }
            //进入这里没有return表示已经完成

            GC.Collect();

            StorageFile file = await StorageFile.GetFileFromPathAsync(Message.TempFilePath);
            StorageFolder folder = await StorageManager.TryGetFolderAsync(Message.FolderToken);

            if(folder == null)
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

            try
            {
                await file.MoveAsync(folder, Message.FileName + Message.Extention, NameCollisionOption.GenerateUniqueName);
            }
            catch (Exception e)
            {
                HandleError(e, operationCode);
                return;
            }

            Message.IsDone = true;

            DisposeSpeedHelper();

            //触发事件
            State = DownloadState.Done;
            DownloadComplete?.Invoke(Message);
        }

        private void HandleError(Exception e, int operationCode)
        {
            if (operationCode != CurrentOperationCode || State == DownloadState.Error) return;

            lock (threadLock)
            {
                if (operationCode != CurrentOperationCode || State == DownloadState.Error) return;

                if (State == DownloadState.Downloading && retryCount < MaximumRetries)
                {//自动重试，不烦用户
                    retryCount++;
                    AutoRefresh();
                    return;
                }
                else
                {
                    State = DownloadState.Error;
                    DisposeThreads();
                }
            }
            //上面的else执行后没有return，会执行这里
            DownloadError?.Invoke(e);
        }
        private uint retryCount = 0;

        private void DisposeThreads()
        {
            Debug.WriteLine("threads disposed");
            CurrentOperationCode++;
        }

        private void DisposeSpeedHelper()
        {
            if (speedHelper == null) return;
            lock (speedHelperLock)
            {
                speedHelper.IsEnabled = false;
                speedHelper.Dispose();
                speedHelper = null;
            }
        }

        private void AutoRefresh()
        {
            if (State != DownloadState.Pause
                && State != DownloadState.Downloading
                && State != DownloadState.Error) return;
            DisposeThreads();
            State = DownloadState.Prepared;
            Start();
        }
    }
}
