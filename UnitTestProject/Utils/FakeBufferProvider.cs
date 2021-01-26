using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Core.Interfaces;

namespace UnitTestProject.Utils
{
    class FakeBufferProvider : IBufferProvider
    {
#pragma warning disable CS1998 // 异步方法缺少 "await" 运算符，将以同步方式运行
        public async Task<byte[]> AllocBufferAsync()
#pragma warning restore CS1998 // 异步方法缺少 "await" 运算符，将以同步方式运行
            => new byte[512L * 1024L];

        public void ReleaseBuffer(byte[] buffer) { }
    }
}
