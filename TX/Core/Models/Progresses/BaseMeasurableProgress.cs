using EnsureThat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Core.Models.Progresses.Interfaces;

namespace TX.Core.Models.Progresses
{
    /// <summary>
    /// A basic implementation of IMeasuableProgress.
    /// </summary>
    class BaseMeasurableProgress : BaseProgress, IMeasurableProgress
    {
        /// <summary>
        /// Construct a BaseMeasuableProgress object with totalSize and optional initialized size.
        /// </summary>
        /// <param name="totalSize">
        ///     Total size of this progress. 
        ///     Can't be motified once set.
        ///     Any attempt to increase DownloadedSize over TotalSize will cause an error.
        /// </param>
        /// <param name="size">
        ///     Initialized downloadedSize, can't be larger than <paramref name="totalSize"/>
        /// </param>
        public BaseMeasurableProgress(long totalSize, long size = 0) : base(size)
        {
            Ensure.That(totalSize).IsGt(0);
            Ensure.That(size).IsLte(totalSize);

            TotalSize = totalSize;
        }

        public long TotalSize { get; private set; }

        public float Progress => ((float)DownloadedSize) / TotalSize;

        /// <summary>
        /// If the downloading is completed.
        /// </summary>
        public bool IsCompleted => DownloadedSize.Equals(TotalSize);
    }
}
