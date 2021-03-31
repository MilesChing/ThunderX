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
        }

        public async Task InitializeAsync(byte[] checkPoint = null)
        {
            try
            {
                await LoadAnnounceUrlsAsync();
                await InitializeDhtEngineAsync();
                InitializeCacheFolder();

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
                            tasks.Add(kvp.Key, kvp.Value);
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

                StartScheduler();
                D("Initialized");
            }
            catch (Exception e)
            {
                D($"Initialization failed: \n{e.Message}");
            }
        }

        public IReadOnlyDictionary<string, DownloadTask> Tasks => tasks;

        public IReadOnlyCollection<AbstractDownloader> Downloaders => downloaders;

        public INotifyCollectionChanged ObservableDownloaders => downloaders;

        public IReadOnlyCollection<DownloadHistory> Histories => histories;

        public INotifyCollectionChanged ObservableHistories => histories;

        public MonoTorrent.Client.ClientEngine TorrentEngine => torrentEngine;

        public IReadOnlyList<string> CustomAnnounceURLs => customAnnounceUrls;

        public IStorageFolder CacheFolder => coreCacheManager.CacheFolder;

        public void RemoveHistory(DownloadHistory history) =>
            histories.Remove(history);

        public string CreateTask(
            AbstractTarget target,
            IStorageFolder destinationFolder,
            bool isBackgroundDownloadAllowed,
            string customFileName = null,
            DateTime? scheduledDateTime = null)
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
            )
            {
                ScheduledStartTime = scheduledDateTime
            };

            CreateDownloader(token);

            return token;
        }

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
                }, new JsonSerializerSettings() 
                { 
                    TypeNameHandling = TypeNameHandling.Auto,
                    Error = HandleJsonError,
                }));
        }

        public void Suspend()
        {
            D("Suspending core...");
            D("Cancelling scheduler...");
            schedulerCancellationTokenSource?.Cancel();
            schedulerCancellationTokenSource = null;
            schedulerRunningTask?.Wait();
            schedulerRunningTask = null;
            D("Cleaning unused task entries...");
            CleanTasks();
            D("Cleaning local cache folder...");
            Task.Run(async () => await CleanCacheFolderAsync()).Wait();
            D("Cancelling downloaders...");
            foreach (var downloader in downloaders)
                if (downloader.Status == DownloaderStatus.Running)
                    downloader.Cancel();
            D("Suspended");
        }

        public void Resume()
        {
            StartScheduler();
            D("Resumed");
        }

        public void Dispose()
        {
            torrentEngine.Dispose();
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
            }
            catch (Exception e)
            {
                D($"DhtEngine initialization failed: {e.Message}");
            }
        }

        private void InitializeCacheFolder()
        {
            coreCacheManager = new LocalCacheManager(
                ApplicationData.Current.LocalCacheFolder);
            D("Core cache manager initialized");
        }

        private void StartScheduler()
        {
            schedulerCancellationTokenSource?.Cancel();
            schedulerCancellationTokenSource = new CancellationTokenSource();
            schedulerRunningTask = RunSchedulerAsync(schedulerCancellationTokenSource.Token);
        }

        private async Task RunSchedulerAsync(CancellationToken token)
        {
            D("[Scheduler] Started");
            while (!token.IsCancellationRequested)
            {
                try
                {
                    DateTime now = DateTime.Now;
                    D($"[Scheduler] <{now:T}> Checking scheduled downloaders");
                    TimeSpan nextDelayTimeSpan = SchedulerTimerInterval;
                    foreach (var downloader in downloaders)
                    {
                        var scheduledTime = downloader.DownloadTask.ScheduledStartTime;
                        if (scheduledTime != null)
                        {
                            if (scheduledTime.Value <= now)
                            {
                                D($"[Scheduler] Start {downloader.GetType().Name} with task {downloader.DownloadTask.Key}");
                                downloader.Start();
                                downloader.DownloadTask.ScheduledStartTime = null;
                            }
                            else
                            {
                                var timeBeforeScheduled = scheduledTime.Value - now;
                                if (timeBeforeScheduled < nextDelayTimeSpan)
                                    nextDelayTimeSpan = timeBeforeScheduled;
                            }
                        }
                    }
                    try { await Task.Delay(nextDelayTimeSpan, token); }
                    catch (TaskCanceledException) { }
                } catch (Exception) { }
            }
            D("[Scheduler] Cancelled");
        }

        private void HandleJsonError(object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
        {
            D($"Failed serializing json: {args.ErrorContext.Error.Message}");
            args.ErrorContext.Handled = true;
        }

        private readonly Settings settingEntries = new Settings();
        private readonly Dictionary<string, DownloadTask> tasks = new Dictionary<string, DownloadTask>();
        private readonly ObservableCollection<AbstractDownloader> downloaders = new ObservableCollection<AbstractDownloader>();
        private readonly ObservableCollection<DownloadHistory> histories = new ObservableCollection<DownloadHistory>();
        private readonly LocalFolderManager coreFolderManager = new LocalFolderManager();
        private readonly MonoTorrent.Client.ClientEngine torrentEngine = new MonoTorrent.Client.ClientEngine();
        private readonly SizeLimitedBufferProvider coreBufferProvider = null;
        private LocalCacheManager coreCacheManager = null;
        private readonly List<string> customAnnounceUrls = new List<string>();
        private Task schedulerRunningTask = null;
        private CancellationTokenSource schedulerCancellationTokenSource = null;
        private static readonly TimeSpan SchedulerTimerInterval = TimeSpan.FromMinutes(1.0);

        private void D(string text) => Debug.WriteLine($"[{GetType().Name}] {text}");

        private class InnerCheckPoint
        {
            public KeyValuePair<string, DownloadTask>[] Tasks;

            public KeyValuePair<string, byte[]>[] Downloaders;

            public DownloadHistory[] Histories;

            public byte[] CacheManagerCheckPoint;
        }
    }
}
