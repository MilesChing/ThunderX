using MonoTorrent.Dht;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using TX.Collections;
using TX.Core.Downloaders;
using TX.Core.Interfaces;
using TX.Core.Models.Contexts;
using TX.Core.Models.Targets;
using TX.Core.Providers;
using TX.Core.Utils;
using TX.Utils;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace TX.Core
{
    public class TXCoreManager : IPersistable, IDisposable
    {
        public TXCoreManager()
        {
            coreBufferProvider = new SizeLimitedBufferProvider(
                settingEntries.MemoryLimit, 512L * 1024L);
            taskScheduler = new DownloadTaskScheduler(downloaders);
            coreCacheManager = new LocalCacheManager(
                ApplicationData.Current.LocalCacheFolder);
        }

        public async Task InitializeAsync(byte[] checkPoint = null)
        {
            try
            {
                await LoadAnnounceUrlsAsync();
                await InitializeDhtEngineAsync();

                if (checkPoint != null)
                {
                    var json = Encoding.UTF8.GetString(checkPoint);
                    var checkPointObject = JsonConvert.DeserializeObject<
                        InnerCheckPoint>(json, new JsonSerializerSettings()
                        {
                            TypeNameHandling = TypeNameHandling.Auto,
                            Error = HandleJsonError,
                        });
                    if (checkPointObject.Tasks != null)
                        foreach (var kvp in checkPointObject.Tasks)
                            tasks.Add(kvp.Value);
                    try { coreCacheManager.Initialize(checkPointObject.CacheManagerCheckPoint); }
                    catch (Exception) { }
                    if (checkPointObject.Downloaders != null)
                        foreach (var kvp in checkPointObject.Downloaders)
                            try { CreateDownloader(kvp.Key, kvp.Value); }
                            catch (Exception) { }
                    if (checkPointObject.Histories != null)
                        foreach (var hist in checkPointObject.Histories)
                            try { histories.Add(hist); }
                            catch (Exception) { }
                }

                taskScheduler.Start();

                D("Initialized");
            }
            catch (Exception e)
            {
                D($"Initialization failed: \n{e.Message}");
            }
        }

        public ISyncableEnumerable<DownloadTask> Tasks => tasks;

        public ISyncableEnumerable<AbstractDownloader> Downloaders => downloaders;

        public ISyncableEnumerable<DownloadHistory> Histories => histories;

        public ISyncableEnumerable<string> AnnounceUrls => announceUrls;

        public MonoTorrent.Client.ClientEngine TorrentEngine => torrentEngine;

        public IStorageFolder CacheFolder => coreCacheManager.CacheFolder;

        public void RemoveHistory(DownloadHistory history) => histories.Remove(history);

        public async Task LoadAnnounceUrlsAsync()
        {
            try
            {
                var lines = (await FileIO.ReadLinesAsync(
                    await StorageUtils.GetOrCreateAnnounceUrlsFileAsync(),
                    Windows.Storage.Streams.UnicodeEncoding.Utf8
                )).Where(url => url.Length > 0);
                announceUrls.Clear();
                foreach (var announceUrl in lines)
                    announceUrls.Add(announceUrl);
                D($"Custom announce URLs loaded, {announceUrls.Count} in total");
            }
            catch (Exception e)
            {
                D($"Custom announce URLs loading failed, {e.Message}");
            }
        }

        public async Task CleanCacheFolderAsync()
        {
            await coreCacheManager.CleanCacheFolderAsync(
                taskKey => downloaders.Any(downloader =>
                    downloader.Status != DownloaderStatus.Completed &&
                    downloader.Status != DownloaderStatus.Disposed &&
                    downloader.DownloadTask.Key.Equals(taskKey))
            );
        }

        public string CreateTask(
            AbstractTarget target,
            IStorageFolder destinationFolder,
            bool isBackgroundDownloadAllowed,
            string customFileName = null,
            DateTime? scheduledDateTime = null)
        {
            string token = RandomUtils.String(8);
            while (tasks.Any(task => task.Key.Equals(token)))
                token = RandomUtils.String(8);

            tasks.Add(new DownloadTask(
                token, target,
                customFileName,
                coreFolderManager.StoreFolder(destinationFolder),
                DateTime.Now,
                isBackgroundDownloadAllowed
            ) { ScheduledStartTime = scheduledDateTime });

            CreateDownloader(token);
            return token;
        }

        private void CreateDownloader(string token, byte[] checkPoint = null)
        {
            AbstractDownloader downloader = null;

            try
            {
                var task = tasks.First(t => t.Key.Equals(token));
                if (task.Target is HttpRangableTarget httpRangableTarget)
                    downloader = new HttpParallelDownloader(
                        task,
                        coreFolderManager,
                        coreCacheManager.GetCacheProviderForTask(token),
                        coreBufferProvider,
                        checkPoint,
                        settingEntries.ThreadNumber);
                else if (task.Target is HttpTarget httpTarget)
                    downloader = new HttpDownloader(
                        task, coreFolderManager,
                        coreCacheManager.GetCacheProviderForTask(token),
                        coreBufferProvider);
                else if (task.Target is TorrentTarget torrentTarget)
                    downloader = new TorrentDownloader(
                        task, torrentEngine, coreFolderManager,
                        coreCacheManager.GetCacheProviderForTask(token),
                        checkPoint: checkPoint,
                        maximumConnections: settingEntries.MaximumConnections,
                        maximumDownloadSpeed: settingEntries.MaximumDownloadSpeed,
                        maximumUploadSpeed: settingEntries.MaximumUploadSpeed,
                        customAnnounceUrls: announceUrls);
            }
            catch (Exception e)
            {
                D($"Downloader with task {token} creation failed, {e.Message}");
            }

            if (downloader != null)
            {
                D($"Downloader with task {token} created");
                downloader.MaximumRetries = settingEntries.MaximumRetries;
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
                if (settingEntries.IsNotificationEnabledWhenTaskCompleted)
                    ToastManager.DownloaderCompletionToast(sender);
            }
            else if (status == DownloaderStatus.Error)
            {
                if (settingEntries.IsNotificationEnabledWhenFailed)
                    ToastManager.DownloaderErrorToast(sender);
            }
            else if (status == DownloaderStatus.Disposed)
            {
                sender.StatusChanged -= AnyDownloader_StatusChanged;
                downloaders.Remove(sender);
            }
        }

        public byte[] ToPersistentByteArray()
        {
            D("Generating persistent byte array...");
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(
                new InnerCheckPoint()
                {
                    Tasks = Tasks.Select(task => new KeyValuePair
                        <string, DownloadTask>(task.Key, task)).ToArray(),
                    Downloaders = Downloaders.Where(downloader =>
                        downloader.Status != DownloaderStatus.Completed &&
                        downloader.Status != DownloaderStatus.Disposed)
                        .Select(downloader =>
                        {
                            byte[] val = null;
                            if (downloader is IPersistable per)
                                try { val = per.ToPersistentByteArray(); } catch (Exception) { }
                            return new KeyValuePair<string, byte[]>(
                                downloader.DownloadTask.Key, val);
                        }).ToArray(),
                    Histories = Histories.ToArray(),
                    CacheManagerCheckPoint = coreCacheManager.ToPersistentByteArray(),
                }, new JsonSerializerSettings() 
                { 
                    TypeNameHandling = TypeNameHandling.Auto,
                    Error = HandleJsonError,
                }));
        }

        public async Task SuspendAsync()
        {
            D("Suspending core...");
            D("Cancelling scheduler...");
            await taskScheduler.StopAsync();
            D("Cleaning unused task entries...");
            CleanTasks();
            D("Cleaning local cache folder...");
            await CleanCacheFolderAsync();
            D("Cancelling downloaders...");
            foreach (var downloader in downloaders)
                if (downloader.Status == DownloaderStatus.Running)
                    downloader.Cancel();
            D("Suspended");
        }

        public void Resume()
        {
            taskScheduler.Start();
            D("Resumed");
        }

        public void Dispose()
        {
            torrentEngine.Dispose();
        }

        private void CleanTasks()
        {
            var toBeDeleted = tasks.Where(task => 
                !downloaders.Any(downloader =>
                downloader.DownloadTask.Key.Equals(task.Key)))
                .ToArray();
            foreach (var task in toBeDeleted)
            {
                D($"Unused task {task.Key} deleted");
                tasks.Remove(task);
            }
        }

        private async Task InitializeDhtEngineAsync()
        {
            try
            {
                var dhtEngine = new DhtEngine(new IPEndPoint(IPAddress.Any, 0));
                await torrentEngine.RegisterDhtAsync(dhtEngine);
                await torrentEngine.DhtEngine.StartAsync();
                D("DhtEngine initialized");
            }
            catch (Exception e)
            {
                D($"DhtEngine initialization failed: {e.Message}");
            }
        }

        private void HandleJsonError(object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
        {
            D($"Failed serializing json: {args.ErrorContext.Error.Message}");
            args.ErrorContext.Handled = true;
        }

        private readonly Settings settingEntries = new Settings();
        private readonly InnerCollection<DownloadTask> tasks = new InnerCollection<DownloadTask>();
        private readonly InnerCollection<AbstractDownloader> downloaders = new InnerCollection<AbstractDownloader>();
        private readonly InnerCollection<DownloadHistory> histories = new InnerCollection<DownloadHistory>();
        private readonly InnerCollection<string> announceUrls = new InnerCollection<string>();
        private readonly LocalFolderManager coreFolderManager = new LocalFolderManager();
        private readonly MonoTorrent.Client.ClientEngine torrentEngine = new MonoTorrent.Client.ClientEngine();
        private readonly SizeLimitedBufferProvider coreBufferProvider = null;
        private readonly DownloadTaskScheduler taskScheduler = null;
        private LocalCacheManager coreCacheManager = null;

        private void D(string text) => Debug.WriteLine($"[{GetType().Name}] {text}");

        private class InnerCheckPoint
        {
            public KeyValuePair<string, DownloadTask>[] Tasks;

            public KeyValuePair<string, byte[]>[] Downloaders;

            public DownloadHistory[] Histories;

            public byte[] CacheManagerCheckPoint;
        }

        private class InnerCollection<T> : ObservableCollection<T>, ISyncableEnumerable<T> { }
    }
}
