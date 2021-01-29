using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TX.Core.Models.Progresses.Interfaces
{
    public interface IProgressChangedEventArg
    {
        /// <summary>
        /// DownloadedSize before this change.
        /// </summary>
        long OldDownloadedSize { get; }

        /// <summary>
        /// DownloadedSize after this change.
        /// </summary>
        long NewDownloadedSize { get; }

        /// <summary>
        /// The variation of DownloadedSize.
        /// </summary>
        long Delta { get; }
    }
}
