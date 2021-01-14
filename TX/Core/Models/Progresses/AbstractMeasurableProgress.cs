using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TX.Core.Models.Progresses
{
    public abstract class AbstractMeasurableProgress : AbstractProgress
    {
        /// <summary>
        /// Total number of byte to be downloaded.
        /// </summary>
        public long TotalSize { get; protected set; }

        /// <summary>
        /// Percentage of data downloaded.
        /// </summary>
        public float Percentage => ((float)((double)DownloadedSize) / TotalSize);
    }
}
