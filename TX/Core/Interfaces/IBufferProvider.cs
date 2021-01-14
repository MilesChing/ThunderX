using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TX.Core.Interfaces
{
    public interface IBufferProvider
    {
        Task<byte[]> AllocBufferAsync();

        void ReleaseBuffer(byte[] buffer);
    }
}
