using EnsureThat;
using System;
using TX.Core.Models.Progresses.Interfaces;

namespace TX.Core.Models.Progresses
{
    /// <summary>
    /// BaseProgress is a basic implementation of IProgress whose value is
    /// supported to be increased and reset to 0.
    /// </summary>
    class BaseProgress : IProgress
    {
        /// <summary>
        /// Construct a BaseProgress object with given size.
        /// </summary>
        /// <param name="size">Initialized DownloadedSize.</param>
        public BaseProgress(long size = 0)
        {
            Ensure.That(size).IsGte(0);
            DownloadedSize = size;
        }

        public long DownloadedSize { get; private set; }

        /// <summary>
        /// Increase DownloadedSize by delta.
        /// </summary>
        /// <param name="delta">Delta must be positive.</param>
        public void Increase(long delta)
        {
            Ensure.That(delta).IsGte(0);
            long old = DownloadedSize;
            DownloadedSize += delta;
            ProgressChanged?.Invoke(this, new BaseProgressChangedEventArg(old, DownloadedSize));
        }

        /// <summary>
        /// Reset DoenloadedSize to 0.
        /// This function will trigger a ProgressChanged event with delta = -(old DownloadedSize).
        /// </summary>
        public void Reset()
        {
            long old = DownloadedSize;
            DownloadedSize = 0;
            ProgressChanged?.Invoke(this, new BaseProgressChangedEventArg(old, 0));
        }

        public event Action<IProgress, IProgressChangedEventArg> ProgressChanged;
    }
}
