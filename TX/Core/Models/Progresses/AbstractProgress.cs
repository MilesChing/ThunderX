using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TX.Core.Models.Progresses
{
    public abstract class AbstractProgress
    {
        /// <summary>
        /// Number of byte downloaded.
        /// </summary>
        public long DownloadedSize 
        { 
            get => _downloaded_size_; 
            protected set
            {
                _downloaded_size_ = value;
                ProgressChanged?.Invoke(this);
            } 
        }
        private long _downloaded_size_;

        /// <summary>
        /// Happens when DownloadedSize is updated;
        /// </summary>
        public event Action<AbstractProgress> ProgressChanged;
    }

}
