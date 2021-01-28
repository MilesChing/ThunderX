using System.IO;
using System.Threading.Tasks;

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
        Task<Stream> GetRangedStreamAsync(long begin, long end);
    }
}
