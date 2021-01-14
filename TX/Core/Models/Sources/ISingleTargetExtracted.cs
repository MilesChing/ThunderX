using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Core.Models.Targets;

namespace TX.Core.Models.Sources
{
    /// <summary>
    /// ISingleTargetExtracted provides method to extract only one target.
    /// </summary>
    interface ISingleTargetExtracted
    {
        /// <summary>
        /// Get the downloading target.
        /// </summary>
        /// <returns>Task extracting the target.</returns>
        Task<AbstractTarget> GetTargetAsync();
    }
}
