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
    class FakeCacheProvider : ICacheStorageProvider
    {
        public async Task<IStorageFile> GetCacheFileByTokenAsync(string token)
        {
            await Task.CompletedTask;
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

        public async Task<string> NewCacheFolderAsync()
        {
            int index = cacheFolders.Count;
            cacheFolders.Add(await ApplicationData.Current.LocalCacheFolder.CreateFolderAsync(
                index.ToString(), CreationCollisionOption.GenerateUniqueName));
            return index.ToString();
        }

        public async Task<IStorageFolder> GetCacheFolderByTokenAsync(string token)
        {
            await Task.CompletedTask;
            if (int.TryParse(token, out int index) &&
                index >= 0 && index < cacheFolders.Count)
                return cacheFolders[index];
            else return null;
        }

        private readonly List<StorageFile> cacheFiles = new List<StorageFile>();
        private readonly List<StorageFolder> cacheFolders = new List<StorageFolder>();
    }
}
