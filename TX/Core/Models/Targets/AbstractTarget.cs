using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Core.Interfaces;

namespace TX.Core.Models.Targets
{
    /// <summary>
    /// A target contains information of the endpoint 
    /// from which a stream of data is eventually downloaded
    /// and provides methods for extracting related streams.
    /// </summary>
    public abstract class AbstractTarget
    {
        /// <summary>
        /// Suggested file name of this target.
        /// </summary>
        public string SuggestedName => GetSuggestedName();

        /// <summary>
        /// Get the suggested file name of this target.
        /// </summary>
        /// <returns>Suggested file name</returns>
        protected abstract string GetSuggestedName();
    }
}
