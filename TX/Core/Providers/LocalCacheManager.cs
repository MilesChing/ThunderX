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
        public void Initialize(byte[] persistentData)
        {
            cacheFiles.Clear();
            if (persistentData != null)
            {
                string dataString = Encoding.ASCII.GetString(persistentData);
                var cacheArr = JsonConvert.DeserializeObject<
                    KeyValuePair<string, CacheFileInfo>[]>(dataString);
                foreach (var kvp in cacheArr) cacheFiles.Add(kvp.Key, kvp.Value);
            }
        }

        public ICacheFileProvider GetCacheProviderForTask(string taskKey)
            => new InnerProvider(this, taskKey);

        public byte[] ToPersistentByteArray()
        {
            lock (cacheFiles)
            {
                string dataString = JsonConvert.SerializeObject(cacheFiles.ToArray());
                return Encoding.ASCII.GetBytes(dataString);
            }
        }

        public async Task CleanCacheFolderAsync(Func<string, bool> isTaskActive)
        {
            try
            {
                Debug.WriteLine("[{0}] cleaning cache folder".AsFormat(
                    nameof(LocalCacheManager)));
                var cacheFolder = ApplicationData.Current.LocalCacheFolder;
                var files = await cacheFolder.GetFilesAsync();
                List<StorageFile> unused_files = null;

                lock (cacheFiles)
                {
                    var unused_records = cacheFiles.Where(
                        kvp =>
                            (!files.Any(file => file.Path.Equals(kvp.Value.FilePath))) ||
                            (!isTaskActive(kvp.Value.TaskKey))
                    ).ToList();

                    foreach (var record in unused_records)
                        cacheFiles.Remove(record.Key);

                    unused_files = files.Where(
                        file => !cacheFiles.Any(
                            cacheFile => cacheFile.Value.FilePath.Equals(file.Path)
                        )
                    ).ToList();
                }

                foreach (var file in unused_files)
                {
                    Debug.WriteLine("[{0}] remove file <{1}>".AsFormat(
                        nameof(LocalCacheManager), file.Path));
                    await file.DeleteAsync();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("[{0}] cache folder cleaning failed: \n{1}".AsFormat(
                    nameof(LocalCacheManager), e.Message));
            }
        }

        private async Task<string> NewCacheFileForTaskAsync(string taskKey)
        {
            try
            {
                string ext = RandomUtils.String(4);
                var fileName = "{0}-{1}".AsFormat(taskKey, ext);
                var file = await ApplicationData.Current.LocalCacheFolder.
                    CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);
                fileName = file.Name;

                lock (cacheFiles)
                {
                    string token = RandomUtils.String(16);
                    while (cacheFiles.ContainsKey(token)) 
                        token = RandomUtils.String(16);
                    cacheFiles[token] = new CacheFileInfo()
                    {
                        Token = token,
                        TaskKey = taskKey,
                        FilePath = file.Path,
                    };
                    return token;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private IStorageFile GetCacheFileForTask(
            string token, string taskKey)
        {
            lock (cacheFiles)
            {
                try
                {
                    if (cacheFiles.TryGetValue(token, out CacheFileInfo info))
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

        private class InnerProvider : ICacheFileProvider
        {
            public InnerProvider(LocalCacheManager manager, string key)
            {
                this.manager = manager;
                this.key = key;
            }

            public IStorageFile GetCacheFileByToken(string token)
                => manager.GetCacheFileForTask(token, key);

            public Task<string> NewCacheFileAsync()
                => manager.NewCacheFileForTaskAsync(key);

            private readonly LocalCacheManager manager;
            private readonly string key;
        }

        private class CacheFileInfo
        {
            public string Token;
            public string TaskKey;
            public string FilePath;
        }

        private readonly Dictionary<string, CacheFileInfo> cacheFiles = 
            new Dictionary<string, CacheFileInfo>();
    }
}
