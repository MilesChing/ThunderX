using EnsureThat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Core.Interfaces;
using TX.Core.Models.Progresses.Interfaces;

namespace TX.Core.Models.Progresses
{
    /// <summary>
    /// CompositeProgress is a special IMeasuableProgress defined on [0, TotalSize).
    /// A CompositeProgress object consists of previously covered ranges and 
    /// some SegmentProgresses, which is active and might be increased to cover
    /// their range in the future.
    /// Previously covered ranges will only be modified once CompositeProgress
    /// is constructed or initialized.
    /// It's meant to take up ranges that were downloaded before 
    /// CompositeProgress was constructed.
    /// SegmentProgresses inside the CompositeProgress are all created through
    /// calling NewSegmentProgress.
    /// Once created, a SegmentProgress, say "sp", then occupies 
    /// its own range [Offset, Offset + TotalSize) and will prevent other 
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
            DownloadedSize = 0;
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
            DownloadedSize = downloaded;
        }

        public long TotalSize { get; private set; }

        public float Progress => ((float)DownloadedSize) / TotalSize;

        public long DownloadedSize 
        {
            get => downloadedSize;
            private set
            {
                long oldV, newV;
                lock (locked)
                {
                    oldV = downloadedSize;
                    newV = downloadedSize = value;
                }
                ProgressChanged?.Invoke(this, new BaseProgressChangedEventArg(oldV, newV));
            }
        }
        private long downloadedSize;

        /// <summary>
        /// Create a SegmentProgress in this CompositeProgress.
        /// See <see cref="CompositeProgress"/>.
        /// </summary>
        /// <param name="offset">Offset of the SegmentProgress created.</param>
        /// <param name="totalSize">TotalSize of the SegmentProgress created.</param>
        /// <returns>SegmentProgress created.</returns>
        public SegmentProgress NewSegmentProgress(long offset, long totalSize)
        {
            var targetRange = new Range(offset, offset + totalSize);
            Ensure.That(coveredRanges.All(covered => !targetRange.IsIntersectWith(covered))).IsTrue();
            Ensure.That(segmentProgresses.All(progress => !targetRange.IsIntersectWith(new Range(progress.Offset, progress.Offset + progress.TotalSize)))).IsTrue();
            var res = new SegmentProgress(offset, totalSize);
            res.ProgressChanged += AnySegmentProgressChanged;
            segmentProgresses.Add(res);
            return res;
        }

        /// <summary>
        /// Get an IEnumerable of ranges covered by any
        /// previously covered range or range covered by any 
        /// SegmentProgress (only downloaded part is considered).
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
            DownloadedSize = 0;
        }

        private void AnySegmentProgressChanged(IProgress _, IProgressChangedEventArg arg) =>
            DownloadedSize += arg.Delta;

        private readonly object locked = new object();
        private readonly List<Range> coveredRanges = new List<Range>();
        private readonly List<SegmentProgress> segmentProgresses 
            = new List<SegmentProgress>();
    }
}
