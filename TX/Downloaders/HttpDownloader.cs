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
using Windows.Storage;

namespace TX.Downloaders
{
    class HttpDownloader : IDownloader
    {
        /// <summary>
        /// 空构造函数
        /// </summary>
        public HttpDownloader()
        {
            state = DownloadState.Pending;
            TemporaryStartTime = new DateTime();
            state = DownloadState.Pending;
        }

        /// <summary>
        /// 事件，下载进度变化，第一个参数为已下载字节数，第二个参数为总字节数
        /// </summary>
        public event Action<long, long> OnDownloadProgressChange;

        /// <summary>
        /// 事件，指示下载已结束
        /// </summary>
        public event Action<DownloaderMessage> DownloadComplete;

        /// <summary>
        /// 事件，指示下载发生错误
        /// </summary>
        public event Action<Exception> DownloadError;

        /// <summary>
        /// 事件，指示外部控件信息已完成填充，可以开始
        /// </summary>
        public event Action<DownloaderMessage> MessageComplete;

        /// <summary>
        /// 用于告知下载器当前状态，如速度等等
        /// </summary>
        public event Action<string> Log;

        /// <summary>
        /// 下载器信息
        /// </summary>
        private DownloaderMessage message;

        /// <summary>
        /// 当前下载状态
        /// </summary>
        private DownloadState state;

        /// <summary>
        /// 临时的开始时间，在下载进行中时有效
        /// 指示了最近一次开始下载（继续下载）的时间记录
        /// </summary>
        private DateTime TemporaryStartTime;

        /// <summary>
        /// 根据message中的线程信息设置线程（直接开始），用于开始和继续下载
        /// 必须设置message.Threads，必须设置message.TempFile
        /// </summary>
        private async Task SetThreadsAsync()
        {
            if (message.Threads.ThreadNum <= 0)
                throw new Exception("线程大小未计算");

            //枚举每个线程，由message中的信息设置线程
            try
            {
                for (int threadIndex = 0; threadIndex < message.Threads.ThreadNum; threadIndex++)
                {
                    ThreadMessage thm = message.Threads;
                    //线程是否已完成
                    if (thm.ThreadSize[threadIndex] == thm.ThreadTargetSize[threadIndex])
                        continue;
                    //每个线程建立单独的流，在线程结束时由线程负责释放
                    //获取网络流
                    Stream source = await NetWork.HttpNetWorkMethods.GetResponseStreamAsync(message.URL,
                        thm.ThreadOffset[threadIndex] + thm.ThreadSize[threadIndex],
                        thm.ThreadOffset[threadIndex] + thm.ThreadTargetSize[threadIndex]);

                    //获取文件流
                    FileStream sink = new FileStream(message.TempFilePath, 
                        FileMode.Open, 
                        FileAccess.Write, 
                        FileShare.ReadWrite);

                    //设置文件流的头
                    sink.Position = thm.ThreadOffset[threadIndex] + thm.ThreadSize[threadIndex];

                    //获取线程Task
                    Task thread = GetDownloadThread(source, sink,
                        thm.ThreadTargetSize[threadIndex] - thm.ThreadSize[threadIndex], 
                        threadIndex,
                        currentOperationCode);

                    thread.Start();
                    Debug.WriteLine("线程 " + threadIndex + " 已开始");
                }
            }
            catch (Exception e) { ErrorHandler(e); }
        }

        /// <summary>
        /// 当前正在工作的线程组编号，当线程检测到currentOperationCode变化时将自动停止工作
        /// 自动对1024取模
        /// </summary>
        private long curopt = 0;
        private long currentOperationCode
        {
            get { return curopt; }
            set { curopt = value % 1024; }
        }

