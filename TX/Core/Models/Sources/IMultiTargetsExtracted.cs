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
        /// GetTargetsAsync returns a task whose result is an
        /// IEnumerable of target entries.
        /// An target entry is represented by its displayed name
        /// and a asynchronous task to get it.
        /// </summary>
        Task<IEnumerable<KeyValuePair<string, Task<AbstractTarget>>>> GetTargetsAsync();
    }
}
