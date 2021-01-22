using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TX.Core.Models.Progresses
{
    /// <summary>
    /// SegmentProgress is a BaseMeasuableProgress with an constant offset.
    /// A SegmentProgress is the progress of downloading a specific segment [Offset, Offset + TotalSize).
    /// </summary>
    class SegmentProgress : BaseMeasurableProgress
    {
        /// <summary>
        /// Initialize a SegmentProgress with given offset.
        /// </summary>
        /// <param name="offset">Offset of this segment.</param>
        /// <param name="totalSize"><see cref="BaseMeasurableProgress.BaseMeasurableProgress(long, long)"/></param>
        /// <param name="size"><see cref="BaseMeasurableProgress.BaseMeasurableProgress(long, long)"/></param>
        public SegmentProgress(long offset, long totalSize, long size = 0) : base(totalSize, size)
        {
            Offset = offset;
        }

        /// <summary>
        /// Left boundary of the downloading.
        /// A SegmentProgress is the progress of downloading a specific segment [Offset, Offset + TotalSize).
        /// </summary>
        public long Offset { get; private set; }
    }
}