        /// <summary>
        /// 获取下载线程（Task）
        /// </summary>
        /// <param name="downloadStream">网络数据流，线程会从流的开始读threadSize个字节</param>
        /// <param name="fileStream">文件流，线程会把targetSize个字节送入流的开始</param>
        /// <param name="targetSize">目标大小</param>
        /// <param name="threadIndex">线程编号，用于更新message中的Threads信息</param>
        private Task GetDownloadThread(Stream downloadStream, FileStream fileStream, 
            long targetSize, int threadIndex, long operationCode)
        {
            Task t = new Task(async () =>
            {
                long remain = targetSize;
                //下载数据缓存数组
                byte[] responseBytes = new byte[100000];
                //剩余字节为0时停止下载
                try
                {
                    while (remain > 0)
                    {
                        //下载数据
                        //当remain太小时不要按照responseBytes的长度申请，避免产生多余数据
                        int pieceLength = downloadStream.Read(responseBytes, 0, (int)(Math.Min(responseBytes.Length, remain)));
                        
                        if (currentOperationCode != operationCode)
                        {
                            Debug.WriteLine("线程 " + threadIndex + " 已退出：检测到operation code变化");
                            break;
                        }
                        
                        //写入文件
                        fileStream.Write(responseBytes, 0, pieceLength);
                        remain -= pieceLength;

                        lock (this)
                        {//更新message中的各种信息，锁定在本线程
                            message.Threads.ThreadSize[threadIndex] += pieceLength;
                            message.DownloadSize += pieceLength;
                        }

                        //下载进度变化
                        ProgressChanged(pieceLength);
                    }
                }
                catch (Exception e)
                {
                    if (downloadStream != null) downloadStream.Dispose();
                    if (fileStream != null) fileStream.Dispose();
                    ErrorHandler(e);
                    return;
                }
                
                //释放资源
                if (downloadStream != null) downloadStream.Dispose();
                if (fileStream != null) fileStream.Dispose();
                if (remain <= 0)
                {
                    Debug.WriteLine("线程 " + threadIndex + " 已完成");
                    await CheckIsDownloadDoneAsync();
                }
            });

            return t;
        }

        /// <summary>
        /// 检查下载是否已完成
        /// </summary>
        private async Task CheckIsDownloadDoneAsync()
        {
            //先检查是否已经判断过了，防止重复触发事件
            lock (this)
            {
                if (state == DownloadState.Done)
                    return;
                if (message.DownloadSize >= message.FileSize)
                    state = DownloadState.Done;
                else return;
            }

            try
            {
                string path = StorageTools.Settings.DownloadFolderPath;
                StorageFile file = await StorageFile.GetFileFromPathAsync(message.TempFilePath);
                await file.MoveAsync(await StorageFolder.GetFolderFromPathAsync(StorageTools.Settings.DownloadFolderPath), message.FileName + message.TypeName, NameCollisionOption.GenerateUniqueName);
                //播放一个通知
                Toasts.ToastManager.ShowDownloadCompleteToastAsync(Strings.AppResources.GetString("DownloadCompleted"), message.FileName + ": " +
                    Converters.StringConverters.GetPrintSize(message.FileSize), file.Path);
                //触发事件
                DownloadComplete(message);
            }
            catch(Exception e)
            {
                //若用户把下载文件夹设置在奇怪的地方，这里会导致无法访问，触发异常
                Debug.WriteLine(e.ToString());
                ErrorHandler(e);
            }
            
        }

        /// <summary>
        /// 内部发生错误时调用的处理函数，负责状态的维护和将异常信息传递出去
        /// </summary>
        private void ErrorHandler(Exception e)
        {
            lock(this){ 
                Debug.WriteLine(e.ToString());
                if (state == DownloadState.Error) return;
                state = DownloadState.Error;
                DownloadError(e);
                Log(e.ToString());
            }
        }

        /// <summary>
        /// 使用这个链接初始化下载器
        /// </summary>
        public async Task SetDownloaderAsync(Models.InitializeMessage imessage)
        {
            try
            {
                //设置文件信息
                message = await NetWork.HttpNetWorkMethods.GetMessageAsync(imessage.Url);

                if (imessage.Rename != null) message.FileName = imessage.Rename;
                //安排线程
                message.Threads.ArrangeThreads(message.FileSize, imessage.Threads <= 0 ? StorageTools.Settings.ThreadNumber : imessage.Threads);
                //申请临时文件
                message.TempFilePath = await StorageTools.StorageManager.GetTemporaryFileAsync();
                //触发事件指示控件加载已完成
                MessageComplete(message);
                Log(Strings.AppResources.GetString("DownloaderDone"));
                state = DownloadState.Prepared;
            }
            catch (Exception e) { ErrorHandler(e); }
        }

