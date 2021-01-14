using Microsoft.Toolkit.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Core.Downloaders;
using TX.Core.Interfaces;
using TX.Core.Models.Contexts;
using TX.Core.Models.Targets;
using TX.Core.Providers;
using TX.Core.Utils;
using TX.Utils;
using Windows.Storage;

namespace TX.Core
{
    public class TXCoreManager : IPersistable, IDisposable
    {
        public TXCoreManager()
        {
            coreBufferProvider = new SizeLimitedBufferProvider(
                SettingEntries.MemoryLimit, 512L * 1024L);
        }

        public void Initialize(byte[] checkPoint = null)
        {
            try
            {
                if (checkPoint == null) return;
                var json = Encoding.ASCII.GetString(checkPoint); 
                var checkPointObject = JsonConvert.DeserializeObject<InnerCheckPoint>(json,
                    new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All });
                foreach (var kvp in checkPointObject.Tasks) tasks.Add(kvp.Key, kvp.Value);
                coreCacheManager.Initialize(checkPointObject.CacheManagerCheckPoint);
                foreach (var kvp in checkPointObject.Downloaders) CreateDownloader(kvp.Key, kvp.Value);
                foreach (var hist in checkPointObject.Histories) histories.Add(hist);
                Debug.WriteLine("[{0}] initialized".AsFormat(nameof(TXCoreManager)));
            }
            catch (Exception e)
            {
                Debug.WriteLine("[{0}] initialization failed: \n{1}".AsFormat(
                    nameof(TXCoreManager), e.Message));
            }
        }

        public IReadOnlyDictionary<string, DownloadTask> Tasks => tasks;

        public IReadOnlyCollection<AbstractDownloader> Downloaders => downloaders;

        public INotifyCollectionChanged ObservableDownloaders => downloaders;

        public IReadOnlyCollection<DownloadHistory> Histories => histories;

        public INotifyCollectionChanged ObservableHistories => histories;

        public void RemoveHistory(DownloadHistory history) =>
            histories.Remove(history);

        public string CreateTask(
            AbstractTarget target,
            IStorageFolder destinationFolder,
            string customFileName = null)
        {
            string token = RandomUtils.String(8);
            while (tasks.ContainsKey(token))
                token = RandomUtils.String(8);

            tasks[token] = new DownloadTask(
                token, target,
                customFileName,
                coreFolderManager.StoreFolder(destinationFolder),
                DateTime.Now
            );

            CreateDownloader(token);

            return token;
        }

        private void CreateDownloader(string token, byte[] checkPoint = null)
        {
            if (!tasks.TryGetValue(token, out DownloadTask task)) return;
            AbstractDownloader downloader = null;

            if (task.Target is HttpRangableTarget httpRangableTarget)
                downloader = new HttpParallelDownloader(
                    task, 
                    coreFolderManager, 
                    coreCacheManager.GetCacheProviderForTask(token), 
                    coreBufferProvider,
                    checkPoint,
                    SettingEntries.ThreadNumber);
            else if (task.Target is HttpTarget httpTarget)
                downloader = new HttpDownloader(
                    task, coreFolderManager,
                    coreCacheManager.GetCacheProviderForTask(token),
                    coreBufferProvider);
            if (downloader != null)
            {
                downloader.MaximumRetries = SettingEntries.MaximumRetries;
                downloader.StatusChanged += AnyDownloader_StatusChanged;
                downloaders.Add(downloader);
            }
        }

        private void AnyDownloader_StatusChanged(
            AbstractDownloader sender, 
            DownloaderStatus status)
        {
            if (status == DownloaderStatus.Completed)
            {
                histories.Add(new DownloadHistory(
                    sender.DownloadTask.Key,
                    sender.Result.Path,
                    DateTime.Now));
                if (SettingEntries.IsNotificationEnabledWhenTaskCompleted)
                {
                    ToastManager.ShowDownloadCompleteToastAsync(
                        "Task Completed", sender.DownloadTask.DestinationFileName,
                        sender.Result.Path, Path.GetDirectoryName(sender.Result.Path));
                }
            }
            else if (status == DownloaderStatus.Error)
            {
                if (SettingEntries.IsNotificationEnabledWhenFailed)
                {
                    ToastManager.ShowSimpleToast(
                        "Task Failed: " + sender.DownloadTask.DestinationFileName,
                        sender.Errors.First().Message);
                }
            }
            else if (status == DownloaderStatus.Disposed)
            {
                sender.StatusChanged -= AnyDownloader_StatusChanged;
                downloaders.Remove(sender);
            }
        }

        public byte[] ToPersistentByteArray()
        {
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(
                new InnerCheckPoint()
                {
                    Tasks = Tasks.ToArray(),
                    Downloaders = Downloaders.Where(
                        downloader => 
                            downloader.Status != DownloaderStatus.Completed &&
                            downloader.Status != DownloaderStatus.Disposed)
                    .Select(
                        downloader =>
                        {
                            byte[] val = null;
                            if (downloader is IPersistable per)
                                val = per.ToPersistentByteArray();
                            return new KeyValuePair<string, byte[]>(
                                downloader.DownloadTask.Key, val);
                        }).ToArray(),
                    Histories = Histories.ToArray(),
                    CacheManagerCheckPoint = coreCacheManager.ToPersistentByteArray(),
                }, 
                new JsonSerializerSettings()
                {
                    TypeNameHandling = TypeNameHandling.All
                }));
        }

        public void Dispose()
        {
            Debug.WriteLine("[{0}] disposing".AsFormat(nameof(TXCoreManager)));

            Task.Run(async () => 
                await coreCacheManager.CleanCacheFolderAsync(
                    taskKey =>
                        Downloaders.Any(downloader => downloader.DownloadTask.Key.Equals(taskKey))
                )
            ).Wait();

            foreach (var downloader in downloaders)
                downloader.Cancel();

            Debug.WriteLine("[{0}] disposed".AsFormat(nameof(TXCoreManager)));
        }

        private readonly Settings SettingEntries = new Settings();
        private readonly Dictionary<string, DownloadTask> tasks = new Dictionary<string, DownloadTask>();
        private readonly ObservableCollection<AbstractDownloader> downloaders = new ObservableCollection<AbstractDownloader>();
        private readonly ObservableCollection<DownloadHistory> histories = new ObservableCollection<DownloadHistory>();
        private readonly LocalCacheManager coreCacheManager = new LocalCacheManager();
        private readonly LocalFolderManager coreFolderManager = new LocalFolderManager();
        private readonly SizeLimitedBufferProvider coreBufferProvider = null;

        private class InnerCheckPoint
        {
            public KeyValuePair<string, DownloadTask>[] Tasks;

            public KeyValuePair<string, byte[]>[] Downloaders;

            public DownloadHistory[] Histories;

            public byte[] CacheManagerCheckPoint;
        }
    }
}
