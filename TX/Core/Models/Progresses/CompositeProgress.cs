using EnsureThat;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TX.Core.Interfaces;
using TX.Core.Models.Progresses.Interfaces;

namespace TX.Core.Models.Progresses
{
    /// <summary>
    /// CompositeProgress is a special IMeasuableProgress defined on 
    /// [0, TotalSize). A CompositeProgress object consists of previously 
    /// covered ranges and some SegmentProgresses, which is active and 
    /// might be increased to cover their range in the future.
    /// 
    /// Previously covered ranges are meant to take up ranges that were 
    /// downloaded before CompositeProgress was constructed.
    /// SegmentProgresses inside the CompositeProgress are all created 
    /// through calling NewSegmentProgress. Once created, a SegmentProgress, 
    /// say "sp", then occupies its own range [Offset, Offset + TotalSize). 
    /// Dispose a SegmentProgress to make it inactive and remove it from 
    /// its parent CompositeProgress.
    /// 
    /// Covered ranges and SegmentProgresses will prevent other 
    /// SegmentProgress that may overlap the range from being created.
    /// CompositeProgress ensures that its all previously covered ranges 
    /// and SegmentProgresses are not intersected.
    /// It is also ensured that DownloadedSize is the sum of the length 
    /// of covered ranges and downloaded part of SegmentProgresses.
    /// </summary>
    class CompositeProgress : IMeasurableProgress, IVisibleProgress
    {
        /// <summary>
        /// Construct a CompositeProgress object with nothing downloaded.
        /// </summary>
        /// <param name="totalSize"><see cref="BaseMeasurableProgress.BaseMeasurableProgress(long, long)"/></param>
        public CompositeProgress(long totalSize)
        {
            TotalSize = totalSize;
            DownloadedSize = 0;
        }

        public event Action<IProgress, IProgressChangedEventArg> ProgressChanged = (sender, arg) => { };
        public event Action<IVisibleProgress> VisibleRangeListChanged = (sender) => { };

        /// <summary>
        /// Initialize the CompositeProgress, clear all occupied ranges,
        /// reset DownloadedSize and TotalSize.
        /// </summary>
        /// <param name="totalSize"><see cref="CompositeProgress.CompositeProgress(long)"/></param>
        public void Initialize(long totalSize)
        {
            Clear();
            TotalSize = totalSize;
            InitializeIVisibleRangesCollection(Array.Empty<Range>());
        }

        /// <summary>
        /// Initialize the CompositeProgress, clear all occupied ranges,
        /// reset DownloadedSize and TotalSize.
        /// </summary>
        /// <param name="totalSize"><see cref="CompositeProgress.CompositeProgress(long)"/></param>
        /// <param name="initCoveredRanges">Previously covered ranges.</param>
        public void Initialize(long totalSize, IEnumerable<Range> initCoveredRanges)
        {
            Initialize(totalSize);
            long downloaded = 0;
            foreach (var range in initCoveredRanges)
            {
                coveredRanges.Add(range);
                downloaded += range.Length;
            }
            InitializeIVisibleRangesCollection(initCoveredRanges);
            DownloadedSize = downloaded;
        }

        public long TotalSize { get; private set; }

        public float Progress => ((float)DownloadedSize) / TotalSize;

        public long DownloadedSize 
        {
            get => downloadedSize;
            private set
            {
                long oldV = downloadedSize;
                downloadedSize = value;
                ProgressChanged?.Invoke(this, new BaseProgressChangedEventArg(oldV, value));
            }
        }
        private long downloadedSize;

