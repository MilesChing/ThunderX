using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TX.Core.Models.Progresses.Interfaces
{
    public interface IMeasurableProgress : IProgress
    {
        /// <summary>
        /// Target size of the task or stream.
        /// </summary>
        long TotalSize { get; }

        /// <summary>
        /// Progress of the downloading, floating number between 0 and 1.
        /// </summary>
        float Progress { get; }
    }
}
