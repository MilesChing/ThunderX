﻿using EnsureThat;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Core.Interfaces;
using TX.Core.Models.Contexts;
using TX.Core.Utils;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace TX.Core.Providers
{
    public class LocalCacheManager : IPersistable
    {
        public LocalCacheManager() { }

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

            D($"Initialized, {cacheItems.Count} entries in total");
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

        public async Task CleanCacheFolderAsync(
            IEnumerable<DownloadTask> activeTasks,
            IEnumerable<IStorageItem> ignoreItems)
        {
            try
            {
                D("Cleaning cache folder");
                Ensure.That(activeTasks).IsNotNull();

                IEnumerable<IStorageItem> items = await cacheFolder.GetItemsAsync();
                if (!cacheFolder.IsEqual(ApplicationData.Current.LocalCacheFolder))
                    items = items.Concat(await ApplicationData.Current
                        .LocalCacheFolder.GetItemsAsync());

                List<IStorageItem> unused_items = null;

                lock (cacheItems)
                {
                    var unused_records = cacheItems.Where(
                        kvp =>
                            activeTasks.All(task => !task.Key.Equals(kvp.Value.TaskKey)) ||
                            items.All(item => !item.Path.Equals(kvp.Value.FilePath))
                    ).ToList();

                    foreach (var record in unused_records)
                    {
                        cacheItems.Remove(record.Key);
                        D($"Unused cache item entry <{record.Value.FilePath}> removed");
                    }

                    unused_items = items.Where(
                        item => 
                            ignoreItems.All(ign => !ign.Path.Equals(item.Path)) && 
                            cacheItems.All(cacheItem => !cacheItem.Value.FilePath.Equals(item.Path))
                    ).ToList();
                }

                foreach (var item in unused_items)
                {
                    try
                    {
                        await item.DeleteAsync();
                        D($"Removed storage item <{item.Path}>");
                    }
                    catch (Exception e)
                    {
                        D($"Storage item removal error: {e.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                D($"Cache folder cleaning failed: {e.Message}");
            }
        }

        private async Task<string> NewCacheStorageForTaskAsync(string taskKey, bool isFolder)
        {
            try
            {
                string ext = RandomUtils.String(4);
                var randomName = $"{taskKey}-{ext}";
                IStorageItem item = null;

                if (isFolder)
                {
                    item = await cacheFolder.CreateFolderAsync(
                        randomName, CreationCollisionOption.GenerateUniqueName);
                    D($"Cache folder created <{item.Path}>");
                }
                else
                {
                    item = await cacheFolder.CreateFileAsync(
                        randomName, CreationCollisionOption.GenerateUniqueName);
                    D($"Cache file created <{item.Path}>");
                }

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

        private async Task<IStorageFile> GetCacheFileForTaskAsync(string token, string taskKey)
        {
            string filePath = string.Empty;

            lock (cacheItems)
            {
                try
                {
                    if (cacheItems.TryGetValue(token, out CacheItemInfo info))
                    {
                        if (info.TaskKey != taskKey) return null;
                        else filePath = info.FilePath;
                    }
                    else return null;
                }
                catch (Exception)
                {
                    return null;
                }
            }

            return await StorageFile.GetFileFromPathAsync(filePath);
        }

        private async Task<IStorageFolder> GetCacheFolderForTaskAsync(string token, string taskKey)
        {
            string folderPath = string.Empty;

            lock (cacheItems)
            {
                try
                {
                    if (cacheItems.TryGetValue(token, out CacheItemInfo info))
                    {
                        if (info.TaskKey != taskKey)
                            return null;
                        else folderPath = info.FilePath;
                    }
                    else return null;
                }
                catch (Exception)
                {
                    return null;
                }
            }

            return await StorageFolder.GetFolderFromPathAsync(folderPath);
        }

        private void D(string message) => Debug.WriteLine($"[{GetType().Name}] {message}");

        private class InnerProvider : ICacheStorageProvider
        {
            public InnerProvider(LocalCacheManager manager, string key)
            {
                this.manager = manager;
                this.key = key;
            }

            public Task<IStorageFile> GetCacheFileByTokenAsync(string token)
                => manager.GetCacheFileForTaskAsync(token, key);

            public Task<IStorageFolder> GetCacheFolderByTokenAsync(string token)
                => manager.GetCacheFolderForTaskAsync(token, key);

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
        private readonly StorageFolder cacheFolder =
            ApplicationData.Current.LocalCacheFolder;
    }
}
