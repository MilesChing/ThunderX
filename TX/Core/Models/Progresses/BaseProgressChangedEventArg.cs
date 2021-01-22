using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Core.Models.Progresses.Interfaces;

namespace TX.Core.Models.Progresses
{
    /// <summary>
    /// A basic implementation of IProgressChangedEventArg.
    /// </summary>
    class BaseProgressChangedEventArg : IProgressChangedEventArg
    {
        public BaseProgressChangedEventArg(long oldSize, long newSize)
        {
            OldDownloadedSize = oldSize;
            NewDownloadedSize = newSize;
        }

        public long OldDownloadedSize { get; private set; }

        public long NewDownloadedSize { get; private set; }

        public long Delta => NewDownloadedSize - OldDownloadedSize;
    }
}