        /// <summary>
        /// Create a SegmentProgress in this CompositeProgress.
        /// Dispose a SegmentProgress to remove it from CompositeProgress
        /// and stablize its downloaded range into previouslv covered ranges.
        /// 
        /// See <see cref="CompositeProgress"/>.
        /// </summary>
        /// <param name="offset">Offset of the SegmentProgress created.</param>
        /// <param name="totalSize">TotalSize of the SegmentProgress created.</param>
        /// <returns>SegmentProgress created.</returns>
        public SegmentProgress NewSegmentProgress(long offset, long totalSize)
        {
            var targetRange = new Range(offset, offset + totalSize);
            var res = new SegmentProgress(this, offset, totalSize);
            lock (segmentProgressesLock)
            {
                Ensure.That(coveredRanges.All(covered => !targetRange.IsIntersectWith(covered))).IsTrue();
                Ensure.That(segmentProgresses.All(progress => !targetRange.IsIntersectWith(new Range(progress.Offset, progress.Offset + progress.TotalSize)))).IsTrue();
                
                res.ProgressChanged += AnySegmentProgressChanged;
                res.Disposed += AnySegmentProgressDisposed;
                segmentProgresses.Add(res);
            }

            lock (ivisibleRangesLock)
            {
                var targetVisibles = ivisibleRangesCollection.Select(
                    range => range as InnerVisibleRange).Where(
                    iv => iv.TotalRange.IsIntersectWith(targetRange)).ToArray();

                foreach (var iv in targetVisibles)
                {
                    ivisibleRangesCollection.Remove(iv);
                    if (!targetRange.Contains(iv.TotalRange))
                    {
                        var remains = iv.TotalRange.Except(targetRange);
                        bool first = true;
                        var newIvs = remains.Select(range => {
                            var resiv = new InnerVisibleRange()
                            {
                                Progress = .0f,
                                TotalRange = new Range(range.Begin, range.End),
                                Total = (float)((double)(range.Length) / TotalSize)
                            };

                            if (first)
                            {
                                first = false;
                                resiv.Progress = iv.Progress / (resiv.Total / iv.Total);
                            }

                            return resiv;
                        });
                        foreach (var newIv in newIvs) ivisibleRangesCollection.Add(newIv);
                    }
                }

                var finalIv = new InnerVisibleRange()
                {
                    Parent = res,
                    Progress = .0f,
                    Total = (float)((double)(targetRange.Length) / TotalSize),
                    TotalRange = targetRange
                };

                ivisibleRangesCollection.Add(finalIv);
                res.ProgressChanged += finalIv.HandleParentProgressChanged;

                CleanIVisibleRanges();
                VisibleRangeListChanged.Invoke(this);
            }

            return res;
        }

        /// <summary>
        /// Get an IEnumerable of ranges covered by any
        /// previously covered range or range covered by any 
        /// SegmentProgress (only downloaded part is considered).
        /// 
        /// Not thread safe, initializing the CompositeProgress or 
        /// disposing any SegmentProgress expires the ienumerable.
        /// </summary>
        /// <returns>Ranges covered.</returns>
        public IEnumerable<Range> GetCoveredRanges() =>
            coveredRanges.Concat(
                segmentProgresses.Select(
                    prog => new Range(prog.Offset, prog.Offset + prog.DownloadedSize)
                )
            );

        /// <summary>
        /// Uncovered ranges are remained after covered ranges are excepted
        /// from [0, TotalSize). See <see cref="GetCoveredRanges"/>.
        /// 
        /// Not thread safe, initializing the CompositeProgress or 
        /// disposing any SegmentProgress expires the ienumerable.
        /// </summary>
        /// <returns>Ranges uncovered.</returns>
        public IEnumerable<Range> GetUncoveredRanges()
        {
            var coveredRanges = GetCoveredRanges().OrderBy(range => range.Begin);
            long begin = 0;
            foreach (var covered in coveredRanges)
            {
                if (covered.Begin > begin)
                    yield return new Range(begin, covered.Begin);
                begin = covered.End;
            }

            if (TotalSize > begin)
                yield return new Range(begin, TotalSize);
        }

        public IReadOnlyList<IVisibleRange> VisibleRangeList => ivisibleRangesCollection;
        private List<IVisibleRange> ivisibleRangesCollection = new List<IVisibleRange>();

        private class InnerVisibleRange : IVisibleRange
        {
            public float Progress
            {
                get => progress;
                set
                {
                    progress = value;
                    OnPropertyChanged();
                }
            }
            private float progress;

            public float Total 
            { 
                get => total; 
                set
                {
                    total = value;
                    OnPropertyChanged();
                }
            }
            private float total;

            public Range TotalRange { get; set; }

            public SegmentProgress Parent { set; get; } = null;

