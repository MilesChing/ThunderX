using System;
using System.IO;
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
            catch (TaskCanceledException) { }
            finally
            {
                bufferProvider.ReleaseBuffer(buffer);
            }
        }
    }
}
