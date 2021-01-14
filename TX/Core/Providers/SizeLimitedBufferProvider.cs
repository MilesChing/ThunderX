using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Core.Interfaces;

namespace TX.Core.Providers
{
    public class SizeLimitedBufferProvider : IBufferProvider
    {
        public SizeLimitedBufferProvider(long capacity, long bufferBlockSize)
        {
            Capacity = capacity;
            BufferBlockSize = bufferBlockSize;
            counter = new BlockingCollection<object>(
                (int)(capacity / bufferBlockSize)
            );
        }

        /// <summary>
        /// Maximum size of buffers totally alloced.
        /// </summary>
        public readonly long Capacity;

        /// <summary>
        /// Size of single buffer alloced.
        /// </summary>
        public readonly long BufferBlockSize;

        /// <summary>
        /// Number of buffer blocks alloced.
        /// </summary>
        public int AllocedBlockNum =>
            counter.Count + freeBufferQueue.Count;

        /// <summary>
        /// Size of buffer totally alloced.
        /// </summary>
        public long AllocedBlockSize =>
            AllocedBlockNum * BufferBlockSize;

        public async Task<byte[]> AllocBufferAsync()
        {
            await Task.CompletedTask;
            if (freeBufferQueue.TryDequeue(out byte[] buffer))
                return buffer;
            counter.Add(new object());
            return new byte[BufferBlockSize];
        }

        public void ReleaseBuffer(byte[] buffer)
        {
            freeBufferQueue.Enqueue(buffer);
        }

        private readonly object lockedObject = new object();
        private readonly BlockingCollection<object> counter;
        private readonly ConcurrentQueue<byte[]> freeBufferQueue 
            = new ConcurrentQueue<byte[]>();
    }
}
