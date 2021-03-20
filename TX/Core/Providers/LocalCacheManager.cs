using Microsoft.Toolkit.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Core.Interfaces;
using TX.Core.Utils;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace TX.Core.Providers
{
    public class LocalCacheManager : IPersistable
    {
        public LocalCacheManager(StorageFolder cacheFolder)
        {
            this.cacheFolder = cacheFolder;
        }

        public void Initialize(byte[] persistentData)
        {
            cacheItems.Clear();
            if (persistentData != null)
            {
                string dataString = Encoding.UTF8.GetString(persistentData);
                var cacheArr = JsonConvert.DeserializeObject<
                    KeyValuePair<string, CacheItemInfo>[]>(dataString);
                foreach (var kvp in cacheArr) cacheItems.Add(kvp.Key, kvp.Value);
            }
        }

        public ICacheStorageProvider GetCacheProviderForTask(string taskKey)
            => new InnerProvider(this, taskKey);

        public IStorageFolder CacheFolder => cacheFolder;

        public byte[] ToPersistentByteArray()
        {
            lock (cacheItems)
            {
                string dataString = JsonConvert.SerializeObject(cacheItems.ToArray());
                return Encoding.UTF8.GetBytes(dataString);
            }
        }

        public async Task CleanCacheFolderAsync(Func<string, bool> isTaskActive)
        {
            try
            {
                Debug.WriteLine("[{0}] cleaning cache folder".AsFormat(
                    nameof(LocalCacheManager)));
                IEnumerable<IStorageItem> items = await cacheFolder.GetItemsAsync();
                if (!cacheFolder.IsEqual(ApplicationData.Current.LocalCacheFolder))
                    items = items.Concat(await ApplicationData.Current
                        .LocalCacheFolder.GetItemsAsync());

                List<IStorageItem> unused_items = null;

                lock (cacheItems)
                {
                    var unused_records = cacheItems.Where(
                        kvp =>
                            (!items.Any(item => item.Path.Equals(kvp.Value.FilePath))) ||
                            (!isTaskActive(kvp.Value.TaskKey))
                    ).ToList();

                    foreach (var record in unused_records)
                        cacheItems.Remove(record.Key);

                    unused_items = items.Where(
                        item => !cacheItems.Any(cacheItem => 
                            cacheItem.Value.FilePath.Equals(item.Path))).ToList();
                }

                foreach (var item in unused_items)
                {
                    try
                    {
                        StorageApplicationPermissions.FutureAccessList.Remove(
                            StorageApplicationPermissions.FutureAccessList.Add(item));
                        await item.DeleteAsync();
                        Debug.WriteLine($"[{nameof(LocalCacheManager)}] remove storage item <{item.Path}>");
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("[{0}] cache folder cleaning failed: \n{1}".AsFormat(
                    nameof(LocalCacheManager), e.Message));
            }
        }

        private async Task<string> NewCacheStorageForTaskAsync(string taskKey, bool isFolder)
        {
            try
            {
                string ext = RandomUtils.String(4);
                var randomName = "{0}-{1}".AsFormat(taskKey, ext);
                IStorageItem item = null;

                if (isFolder) item = await cacheFolder.CreateFolderAsync(
                    randomName, CreationCollisionOption.GenerateUniqueName);
                else item = await cacheFolder.CreateFileAsync(
                    randomName, CreationCollisionOption.GenerateUniqueName);

                StorageApplicationPermissions.FutureAccessList.Add(item);

                lock (cacheItems)
                {
                    string token = RandomUtils.String(16);
                    while (cacheItems.ContainsKey(token)) 
                        token = RandomUtils.String(16);
                    cacheItems[token] = new CacheItemInfo()
                    {
                        Token = token,
                        TaskKey = taskKey,
                        FilePath = item.Path,
                    };
                    return token;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private IStorageFile GetCacheFileForTask(string token, string taskKey)
        {
            lock (cacheItems)
            {
                try
                {
                    if (cacheItems.TryGetValue(token, out CacheItemInfo info))
                    {
                        if (info.TaskKey != taskKey)
                            return null;
                        var linqTask = StorageFile.GetFileFromPathAsync(info.FilePath).AsTask();
                        linqTask.Wait();
                        return linqTask.Result;
                    }
                    else return null;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        private IStorageFolder GetCacheFolderForTask(string token, string taskKey)
        {
            lock (cacheItems)
            {
                try
                {
                    if (cacheItems.TryGetValue(token, out CacheItemInfo info))
                    {
                        if (info.TaskKey != taskKey)
                            return null;
                        var linqTask = StorageFolder.GetFolderFromPathAsync(info.FilePath).AsTask();
                        linqTask.Wait();
                        return linqTask.Result;
                    }
                    else return null;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        private class InnerProvider : ICacheStorageProvider
        {
            public InnerProvider(LocalCacheManager manager, string key)
            {
                this.manager = manager;
                this.key = key;
            }

            public IStorageFile GetCacheFileByToken(string token)
                => manager.GetCacheFileForTask(token, key);

            public IStorageFolder GetCacheFolderByToken(string token)
                => manager.GetCacheFolderForTask(token, key);

            public Task<string> NewCacheFileAsync()
                => manager.NewCacheStorageForTaskAsync(key, false);

            public Task<string> NewCacheFolderAsync()
                => manager.NewCacheStorageForTaskAsync(key, true);

            private readonly LocalCacheManager manager;
            private readonly string key;
        }

        private class CacheItemInfo
        {
            public string Token;
            public string TaskKey;
            public string FilePath;
        }

        private readonly Dictionary<string, CacheItemInfo> cacheItems = 
            new Dictionary<string, CacheItemInfo>();
        private readonly StorageFolder cacheFolder = null;
    }
}