            public void HandleParentProgressChanged(IProgress arg1, IProgressChangedEventArg arg2)
            {
                var im = arg1 as IMeasurableProgress;
                Progress = im.Progress;
            }

            public void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));

            public event PropertyChangedEventHandler PropertyChanged = (sender, e) => { };
        }

        private void InitializeIVisibleRangesCollection(IEnumerable<Range> initCoveredRanges)
        {
            ivisibleRangesCollection.Clear();
            long left = 0, right = 0;
            foreach (var range in initCoveredRanges.OrderBy(range => range.Begin))
            {
                if (range.Begin != left)
                {
                    ivisibleRangesCollection.Add(new InnerVisibleRange()
                    {
                        Progress = (float)((double)(right - left) / (range.Begin - left)),
                        Total = (float)((double)(range.Begin - left) / TotalSize),
                        TotalRange = range
                    });
                }
                left = range.Begin;
                right = range.End;
            }
            if (left != TotalSize)
            {
                ivisibleRangesCollection.Add(new InnerVisibleRange()
                {
                    Progress = (float)((double)(right - left) / (TotalSize - left)),
                    Total = (float)((double)(TotalSize - left) / TotalSize),
                    TotalRange = new Range(left, TotalSize)
                });
            }
            CleanIVisibleRanges();
            VisibleRangeListChanged.Invoke(this);
        }

        private void CleanIVisibleRanges()
        {
            var newList = new List<IVisibleRange>();
            var enu = ivisibleRangesCollection.OrderBy(
                range => ((InnerVisibleRange)range).TotalRange.Begin).GetEnumerator();
            if (enu.MoveNext())
            {
                InnerVisibleRange jvr = (InnerVisibleRange) enu.Current;
                newList.Add(jvr);
                while (enu.MoveNext())
                {
                    InnerVisibleRange ivr = (InnerVisibleRange)enu.Current;
                    if (jvr.Progress == 1.0f && 
                        jvr.Parent == null &&
                        ivr.Parent == null &&
                        jvr.TotalRange.End == ivr.TotalRange.Begin)
                    {
                        newList.Remove(jvr);
                        var newJvr = new InnerVisibleRange()
                        {
                            Total = jvr.Total + ivr.Total,
                            TotalRange = jvr.TotalRange.Union(ivr.TotalRange),
                            Progress = (jvr.Progress * jvr.Total + ivr.Progress * ivr.Total) 
                                / (jvr.Total + ivr.Total)
                        };
                        newList.Add(jvr = newJvr);
                    }
                    else newList.Add(jvr = ivr);
                }
            }
            ivisibleRangesCollection = newList;
        }                

        private void Clear()
        {
            coveredRanges.Clear();
            InitializeIVisibleRangesCollection(Array.Empty<Range>());
            foreach (var prog in segmentProgresses)
                prog.ProgressChanged -= AnySegmentProgressChanged;
            segmentProgresses.Clear();
            DownloadedSize = 0;
        }

        private void AnySegmentProgressChanged(IProgress _, IProgressChangedEventArg arg)
        {
            lock (downloadedSizeLock)
                DownloadedSize += arg.Delta;
        }

        private void AnySegmentProgressDisposed(SegmentProgress prog)
        {
            prog.ProgressChanged -= AnySegmentProgressChanged;
            prog.Disposed -= AnySegmentProgressDisposed;
            lock (segmentProgressesLock)
            {
                segmentProgresses.Remove(prog);
                if (prog.DownloadedSize != 0)
                    coveredRanges.Add(new Range(
                        prog.Offset, prog.Offset + prog.DownloadedSize));
            }

            lock (ivisibleRangesLock)
            {
                var targetIv = ivisibleRangesCollection.First(
                    iv => (iv as InnerVisibleRange).Parent == prog) as InnerVisibleRange;
                targetIv.Parent = null;
                prog.ProgressChanged -= targetIv.HandleParentProgressChanged;
            }
        }

        private readonly object downloadedSizeLock = new object();
        private readonly object segmentProgressesLock = new object();
        private readonly object ivisibleRangesLock = new object();
        private readonly List<Range> coveredRanges = new List<Range>();
        private readonly List<SegmentProgress> segmentProgresses 
            = new List<SegmentProgress>();
    }
}
