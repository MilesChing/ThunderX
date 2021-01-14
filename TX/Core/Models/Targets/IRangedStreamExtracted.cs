using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Core.Models.Progresses;

namespace TX.Core.Models.Targets
{
    /// <summary>
    /// IRangedStreamExtracted provides method to extract streams by given range.
    /// </summary>
    interface IRangedStreamExtracted
    {
        /// <summary>
        /// Get a stream from the given range.
        /// </summary>
        /// <returns>Task extracting stream containing data in the given range.</returns>
        Task<Stream> GetRangedStreamAsync(Range<long> range);
    }
}
