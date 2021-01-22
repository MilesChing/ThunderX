using Microsoft.Toolkit.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TX.Core.Interfaces;

namespace TX.Core.Utils
{
    static class StreamExtension
    {
        public static async Task CopyToAsync(
            this Stream istream, 
            Stream ostream,
            IBufferProvider bufferProvider, 
            CancellationToken token,
            Action<long> progressIncreased)
        {
            var buffer = await bufferProvider.AllocBufferAsync();
            try
            {
                while (!token.IsCancellationRequested)
                {
                    int readLen = await istream.ReadAsync(buffer, 0, buffer.Length, token);
                    if (readLen <= 0) break;
                    await ostream.WriteAsync(buffer, 0, readLen, token);
                    progressIncreased(readLen);
                }
            }
            finally
            {
                bufferProvider.ReleaseBuffer(buffer);
            }
        }
    }
}
