using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Core.Interfaces;
using Windows.Storage;

namespace TXUnitTest.Utils
{
    /// <summary>
    /// FakeFolderProvider always provides LocalCacheFolder for test.
    /// </summary>
    class FakeFolderProvider : IFolderProvider
    {
        public Task<IStorageFolder> GetFolderFromTokenAsync(string token) =>
            Task.Run<IStorageFolder>(() => ApplicationData.Current.LocalCacheFolder);
    }
}
