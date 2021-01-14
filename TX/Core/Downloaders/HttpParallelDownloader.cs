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
            ICacheFileProvider cacheProvider,
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

            Progress = new InnerRangableProgress((task.Target as HttpRangableTarget).DataLength);
            Speed = SharedSpeedCalculatorFactory.NewSpeedCalculator();
            Progress.ProgressChanged += (sender) =>
                Speed.CurrentValue = Progress.DownloadedSize;

            if (checkPoint != null)
                ApplyCheckPoint(checkPoint);
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
            ConcurrentQueue<Range<long>> workQueue = ArrangeSegments();
            TasksRemained = workQueue.Count;
            cancellationTokenSource = new CancellationTokenSource();

            int realThreadNum = Math.Min(workQueue.Count, threadNum);
            Debug.WriteLine("[{0} with task {1}] launching {2} workers".AsFormat(
                nameof(HttpParallelDownloader),
                DownloadTask.Key,
                realThreadNum));
            var workers = new Task[realThreadNum];
            WorkerCount = realThreadNum;
            for(int i = 0; i < workers.Length; ++i)
                workers[i] = Worker(workQueue, cacheFile,
                    cancellationTokenSource.Token, i);

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
                    var progress = Progress as InnerRangableProgress;
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
        private ConcurrentQueue<Range<long>> ArrangeSegments()
        {
            ConcurrentQueue<Range<long>> workQueue =
                new ConcurrentQueue<Range<long>>();
            var progress = Progress as InnerRangableProgress;
            long maxSegment = threadSegmentSize + threadSegmentSize / 2;
            foreach (var task in progress.UncoveredRanges)
            {
                if (task.To - task.From <= maxSegment)
                    workQueue.Enqueue(task);
                else
                {
                    long now = task.From;
                    while(task.To - now > maxSegment)
                    {
                        long to = now + threadSegmentSize;
                        workQueue.Enqueue(new Range<long>(now, to));
                        now = to;
                    }
                    if (now < task.To)
                        workQueue.Enqueue(new Range<long>(now, task.To));
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
                    ((InnerRangableProgress)Progress).Initialize(Array.Empty<Range<long>>());
                }
                else
                {
                    cacheFile = cacheProvider.GetCacheFileByToken(cacheFileToken);
                    if(cacheFile == null)
                    {
                        cacheFileToken = await cacheProvider.NewCacheFileAsync();
                        cacheFile = cacheProvider.GetCacheFileByToken(cacheFileToken);
                        ((InnerRangableProgress)Progress).Initialize(Array.Empty<Range<long>>());
                    }
                }
            }
        }

        private async Task Worker(
            ConcurrentQueue<Range<long>> taskQueue,
            IStorageFile file,
            CancellationToken cancellationToken,
            int workerId)
        {
            var prefix = "[{0} with task {1}] [worker {2}]".AsFormat(
                nameof(HttpParallelDownloader), DownloadTask.Key, workerId);
            Debug.WriteLine("{0} launched".AsFormat(prefix));
            while ((!cancellationToken.IsCancellationRequested)
                && (!taskQueue.IsEmpty))
            {
                bool succ = taskQueue.TryDequeue(out Range<long> task);
                var progress = (Progress as InnerRangableProgress)
                    .NewThreadProgress(task.From, task.To - task.From);
                if (!succ) continue;
                TasksRemained = taskQueue.Count;
                Debug.WriteLine("{0} picked task [{1},{2}] , {3} remained in queue"
                    .AsFormat(prefix, task.From, task.To, taskQueue.Count));
                var target = (DownloadTask.Target as HttpRangableTarget);
                using (var istream = await target.GetRangedStreamAsync(task))
                using (var ostream = new FileStream(file.Path,
                    FileMode.Open, FileAccess.Write, FileShare.Write))
                {
                    ostream.Seek(task.From, SeekOrigin.Begin);
                    await istream.CopyToAsync(
                        ostream,
                        bufferProvider,
                        cancellationToken,
                        size => progress.ReportProgress(size)
                    );
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

        public byte[] ToPersistentByteArray()
        {
            var progress = (InnerRangableProgress)Progress;
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(
                new InnerCheckPoint()
                {
                    TaskKey = DownloadTask.Key,
                    TotalSize = progress.TotalSize,
                    CacheFileToken = cacheFileToken,
                    CoveredRanges = progress.CoveredRanges.ToArray()
                }
            ));
        }

        private void ApplyCheckPoint(byte[] checkPointByteArray)
        {
            var checkPoint = JsonConvert.DeserializeObject<InnerCheckPoint>(
                Encoding.ASCII.GetString(checkPointByteArray));
            var progress = (InnerRangableProgress) Progress;
            Ensure.That(checkPoint.TaskKey, nameof(checkPoint.TaskKey))
                .IsEqualTo(DownloadTask.Key);
            Ensure.That(checkPoint.TotalSize, nameof(checkPoint.TotalSize))
                .Is(((HttpRangableTarget)DownloadTask.Target).DataLength);
            
            cacheFileToken = checkPoint.CacheFileToken;
            progress.Initialize(checkPoint.CoveredRanges);
        }

        private class InnerCheckPoint
        {
            public string TaskKey;
            public long TotalSize;
            public string CacheFileToken;
            public Range<long>[] CoveredRanges;
        }

        private class InnerRangableProgress : 
            AbstractMeasurableProgress,
            ICoveredLinq
        {
            public InnerRangableProgress(
                long totalSize)
            {
                TotalSize = totalSize;
            }

            public void Initialize(IEnumerable<Range<long>> covered)
            {
                DownloadedSize = covered.Sum(r => r.To - r.From);
                CoveredLinkedList.Clear();
                foreach (var r in covered.OrderBy(o => o.From))
                    CoveredLinkedList.AddLast(r);
            }

            public IEnumerable<Range<long>> UncoveredRanges
            {
                get
                {
                    long next = 0;
                    foreach (var range in CoveredLinkedList)
                    {
                        if (next < range.From)
                            yield return new Range<long>(next, range.From);
                        next = range.To;
                    }
                    if (next < TotalSize)
                        yield return new Range<long>(next, TotalSize);
                }
            }

            public IEnumerable<Range<long>> CoveredRanges => 
                CoveredLinkedList.Where(r => r.To != r.From);

            public InnerThreadProgress NewThreadProgress(long offset, long totalSize)
            {
                lock (LockedObject)
                {
                    var target = new Range<long>(offset, offset);
                    LinkedListNode<Range<long>> node = null;
                    if (CoveredLinkedList.First == null)
                        node = CoveredLinkedList.AddFirst(target);
                    else
                    {
                        var f = CoveredLinkedList.First;
                        while (f.Next != null && f.Next.Value.From < target.From)
                            f = f.Next;
                        node = CoveredLinkedList.AddAfter(f, target);
                    }
                    var res = new InnerThreadProgress(offset, totalSize, node);
                    res.ProgressChanged += NodeProgressChanged;
                    return res;
                }
            }

            private void NodeProgressChanged(AbstractProgress sender)
            {
                if (sender is InnerThreadProgress prog)
                {
                    long oldSize = prog.Node.Value.To - prog.Node.Value.From;
                    long delta = prog.DownloadedSize - oldSize;
                    lock (LockedObject) DownloadedSize += delta;
                    prog.Node.Value = new Range<long>(
                        prog.Node.Value.From,
                        prog.Node.Value.From + 
                        prog.DownloadedSize);
                }
            }

            public IEnumerable<KeyValuePair<Range<long>, bool>> GetRangeAndDownloadingStatus()
                => CoveredRanges.Select(cr => new KeyValuePair<Range<long>, bool>(cr, true))
                    .Concat(UncoveredRanges.Select(cr => new KeyValuePair<Range<long>, bool>(cr, true)));

            private readonly LinkedList<Range<long>> CoveredLinkedList
                = new LinkedList<Range<long>>();
            private readonly object LockedObject = new object();
        }

        private class InnerThreadProgress : AbstractMeasurableProgress
        {
            public InnerThreadProgress(
                long offset,
                long totalSize, 
                LinkedListNode<Range<long>> node)
            {
                DownloadedSize = 0;
                Offset = offset;
                TotalSize = totalSize;
                Node = node;
            }

            public Range<long> TaskRange => new Range<long>(Offset, Offset + TotalSize);

            public void ReportProgress(long size) =>
                DownloadedSize = Math.Min(size, TotalSize);

            public long Offset { get; }

            public readonly LinkedListNode<Range<long>> Node;
        }

        private CancellationTokenSource cancellationTokenSource = null;
        private Task downloadTask = null;
        private string cacheFileToken = string.Empty;
        private IStorageFile cacheFile = null;
        private readonly IFolderProvider folderProvider;
        private readonly ICacheFileProvider cacheProvider;
        private readonly IBufferProvider bufferProvider;
        private readonly int threadNum;
        private readonly long threadSegmentSize;
    }
}
