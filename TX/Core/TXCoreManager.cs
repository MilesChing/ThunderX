using Microsoft.Toolkit.Extensions;
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
using System.Threading.Tasks;
using System.Timers;
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
                settingEntries.MemoryLimit, 512L * 1024L);
            schedulerTimer.Interval = TimeSpan.FromMinutes(5).TotalMilliseconds;
            schedulerTimer.AutoReset = true;
            schedulerTimer.Elapsed += (sender, e) => CheckSchedulerList();
        }

        public async Task InitializeAsync(byte[] checkPoint = null)
        {
            try
            {
                await LoadAnnounceUrlsAsync();
                await InitializeDhtEngineAsync();
                schedulerTimer.Start();

                if (checkPoint != null)
                {
                    var json = Encoding.ASCII.GetString(checkPoint);
                    var checkPointObject = JsonConvert.DeserializeObject<InnerCheckPoint>(json,
                        new JsonSerializerSettings() { 
                            TypeNameHandling = TypeNameHandling.All
                        });
                    if (checkPointObject.Tasks != null)
                        foreach (var kvp in checkPointObject.Tasks) 
                            tasks.Add(kvp.Key, kvp.Value);
                    coreCacheManager.Initialize(checkPointObject.CacheManagerCheckPoint);
                    if (checkPointObject.Downloaders != null)
                        foreach (var kvp in checkPointObject.Downloaders) 
                            CreateDownloader(kvp.Key, kvp.Value);
                    if (checkPointObject.Histories != null)
                        foreach (var hist in checkPointObject.Histories) 
                            histories.Add(hist);
                    if (checkPointObject.Schedules != null)
                        foreach (var kvp in checkPointObject.Schedules) 
                            schedulerList.Add(kvp.Key, kvp.Value);
                }

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

        public MonoTorrent.Client.ClientEngine TorrentEngine => torrentEngine;

        public IReadOnlyList<string> CustomAnnounceURLs => customAnnounceUrls;

        public void RemoveHistory(DownloadHistory history) =>
            histories.Remove(history);

        public string CreateTask(
            AbstractTarget target,
            IStorageFolder destinationFolder,
            bool isBackgroundDownloadAllowed,
            string customFileName = null)
        {
            string token = RandomUtils.String(8);
            while (tasks.ContainsKey(token))
                token = RandomUtils.String(8);

            tasks[token] = new DownloadTask(
                token, target,
                customFileName,
                coreFolderManager.StoreFolder(destinationFolder),
                DateTime.Now,
                isBackgroundDownloadAllowed
            );

            CreateDownloader(token);

            return token;
        }

        public void ScheduleTask(string key, DateTime startTime) => schedulerList.Add(startTime, key);

        public async Task LoadAnnounceUrlsAsync()
        {
            try
            {
                customAnnounceUrls.Clear();
                customAnnounceUrls.AddRange(
                    (await FileIO.ReadLinesAsync(
                        await StorageUtils.GetOrCreateAnnounceUrlsFileAsync(),
                        Windows.Storage.Streams.UnicodeEncoding.Utf8
                    )).Where(url => url.Length > 0)
                );
                D($"Custom announce URLs loaded, {customAnnounceUrls.Count} in total");
            }
            catch (Exception e)
            {
                D($"Custom announce URLs loading failed, {e.Message}");
            }
        }

        private void CreateDownloader(string token, byte[] checkPoint = null)
        {
            if (!tasks.TryGetValue(token, out DownloadTask task)) return;
            
            AbstractDownloader downloader = null;

            try
            {
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
                        customAnnounceUrls: customAnnounceUrls);
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
                {
                    ToastManager.ShowDownloadCompleteToastAsync(
                        "Task Completed", sender.DownloadTask.DestinationFileName,
                        sender.Result.Path, Path.GetDirectoryName(sender.Result.Path));
                }
            }
            else if (status == DownloaderStatus.Error)
            {
                if (settingEntries.IsNotificationEnabledWhenFailed)
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
            D("Generating persistent byte array...");
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(
                new InnerCheckPoint()
                {
                    Tasks = Tasks.ToArray(),
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
                    Schedules = schedulerList.ToArray(),
                }, 
                new JsonSerializerSettings()
                {
                    TypeNameHandling = TypeNameHandling.All
                }));
        }

        public void Suspend()
        {
            D("Suspending core...");
            CleanTasks();
            Task.Run(async () => await CleanCacheFolderAsync()).Wait();
            D("Cancelling downloaders...");
            foreach (var downloader in downloaders)
                if (downloader.Status == DownloaderStatus.Running)
                {
                    ScheduleTask(downloader.DownloadTask.Key, DateTime.MinValue);
                    downloader.Cancel();
                }
            D("Stop scheduler timer");
            schedulerTimer.Stop();
            D("Suspended");
        }

        public void Resume()
        {
            D("Resuming...");
            schedulerTimer.Start();
            D("Resumed");
        }

        public void Dispose()
        {
            torrentEngine.Dispose();
            schedulerTimer.Dispose();
        }

        public async Task CleanCacheFolderAsync()
        {
            await coreCacheManager.CleanCacheFolderAsync(
                taskKey => Downloaders.Any(
                    downloader => 
                        downloader.Status != DownloaderStatus.Completed &&
                        downloader.Status != DownloaderStatus.Disposed &&
                        downloader.DownloadTask.Key.Equals(taskKey))
            );
        }

        private void CleanTasks()
        {
            var toBeDeleted = tasks.Select(task => task.Key).Where(
                key => !downloaders.Any(downloader =>
                    downloader.DownloadTask.Key.Equals(key))).ToArray();
            foreach (var key in toBeDeleted)
            {
                D($"Unused task {key} deleted");
                tasks.Remove(key);
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
            } catch (Exception e) 
            {
                D($"DhtEngine initialization failed: {e.Message}");
            }
        }

        private void CheckSchedulerList()
        {
            D("Checking scheduler list...");
            var now = DateTime.Now;
            while (schedulerList.Count > 0)
            {
                var f = schedulerList.First();
                if (f.Key <= now)
                {
                    foreach (var downloader in downloaders.Where(
                        down => down.DownloadTask.Key.Equals(f.Value)))
                    {
                        downloader.Start();
                        D($"Start {downloader.GetType().Name} with task {f.Value} at {now}, scheduled time: {f.Key}");
                    }
                    schedulerList.Remove(f.Key);
                }
                else break;
            }
        }

        private readonly Settings settingEntries = new Settings();
        private readonly Dictionary<string, DownloadTask> tasks = new Dictionary<string, DownloadTask>();
        private readonly ObservableCollection<AbstractDownloader> downloaders = new ObservableCollection<AbstractDownloader>();
        private readonly ObservableCollection<DownloadHistory> histories = new ObservableCollection<DownloadHistory>();
        private readonly LocalCacheManager coreCacheManager = new LocalCacheManager();
        private readonly LocalFolderManager coreFolderManager = new LocalFolderManager();
        private readonly MonoTorrent.Client.ClientEngine torrentEngine = new MonoTorrent.Client.ClientEngine();
        private readonly SizeLimitedBufferProvider coreBufferProvider = null;
        private readonly List<string> customAnnounceUrls = new List<string>();
        private readonly SortedList<DateTime, string> schedulerList = new SortedList<DateTime, string>();
        private readonly Timer schedulerTimer = new Timer();

        private void D(string text) => Debug.WriteLine($"[{GetType().Name}] {text}");

        private class InnerCheckPoint
        {
            public KeyValuePair<string, DownloadTask>[] Tasks;

            public KeyValuePair<string, byte[]>[] Downloaders;

            public DownloadHistory[] Histories;

            public byte[] CacheManagerCheckPoint;

            public KeyValuePair<DateTime, string>[] Schedules;
        }
    }
}