        /// <summary>
        /// 释放线程资源
        /// </summary>
        public void DisposeThreads()
        {
            currentOperationCode++;
        }
        
        /// <summary>
        /// 删除临时文件
        /// </summary>
        public void StartDisposeTemporaryFile()
        {
            Task.Run(async () =>
            {
                try
                {
                    StorageFile temp = await StorageFile.GetFileFromPathAsync(message.TempFilePath);
                    await temp.DeleteAsync();
                    Debug.WriteLine("临时文件删除成功");
                }
                catch(Exception e)
                {
                    Debug.WriteLine(e.ToString());
                }
            });
        }

        /// <summary>
        /// 释放下载器资源
        /// </summary>
        public void Dispose()
        {
            Debug.WriteLine("开始释放资源");
            DisposeThreads();
            StartDisposeTemporaryFile();
        }

        /// <summary>
        /// 获取下载器信息
        /// </summary>
        public DownloaderMessage GetDownloaderMessage()
        {
            return message;
        }

        /// <summary>
        /// 获取下载状态
        /// </summary>
        public DownloadState GetDownloadState()
        {
            return state;
        }

        /// <summary>
        /// 获取下载时间
        /// </summary>
        /// <param name="NowTime">查询时间</param>
        public TimeSpan GetDownloadTime(DateTime NowTime)
        {
            if (state != DownloadState.Downloading)
                return message.DownloadTime;
            //临时开始时间为空代表当前状态不是正在下载
            else return message.DownloadTime + (NowTime - TemporaryStartTime);
        }

        /// <summary>
        /// 暂停
        /// </summary>
        public void Pause()
        {
            Log(Strings.AppResources.GetString("Pause"));
            Debug.WriteLine("已暂停");
            if (state != DownloadState.Downloading) return;
            currentOperationCode++;
            message.DownloadTime += DateTime.Now - TemporaryStartTime;
            state = DownloadState.Pending;
        }

        /// <summary>
        /// 重试
        /// 在Prepared以后都可以用
        /// </summary>
        public void Refresh()
        {
            Log(Strings.AppResources.GetString("Refreshing"));
            DisposeThreads();
            state = DownloadState.Pending;
            Start();
        }

        /// <summary>
        /// 开始（或继续）下载，必须保证message.Threads有效
        /// </summary>
        public void Start()
        {
            if (state == DownloadState.Downloading) return;
            TemporaryStartTime = DateTime.Now;
            state = DownloadState.Downloading;
            Task.Run(async () => { await SetThreadsAsync(); });
            Log(Strings.AppResources.GetString("Downloading"));
            Debug.WriteLine("任务开始");
        }

        /// <summary>
        /// 使用Message重置下载器，用于恢复任务
        /// </summary>
        /// <param name="message"></param>
        public void SetDownloader(DownloaderMessage mes)
        {
            //触发事件指示控件加载已完成
            message = mes;
            MessageComplete(message);
            state = DownloadState.Prepared;
            Log(Strings.AppResources.GetString("DownloaderDone"));
        }

        /// <summary>
        /// 集中管理各个线程的更新信息，在增量达到一定程度（10kb）更新界面
        /// </summary>
        private long progressDelta = 0;
        private void ProgressChanged(long size)
        {
            const long tick = 10240;
            lock (this)
            {
                if (state != DownloadState.Downloading || ((App)Windows.UI.Xaml.Application.Current).InBackground) return;
                progressDelta += size;
                if (progressDelta > tick)
                {
                    OnDownloadProgressChange(message.DownloadSize, message.FileSize);
                    progressDelta -= tick;
                }
            }
        }
    }
}
