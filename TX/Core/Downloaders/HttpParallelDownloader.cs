using EnsureThat;
using Microsoft.Toolkit.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TX.Core.Interfaces;
using TX.Core.Models.Contexts;
using TX.Core.Models.Progresses;
using TX.Core.Models.Targets;
using TX.Core.Utils;
using Windows.Storage;

namespace TX.Core.Downloaders
{
    /// <summary>
    /// HttpParallelDownloader downloads file from a task with HttpRangableTarget.
    /// HttpDownloader divides data to be downloaded into segments and creates
    /// threads to transfer these segments parallelly.
    /// </summary>
    public class HttpParallelDownloader : AbstractDownloader, IPersistable
    {
        /// <summary>
        /// Construct a HttpParallelDownloader.
        /// </summary>
        /// <param name="task">Download task, must with HttpRangedTarget.</param>
        /// <param name="folderProvider">Folder provider must not be null.</param>
        /// <param name="cacheProvider">Cache provider must not be null.</param>
        /// <param name="bufferProvider">Buffer provider must not be null.</param>
        /// <param name="checkPoint">Set the downloader to start at given checkPoint.</param>
        /// <param name="threadNum">Number of threads used.</param>
        /// <param name="threadSegmentSize">Downloading task is divided as segments 
        /// before assigned to each thread. Segment size defines the approximate 
        /// size of each segment.</param>
        public HttpParallelDownloader(
            DownloadTask task,
            IFolderProvider folderProvider,
            ICacheStorageProvider cacheProvider,
            IBufferProvider bufferProvider,
            byte[] checkPoint = null,
            int threadNum = 8,
            long threadSegmentSize = 8 * 1024 * 1024
        ) : base(task)
        {
            Ensure.That(task.Target is HttpRangableTarget, null,
                opts => opts.WithMessage("type of {0} must be {1}".AsFormat(
                    nameof(task.Target), nameof(HttpRangableTarget)
                ))
            ).IsTrue();
            Ensure.That(folderProvider, nameof(folderProvider)).IsNotNull();
            Ensure.That(cacheProvider, nameof(cacheProvider)).IsNotNull();
            Ensure.That(bufferProvider, nameof(bufferProvider)).IsNotNull();
            Ensure.That(threadNum, nameof(threadNum)).IsGte(1);
            Ensure.That(threadSegmentSize, nameof(threadSegmentSize)).IsGt(1024 * 1024);

            this.folderProvider = folderProvider;
            this.cacheProvider = cacheProvider;
            this.bufferProvider = bufferProvider;
            this.threadNum = threadNum;
            this.threadSegmentSize = threadSegmentSize;

            Progress = new CompositeProgress((task.Target as HttpRangableTarget).DataLength);
            Speed = SharedSpeedCalculatorFactory.NewSpeedCalculator();
            Progress.ProgressChanged += (sender, arg) => Speed.CurrentValue = Progress.DownloadedSize;

            if (checkPoint != null) ApplyCheckPoint(checkPoint);
        }

        public int WorkerCount { get; private set; }

        public int TasksRemained { get; private set; }

        protected override Task CancelAsync()
            => Task.Run(() =>
            {
                cancellationTokenSource?.Cancel();
                cancellationTokenSource = null;
                downloadTask?.Wait();
                downloadTask = null;
            });

        protected override Task DisposeAsync() => CancelAsync();

        protected override async Task StartAsync()
        {
            Speed.IsEnabled = true;

            await ValidateCacheFileAsync();
            ConcurrentQueue<Range> workQueue = ArrangeSegments();
            TasksRemained = workQueue.Count;
            cancellationTokenSource = new CancellationTokenSource();

            int realThreadNum = Math.Min(workQueue.Count, threadNum);
            Debug.WriteLine("[{0} with task {1}] launching {2} workers".AsFormat(
                nameof(HttpParallelDownloader), DownloadTask.Key, realThreadNum));

            var workers = new Task[realThreadNum];
            WorkerCount = realThreadNum;
            for(int i = 0; i < workers.Length; ++i)
                workers[i] = Worker(workQueue, cacheFile, cancellationTokenSource.Token, i);

            downloadTask = Task.Run(async () =>
            {
                try
                {
                    Task.WaitAll(workers);
                }
                catch(AggregateException e)
                {
                    if (e.InnerExceptions != null &&
                        e.InnerExceptions.Count > 0)
                        ReportError(e.InnerExceptions.First(), false);
                }
                catch(Exception e)
                {
                    ReportError(e, false);
                    return;
                }
                finally
                {
                    WorkerCount = 0;
                    Speed.IsEnabled = false;
                }

                try
                {
                    var progress = Progress as CompositeProgress;
                    if (progress.DownloadedSize == progress.TotalSize)
                    {
                        // get destination folder
                        var folder = await folderProvider.GetFolderFromTokenAsync(
                            DownloadTask.DestinationFolderKey);
                        // move cacheFile to destination folder
                        await cacheFile.MoveAsync(folder, DownloadTask.DestinationFileName,
                            NameCollisionOption.GenerateUniqueName);
                        var res = cacheFile;
                        cacheFile = null;
                        ReportCompleted(res);
                    }
                }
                catch(Exception e)
                {
                    ReportError(e);
                }
            });
        }

