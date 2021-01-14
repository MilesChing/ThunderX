using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TX.Core.Models.Targets
{
    /// <summary>
    /// ILengthSupported provides data's length.
    /// </summary>
    interface ILengthSupported
    {
        /// <summary>
        /// Get length of data to be downloaded.
        /// </summary>
        /// <returns>Length of data.</returns>
        long GetDataLength();
    }
}
