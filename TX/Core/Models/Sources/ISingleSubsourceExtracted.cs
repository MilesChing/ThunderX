using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TX.Core.Models.Sources
{
    /// <summary>
    /// ISingleSubsourceExtracted provides method to extract only one subsource.
    /// </summary>
    interface ISingleSubsourceExtracted
    {
        /// <summary>
        /// Get the subsource.
        /// </summary>
        /// <returns>Task extracting the subsource.</returns>
        Task<AbstractSource> GetSubsourceAsync();
    }
}