        /// <summary>
        /// Generate a queue with segments (ranges) arranged from
        /// Progress.UncoveredRanges.
        /// </summary>
        /// <returns>Concurrent queue with segments.</returns>
        private ConcurrentQueue<Range> ArrangeSegments()
        {
            ConcurrentQueue<Range> workQueue = new ConcurrentQueue<Range>();
            var progress = Progress as CompositeProgress;
            long maxSegment = threadSegmentSize + threadSegmentSize / 2;
            foreach (var task in progress.GetUncoveredRanges())
            {
                if (task.Length <= maxSegment) workQueue.Enqueue(task);
                else
                {
                    long now = task.Begin;
                    while(task.End - now > maxSegment)
                    {
                        long to = now + threadSegmentSize;
                        workQueue.Enqueue(new Range(now, to));
                        now = to;
                    }
                    if (now < task.End) workQueue.Enqueue(new Range(now, task.End));
                }
            }
            return workQueue;
        }

        /// <summary>
        /// Check if cacheFileToken has a valid value.
        /// Get the cache file from cacheProvider if so,
        /// otherwise clear the progress, and initialize a
        /// new cache file (with a new token).
        /// </summary>
        private async Task ValidateCacheFileAsync()
        {
            if (cacheFile == null)
            {
                if(cacheFileToken == string.Empty)
                {
                    cacheFileToken = await cacheProvider.NewCacheFileAsync();
                    cacheFile = cacheProvider.GetCacheFileByToken(cacheFileToken);
                    ((CompositeProgress)Progress).Initialize(
                        (DownloadTask.Target as HttpRangableTarget).DataLength);
                }
                else
                {
                    cacheFile = cacheProvider.GetCacheFileByToken(cacheFileToken);
                    if(cacheFile == null)
                    {
                        cacheFileToken = await cacheProvider.NewCacheFileAsync();
                        cacheFile = cacheProvider.GetCacheFileByToken(cacheFileToken);
                        ((CompositeProgress)Progress).Initialize(
                            (DownloadTask.Target as HttpRangableTarget).DataLength);
                    }
                }
            }
        }

        private async Task Worker(
            ConcurrentQueue<Range> taskQueue,
            IStorageFile file,
            CancellationToken cancellationToken,
            int workerId)
        {
            var prefix = "[{0} with task {1}] [worker {2}]".AsFormat(
                nameof(HttpParallelDownloader), DownloadTask.Key, workerId);
            SegmentProgress progress;
            Debug.WriteLine("{0} launched".AsFormat(prefix));
            while ((!cancellationToken.IsCancellationRequested)
                && (!taskQueue.IsEmpty))
            {
                bool succ = taskQueue.TryDequeue(out Range task);
                if (!succ) continue;
                TasksRemained = taskQueue.Count;
                Debug.WriteLine("{0} picked task [{1},{2}) , {3} remained in queue"
                    .AsFormat(prefix, task.Begin, task.End, taskQueue.Count));

                var target = (DownloadTask.Target as HttpRangableTarget);

                using (progress = ((CompositeProgress)Progress).NewSegmentProgress(task.Begin, task.Length))
                {
                    using (var istream = await target.GetRangedStreamAsync(task.Begin, task.End))
                    {
                        using (var ostream = new FileStream(file.Path, FileMode.Open, FileAccess.Write, FileShare.Write))
                        {
                            ostream.Seek(task.Begin, SeekOrigin.Begin);
                            await istream.CopyToAsync(
                                ostream,
                                bufferProvider,
                                cancellationToken,
                                size => progress.Increase(size)
                            );
                        }
                    }

                    if (cancellationToken.IsCancellationRequested)
                        Debug.WriteLine("{0} task canceled".AsFormat(prefix));
                    else if (progress.DownloadedSize == progress.TotalSize)
                        Debug.WriteLine("{0} task completed".AsFormat(prefix));
                    else
                    {
                        Debug.WriteLine("{0} task uncompleted".AsFormat(prefix));
                        throw new Exception("{0} task uncompleted".AsFormat(prefix));
                    }
                }
            }
        }

        public byte[] ToPersistentByteArray()
        {
            var progress = (CompositeProgress)Progress;
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(
                new InnerCheckPoint()
                {
                    TaskKey = DownloadTask.Key,
                    TotalSize = progress.TotalSize,
                    CacheFileToken = cacheFileToken,
                    CoveredRanges = progress.GetCoveredRanges().ToArray()
                }
            ));
        }

        private void ApplyCheckPoint(byte[] checkPointByteArray)
        {
            var checkPoint = JsonConvert.DeserializeObject<InnerCheckPoint>(
                Encoding.UTF8.GetString(checkPointByteArray));
            var progress = (CompositeProgress) Progress;
            Ensure.That(checkPoint.TaskKey, nameof(checkPoint.TaskKey))
                .IsEqualTo(DownloadTask.Key);
            Ensure.That(checkPoint.TotalSize, nameof(checkPoint.TotalSize))
                .Is(((HttpRangableTarget)DownloadTask.Target).DataLength);
            
            cacheFileToken = checkPoint.CacheFileToken;
            progress.Initialize(checkPoint.TotalSize, checkPoint.CoveredRanges);
        }

        private class InnerCheckPoint
        {
            public string TaskKey;
            public long TotalSize;
            public string CacheFileToken;
            public Range[] CoveredRanges;
        }

        private CancellationTokenSource cancellationTokenSource = null;
        private Task downloadTask = null;
        private string cacheFileToken = string.Empty;
        private IStorageFile cacheFile = null;
        private readonly IFolderProvider folderProvider;
        private readonly ICacheStorageProvider cacheProvider;
        private readonly IBufferProvider bufferProvider;
        private readonly int threadNum;
        private readonly long threadSegmentSize;
    }
}
