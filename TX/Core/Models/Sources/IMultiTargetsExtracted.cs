using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Core.Models.Targets;

namespace TX.Core.Models.Sources
{
    /// <summary>
    /// IMultiTargetsExtracted provides method to extract multiply targets.
    /// </summary>
    interface IMultiTargetsExtracted
    {
        /// <summary>
        /// GetTargetInfosAsync returns a task whose result is an
        /// IEnumerable of target infos.
        /// A target info is represented by its key and 
        /// its displaied name, which will be shown and
        /// selected by user.
        /// </summary>
        Task<IEnumerable<KeyValuePair<string, string>>> GetTargetInfosAsync();

        /// <summary>
        /// GetTargetAsync returns a target for user filtered
        /// target keys, observed from GetTargetInfosAsync.
        /// More than one keys inputed is not allowed when
        /// IsMultiSelectionSupported is false.
        /// </summary>
        /// <param name="keys">Keys of selected targets.</param>
        Task<AbstractTarget> GetTargetAsync(IEnumerable<string> keys);

        /// <summary>
        /// Get a value indicates whether get a target 
        /// using multiple infos is supported.
        /// </summary>
        bool IsMultiSelectionSupported { get; }
    }
}
