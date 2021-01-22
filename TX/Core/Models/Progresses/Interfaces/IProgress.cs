using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TX.Core.Models.Progresses.Interfaces
{
    public interface IProgress
    {
        /// <summary>
        /// Size downloaded.
        /// </summary>
        long DownloadedSize { get; }

        /// <summary>
        /// Occured once this progress has been changed.
        /// </summary>
        event Action<IProgress, IProgressChangedEventArg> ProgressChanged;
    }
}
