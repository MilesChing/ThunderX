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
                _prog_.AverageSpeed = speedHelper.AverageSpeed;
                _prog_.CurrentValue = speedHelper.CurrentValue;
                _prog_.Speed = speedHelper.Speed;
                _prog_.TargetValue = Message.FileSize;
                
                DownloadProgressChanged(_prog_);
            };
        }

        public override void SetDownloader(InitializeMessage iMessage)
        {
            if (State != DownloadState.Uninitialized) return;

            try
            {
                Message = new DownloaderMessage();
                Message.DownloaderType = Type;
                //设置文件信息
                Message.FileSize = iMessage.Size;
                Message.URL = iMessage.Url;
                Message.FileName = Path.GetFileNameWithoutExtension(iMessage.FileName);
                Message.Extention = Path.GetExtension(iMessage.FileName);
                //安排线程
                Message.Threads.ArrangeThreads((long)Message.FileSize, iMessage.Threads <= 0 ? StorageTools.Settings.ThreadNumber : iMessage.Threads);
                //申请临时文件
                Message.TempFilePath = iMessage.FilePath;
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
            if (State != DownloadState.Pause
                && State != DownloadState.Downloading
                && State != DownloadState.Error) return;
            DisposeThreads();
            State = DownloadState.Prepared;
            Start();
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
            if (Message.Threads.ThreadNum <= 0)
                throw new Exception("线程大小未计算");

            //枚举每个线程，由Message中的信息设置线程
            try
            {
                for (int threadIndex = 0; threadIndex < Message.Threads.ThreadNum; threadIndex++)
                {
                    ThreadMessage thm = Message.Threads;
                    //线程是否已完成
                    if (thm.ThreadSize[threadIndex] == thm.ThreadTargetSize[threadIndex])
                        continue;
                    //每个线程建立单独的流，在线程结束时由线程负责释放
                    //获取网络流
                    Stream source = await NetWork.HttpNetWorkMethods.GetResponseStreamAsync(Message.URL,
                        thm.ThreadOffset[threadIndex] + thm.ThreadSize[threadIndex],
                        thm.ThreadOffset[threadIndex] + thm.ThreadTargetSize[threadIndex]);

                    //获取文件流
                    FileStream sink = new FileStream(Message.TempFilePath,
                        FileMode.Open,
                        FileAccess.Write,
                        FileShare.ReadWrite);

                    //设置文件流的头
                    sink.Position = thm.ThreadOffset[threadIndex] + thm.ThreadSize[threadIndex];

                    //获取线程Task
                    Task thread = GetDownloadThread(source, sink,
                        thm.ThreadTargetSize[threadIndex] - thm.ThreadSize[threadIndex],
                        threadIndex,
                        CurrentOperationCode);

                    thread.Start();
                    Debug.WriteLine("线程 " + threadIndex + " 已开始");
                }
            }
            catch (Exception e) { HandleError(e, CurrentOperationCode); }
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
        /// 获取下载线程（Task）
        /// </summary>
        /// <param name="downloadStream">网络数据流，线程会从流的开始读threadSize个字节</param>
        /// <param name="fileStream">文件流，线程会把targetSize个字节送入流的开始</param>
        /// <param name="targetSize">目标大小</param>
        /// <param name="threadIndex">线程编号，用于更新Message中的Threads信息</param>
        /// <param name="operationCode">操作码，用于确定线程是否处于受控状态</param>
        private Task GetDownloadThread(Stream downloadStream, FileStream fileStream,
            long targetSize, int threadIndex, int operationCode)
        {
            Task t = new Task(async () =>
            {
                int code = operationCode;
                long remain = targetSize;
                //下载数据缓存数组
                byte[] responseBytes = new byte[100000];
                //剩余字节为0时停止下载
                try
                {
                    while (remain > 0 && State == DownloadState.Downloading && code == operationCode)
                    {
                        //下载数据
                        //当remain太小时不要按照responseBytes的长度申请，避免产生多余数据
                        int pieceLength = downloadStream.Read(responseBytes, 0, (int)(Math.Min(responseBytes.Length, remain)));
                        //写入文件
                        fileStream.Write(responseBytes, 0, pieceLength);
                        remain -= pieceLength;

                        lock (this)
                        {//更新Message中的各种信息，锁定在本线程
                            Message.Threads.ThreadSize[threadIndex] += pieceLength;
                            Message.DownloadSize += pieceLength;
                            speedHelper.CurrentValue += pieceLength;
                        }
                    }
                }
                catch (Exception e)
                {
                    if (downloadStream != null) downloadStream.Dispose();
                    if (fileStream != null) fileStream.Dispose();
                    HandleError(e, code);
                    return;
                }

                //释放资源
                if (downloadStream != null) downloadStream.Dispose();
                if (fileStream != null) fileStream.Dispose();
                if (remain <= 0)
                {
                    Debug.WriteLine("线程 " + threadIndex + " 已完成");
                    await CheckIsDownloadDoneAsync(code);
                }
            });

            return t;
        }

        /// <summary>
        /// 检查下载是否已完成
        /// </summary>
        private async Task CheckIsDownloadDoneAsync(int operationCode)
        {
            lock (this)
            {
                if (operationCode != CurrentOperationCode || State != DownloadState.Downloading) return;
            }

            if (Message.DownloadSize >= Message.FileSize)
            {
                State = DownloadState.Done;
                DisposeThreads();
            }
            else return;

            try
            {
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


        /// <summary>
        /// 内部发生错误时调用的处理函数，负责状态的维护和将异常信息传递出去
        /// </summary>
        private void HandleError(Exception e, int operationCode)
        {
            lock (this)
            {
                if (operationCode != CurrentOperationCode || State == DownloadState.Error) return;
            }

            if (State == DownloadState.Downloading && retryCount < MaxiMaximumRetries)
            {//自动重试，不烦用户
                retryCount++;
                Debug.WriteLine("正在进行第" + retryCount + "次重试...");
                Refresh();
            }
            else
            {
                State = DownloadState.Error;
                DisposeThreads();
                DownloadError?.Invoke(e);
            }
        }
        private uint retryCount = 0;

        /// <summary>
        /// 释放线程资源
        /// </summary>
        private void DisposeThreads()
        {
            CurrentOperationCode++;
        }

        /// <summary>
        /// 删除临时文件
        /// </summary>
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
    }
}
