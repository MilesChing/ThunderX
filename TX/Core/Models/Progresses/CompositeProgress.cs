using EnsureThat;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
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
    class CompositeProgress : IMeasurableProgress
    {
        /// <summary>
        /// Construct a CompositeProgress object with nothing downloaded.
        /// </summary>
        /// <param name="totalSize"><see cref="BaseMeasurableProgress.BaseMeasurableProgress(long, long)"/></param>
        public CompositeProgress(long totalSize)
        {
            TotalSize = totalSize;
            downloadedSize = 0;
        }

        public event Action<IProgress, IProgressChangedEventArg> ProgressChanged;

        /// <summary>
        /// Initialize the CompositeProgress, clear all occupied ranges,
        /// reset DownloadedSize and TotalSize.
        /// </summary>
        /// <param name="totalSize"><see cref="CompositeProgress.CompositeProgress(long)"/></param>
        public void Initialize(long totalSize)
        {
            Clear();
            TotalSize = totalSize;
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
            OnDownloadedSizeChanged(downloadedSize, 
                downloadedSize = downloaded);
        }

        public long TotalSize { get; private set; }

        public float Progress => ((float)DownloadedSize) / TotalSize;

        public long DownloadedSize => downloadedSize;
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

        private void Clear()
        {
            coveredRanges.Clear();
            foreach (var prog in segmentProgresses)
                prog.ProgressChanged -= AnySegmentProgressChanged;
            segmentProgresses.Clear();
            OnDownloadedSizeChanged(downloadedSize, downloadedSize = 0);
        }

        private void AnySegmentProgressChanged(IProgress _, IProgressChangedEventArg arg)
        {
            long newVal = Interlocked.Add(ref downloadedSize, arg.Delta);
            OnDownloadedSizeChanged(newVal - arg.Delta, newVal);
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
        }

        private void OnDownloadedSizeChanged(long oldSize, long newSize) =>
            ProgressChanged(this, new BaseProgressChangedEventArg(oldSize, newSize));

        private readonly object segmentProgressesLock = new object();
        private readonly List<Range> coveredRanges = new List<Range>();
        private readonly List<SegmentProgress> segmentProgresses 
            = new List<SegmentProgress>();
    }
}
