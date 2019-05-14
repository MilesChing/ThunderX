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
using Windows.Storage;

namespace TX.Downloaders
{
    class HttpDownloader : AbstractDownloader
    {
        //locks
        private object threadLock = new object();

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
                Message.Threads.ArrangeThreads((long)Message.FileSize, settings.Threads <= 0 ? StorageTools.Settings.ThreadNumber : ((int)settings.Threads));
                //申请临时文件
                Message.TempFilePath = settings.FilePath;
                State = DownloadState.Prepared;
            }
            catch (Exception e) { HandleError(e, CurrentOperationCode); }
        }

        public override void Dispose()
        {
            if (State == DownloadState.Disposed) return;
            DisposeThreads();
            speedHelper.IsEnabled = false;
            speedHelper.Dispose();
            speedHelper = null;
            StartDisposeTemporaryFile();
            State = DownloadState.Disposed;
        }

        public override void Pause()
        {
            if (State != DownloadState.Downloading) return;
            CurrentOperationCode++;
            speedHelper.IsEnabled = false;
            State = DownloadState.Pause;
        }

        public override void Refresh()
        {
            AutoRefresh();
            //由外部调用（如用户点击重试按钮）的重试操作
            //将当前出错次数重置
            retryCount = 0;
        }

        public override void Start()
        {
            if (State != DownloadState.Prepared
                && State != DownloadState.Pause) return;
            State = DownloadState.Downloading;
            speedHelper.IsEnabled = true;
            Task.Run(async () => { await SetThreadsAsync(); });
        }

        public override bool NeedTemporaryFilePath { get { return true; } }

        public override void SetDownloaderFromBreakpoint(DownloaderMessage mes)
        {
            if (State != DownloadState.Uninitialized) return;
            //触发事件指示控件加载已完成
            Message = mes;
            speedHelper.CurrentValue = mes.DownloadSize;
            State = DownloadState.Prepared;
        }

        public override DownloaderType Type { get { return DownloaderType.HttpDownloader; } }

        /// <summary>
        /// 根据Message中的线程信息设置线程（直接开始），用于开始和继续下载
        /// 必须设置Message.Threads，必须设置Message.TempFile
        /// </summary>
        private async Task SetThreadsAsync()
        {
            int startCode = CurrentOperationCode;   //记录当前操作码，保证建立的线程均属于统一操作码

            if (Message.Threads.ThreadNum <= 0)
                throw new Exception("线程大小未计算");

            //枚举每个线程，由Message中的信息设置线程
            try
            {
                for (int threadIndex = 0; threadIndex < Message.Threads.ThreadNum; threadIndex++)
                {
                    if (startCode != CurrentOperationCode) return;  //已经不是这个操作码了，继续开线程没有意义

                    long _offset = Message.Threads.ThreadOffset[threadIndex];
                    long _size = 0;
                    long _targetSize = Message.Threads.ThreadTargetSize[threadIndex];

                    lock (Message.Threads){ _size = Message.Threads.ThreadSize[threadIndex]; }
                    
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
                    StartNewDownloadThread(source, sink,
                        _targetSize - _size,
                        threadIndex,
                        startCode);

                    Debug.WriteLine("线程 " + threadIndex + " 已开始");
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
        /// <param name="operationCode">操作码，用于确定线程是否处于受控状态</param>
        private void StartNewDownloadThread(Stream downloadStream, FileStream fileStream,
            long targetSize, int threadIndex, int operationCode)
        {
            Task.Factory.StartNew(async (arg) =>
            {
                Tuple<Stream, FileStream, long, int, int> args = (Tuple<Stream, FileStream, long, int, int>)arg;
                Stream _downloadStream = args.Item1;
                FileStream _fileStream = args.Item2;
                long _targetSize = args.Item3;
                int _operationCode = args.Item5;
                int _threadIndex = args.Item4;
                long remain = _targetSize;

                //下载数据缓存数组
                byte[] responseBytes = new byte[100000];
                //剩余字节为0时停止下载

                while (remain > 0 && State == DownloadState.Downloading && _operationCode == CurrentOperationCode)
                {
                    int pieceLength = 0;
                    try
                    {
                        //下载数据
                        pieceLength = _downloadStream.Read(responseBytes, 0, (int)(Math.Min(responseBytes.Length, remain)));
                        //写入文件
                        _fileStream.Write(responseBytes, 0, pieceLength);
                    }
                    catch (Exception e)
                    {
                        if (_downloadStream != null) _downloadStream.Dispose();
                        if (_fileStream != null) _fileStream.Dispose();
                        HandleError(e, _operationCode);
                        return;
                    }
                    remain -= pieceLength;

                    lock (Message.Threads) { Message.Threads.ThreadSize[_threadIndex] += pieceLength; }
                    lock (Message) Message.DownloadSize += pieceLength;
                    lock (speedHelper) { speedHelper.CurrentValue += pieceLength; }
                }

                //释放资源
                if (_downloadStream != null) _downloadStream.Dispose();
                if (_fileStream != null) _fileStream.Dispose();
                GC.Collect();

                if (remain <= 0)
                {
                    Debug.WriteLine("线程 " + _threadIndex + " 已完成");
                    await CheckIsDownloadDoneAsync(_operationCode);
                }
            }, new Tuple<Stream, FileStream, long, int, int>(downloadStream, fileStream, targetSize, threadIndex, operationCode));
        }

        private async Task CheckIsDownloadDoneAsync(int operationCode)
        {
            lock (threadLock)
            {
                if (operationCode != CurrentOperationCode || State != DownloadState.Downloading) return;
                if (Message.DownloadSize >= Message.FileSize)
                {
                    State = DownloadState.Done;
                    DisposeThreads();
                }
                else return;
            }
            //进入这里没有return表示已经完成
            try
            {
                GC.Collect();

                string path = StorageTools.Settings.DownloadFolderPath;
                StorageFile file = await StorageFile.GetFileFromPathAsync(Message.TempFilePath);
                await file.MoveAsync(await StorageFolder.GetFolderFromPathAsync(StorageTools.Settings.DownloadFolderPath), Message.FileName + Message.Extention, NameCollisionOption.GenerateUniqueName);
                //播放一个通知
                Toasts.ToastManager.ShowDownloadCompleteToastAsync(Strings.AppResources.GetString("DownloadCompleted"), Message.FileName + " - " +
                    Converters.StringConverter.GetPrintSize((long)Message.FileSize), file.Path);
                //触发事件
                DownloadComplete?.Invoke(Message);
            }
            catch (Exception e)
            {
                //若用户把下载文件夹设置在奇怪的地方，这里会导致无法访问，触发异常
                Debug.WriteLine(e.ToString());
                HandleError(e, CurrentOperationCode);
            }
        }

        private void HandleError(Exception e, int operationCode)
        {
            lock (threadLock)
            {
                if (operationCode != CurrentOperationCode || State == DownloadState.Error) return;

                if (State == DownloadState.Downloading && retryCount < MaximumRetries)
                {//自动重试，不烦用户
                    retryCount++;
                    Debug.WriteLine("正在进行第" + retryCount + "次重试...");
                    AutoRefresh();
                    return;
                }
                else
                {
                    State = DownloadState.Error;
                    DisposeThreads();
                }
            }
            //上面的else执行后没有return，会执行这里（事件回调内含Async不应在lock块中？）
            DownloadError?.Invoke(e);
        }
        private uint retryCount = 0;

        private void DisposeThreads()
        {
            CurrentOperationCode++;
        }

        private void StartDisposeTemporaryFile()
        {
            Task.Run(async () =>
            {
                try
                {
                    StorageFile temp = await StorageFile.GetFileFromPathAsync(Message.TempFilePath);
                    await temp.DeleteAsync();
                    Debug.WriteLine("临时文件删除成功");
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                }
            });
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
