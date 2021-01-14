using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TX.Core.Models.Targets
{
    /// <summary>
    /// IStreamExtracted provides method to extract a stream.
    /// </summary>
    interface IStreamExtracted
    {
        /// <summary>
        /// Get a stream.
        /// </summary>
        /// <returns>Task extracting stream containing all data to be downloaded.</returns>
        Task<Stream> GetStreamAsync();
    }
}
