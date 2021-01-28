using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Core.Interfaces;
using Windows.Storage;

namespace UnitTestProject.Utils
{
    /// <summary>
    /// FakeCacheProvider provides cache files created in LocalCacheFolder.
    /// </summary>
    class FakeCacheProvider : ICacheFileProvider
    {
        public IStorageFile GetCacheFileByToken(string token)
        {
            if (int.TryParse(token, out int index) &&
                index >= 0 && index < cacheFiles.Count)
                return cacheFiles[index];
            else return null;
        }

        public async Task<string> NewCacheFileAsync()
        {
            int index = cacheFiles.Count;
            cacheFiles.Add(await ApplicationData.Current.LocalCacheFolder.CreateFileAsync(
                index.ToString(), CreationCollisionOption.GenerateUniqueName));
            return index.ToString();
        }

        private readonly List<StorageFile> cacheFiles = new List<StorageFile>();
    }
}
