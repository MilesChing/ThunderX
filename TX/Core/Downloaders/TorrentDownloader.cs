using EnsureThat;
using MonoTorrent.BEncoding;
using MonoTorrent.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TX.Core.Interfaces;
using TX.Core.Models.Contexts;
using TX.Core.Models.Progresses;
using TX.Core.Models.Progresses.Interfaces;
using TX.Core.Models.Targets;
using TX.Core.Utils;
using Windows.Storage;
using Windows.System;

namespace TX.Core.Downloaders
{
    /// <summary>
    /// TorrentDownloader takes Task with TorrentTarget.
    /// Test of MagnetTarget has not been completed yet. 
    /// </summary>
    public class TorrentDownloader : AbstractDownloader, IPersistable
    {
        /// <summary>
        /// Construct a TorrentDownloader.
        /// </summary>
        /// <param name="task">Download task, must with HttpRangedTarget.</param>
        /// <param name="engine">Client engine of MonoTorrent which provides torrent and magnet downloading.</param>
        /// <param name="folderProvider">Folder provider must not be null.</param>
        /// <param name="cacheProvider">Cache provider must not be null.</param>
        /// <param name="checkPoint">Set the downloader to start at given checkPoint.</param>
        /// <param name="maximumConnections">
        /// The maximum number of concurrent open connections for this torrent. 
        /// Defaults to 60.</param>
        /// <param name="maximumDownloadSpeed">
        /// The maximum download speed, in bytes per second, for this torrent. 
        /// A value of 0 means unlimited. Defaults to 0.</param>
        /// <param name="maximumUploadSpeed">
        /// The maximum upload speed, in bytes per second, for this torrent. 
        /// A value of 0 means unlimited. defaults to 0.</param>
        /// <param name="uploadSlots">
        /// The number of peers which can be uploaded to concurrently for this torrent. 
        /// A value of 0 means unlimited. defaults to 8.</param>
        public TorrentDownloader(
            DownloadTask task,
            ClientEngine engine,
            IFolderProvider folderProvider,
            ICacheStorageProvider cacheProvider,
            byte[] checkPoint = null,
            int maximumConnections = 60,
            int maximumDownloadSpeed = 0,
            int maximumUploadSpeed = 0,
            int uploadSlots = 8
        ) : base (task)
        {
            Ensure.That(task.Target is TorrentTarget || task.Target is MagnetTarget).IsTrue();
            Ensure.That(cacheProvider, nameof(cacheFolder)).IsNotNull();
            Ensure.That(folderProvider, nameof(folderProvider)).IsNotNull();
            Ensure.That(engine, nameof(engine)).IsNotNull();
            Ensure.That(maximumConnections).IsGt(0);
            Ensure.That(maximumDownloadSpeed).IsGte(0);
            Ensure.That(maximumUploadSpeed).IsGte(0);
            Ensure.That(uploadSlots).IsGt(0);

            this.engine = engine;
            this.cacheProvider = cacheProvider;
            this.folderProvider = folderProvider;
            this.maximumConnections = maximumConnections;
            this.maximumDownloadSpeed = maximumDownloadSpeed;
            this.maximumUploadSpeed = maximumUploadSpeed;
            this.uploadSlots = uploadSlots;

            long? totalSize = 0;
            if (task.Target is TorrentTarget tt)
                totalSize = tt.Torrent.Files.Sum(file =>
                    file.Priority == MonoTorrent.Priority.DoNotDownload ? 0 : file.Length);
            else if (task.Target is MagnetTarget mt)
                totalSize = mt.Link.Size;

            if (totalSize == null)
                Progress = new BaseProgress();
            else 
                Progress = new BaseMeasurableProgress((long)totalSize);

            Speed = SharedSpeedCalculatorFactory.NewSpeedCalculator();

            if (checkPoint != null)
                ApplyCheckPoint(checkPoint);

            Progress.ProgressChanged += (sender, arg) => Speed.CurrentValue = Progress.DownloadedSize;
        }

        public byte[] ToPersistentByteArray()
        {
            long? totalSize = null;
            byte[] fastResumeData = null;

            try
            {
                if (manager?.Torrent != null)
                    totalSize = manager.Torrent.Files.Sum(file =>
                        file.Priority == MonoTorrent.Priority.DoNotDownload ? 0 : file.Length);
                using (var stream = new MemoryStream())
                {
                    manager?.SaveFastResume()?.Encode(stream);
                    fastResumeData = stream.ToArray();
                }
            }
            catch (Exception) { }

            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(
                new InnerCheckPoint()
                {
                    TaskKey = DownloadTask.Key,
                    CacheFolderToken = cacheFolderToken,
                    DownloadedSize = Progress.DownloadedSize,
                    FastResumeData = fastResumeData,
                    TotalSize = totalSize,
                }
            ));
        }

        public PeerManager Peers => manager?.Peers;

        /// <summary>
        /// The number of peers that this torrent instance is connected to
        /// </summary>
        public int OpenConnections => manager?.OpenConnections ?? 0;

        protected override async Task CancelAsync()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = null;
            downloadTask?.Wait();
            downloadTask = null;
            await manager?.PauseAsync();
        }

        protected override Task DisposeAsync()
            => Task.Run(async () =>
            {
                if (manager != null)
                {
                    manager.TorrentStateChanged -= ManagerTorrentStateChanged;
                    await manager.StopAsync();
                    manager.Dispose();
                    await engine.Unregister(manager);
                    manager = null;
                }
            });

        protected override async Task StartAsync()
        {
            await ValidateCacheFolderAsync();

            if (manager == null)
            {
                TorrentSettings settings = new TorrentSettings()
                {
                    MaximumConnections = maximumConnections,
                    MaximumDownloadSpeed = maximumDownloadSpeed,
                    MaximumUploadSpeed = maximumUploadSpeed,
                    UploadSlots = uploadSlots,
                };
                if (DownloadTask.Target is TorrentTarget tt)
                    manager = new TorrentManager(
                        tt.Torrent, 
                        cacheFolder.Path,
                        settings);
                if (DownloadTask.Target is MagnetTarget mt)
                    manager = new TorrentManager(mt.Link,
                        cacheFolder.Path,
                        settings, 
                        // using local cache folder to save torrent
                        ApplicationData.Current.LocalCacheFolder.Path);
                if (fastResume != null) manager.LoadFastResume(fastResume);
                manager.TorrentStateChanged += ManagerTorrentStateChanged;
                RegisterDebugMessages();
                await engine.Register(manager);
            }

            await manager.StartAsync();

            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            downloadTask = Task.Run(() =>
            {
                Speed.IsEnabled = true;
                try
                {
                    var prog = Progress as BaseProgress;
                    var mprog = Progress as BaseMeasurableProgress;
                    long? totalSize = null;
                    if (manager.Torrent != null)
                        totalSize = manager.Torrent.Files.Sum(file =>
                            file.Priority == MonoTorrent.Priority.DoNotDownload ? 0 : file.Length);
                    while (!token.IsCancellationRequested)
                    {
                        try
                        {
                            Task.Delay(ProgressUpdateInterval).Wait();

                            long nowVal = manager.Monitor.DataBytesDownloaded;
                            if (totalSize.HasValue)
                                nowVal = (long)(manager.PartialProgress / 100.0 * totalSize);
                            if (mprog != null)
                                nowVal = Math.Min(nowVal, mprog.TotalSize);
                            long delta = nowVal - prog.DownloadedSize;
                            if (delta < 0)
                            {
                                delta = nowVal;
                                prog.Reset();
                            }
                            prog.Increase(delta);
                        }
                        catch (Exception) { }
                    }
                }
                finally
                {
                    Speed.IsEnabled = false;
                }
            });
        }

        private void RegisterDebugMessages()
        {
            manager.PeerConnected += (o, e) => D($"Connection succeeded: {e.Peer.Uri}");
            
            manager.ConnectionAttemptFailed += (o, e) =>
                D($"Connection failed: {e.Peer.ConnectionUri} - {e.Reason} - {e.Peer.AllowedEncryption}");
            
            // Every time a piece is hashed, this is fired.
            manager.PieceHashed += delegate (object o, PieceHashedEventArgs e) {
                D($"Piece Hashed: {e.PieceIndex} - {(e.HashPassed ? "Pass" : "Fail")}");
            };

            // Every time the state changes (Stopped -> Seeding -> Downloading -> Hashing) this is fired
            manager.TorrentStateChanged += delegate (object o, TorrentStateChangedEventArgs e) {
                D($"OldState: {e.OldState} NewState: {e.NewState}");
            };

            // Every time the tracker's state changes, this is fired
            manager.TrackerManager.AnnounceComplete += (sender, e) =>
                D($"{e.Successful}: {e.Tracker}");
        }

        private async void ManagerTorrentStateChanged(object sender, TorrentStateChangedEventArgs e)
        {
            var ma = sender as TorrentManager;
            D($"Torrent manager state changed <{e.NewState}> , old: <{e.OldState}>");
            switch (e.NewState)
            {
                case TorrentState.Error:
                    ReportError(ma.Error.Exception, false);
                    break;
                case TorrentState.Seeding:
                    cancellationTokenSource?.Cancel();
                    cancellationTokenSource = null;

                    await Task.Delay(5000);
                    
                    downloadTask?.Wait();
                    downloadTask = null;
                    
                    await manager.StopAsync();

                    try
                    {
                        var folder = await folderProvider.GetFolderFromTokenAsync(DownloadTask.DestinationFolderKey);
                        var targetFolder = await folder.CreateFolderAsync(
                            DownloadTask.DestinationFileName, 
                            CreationCollisionOption.GenerateUniqueName);
                        await cacheFolder.MoveContentToAsync(targetFolder);
                        cacheFolder = null;
                        ReportCompleted(targetFolder);
                    }
                    catch (Exception ex) { ReportError(ex, true); }
                    break;
            }
        }

        private async Task ValidateCacheFolderAsync()
        {
            if (cacheFolder == null)
            {
                if (cacheFolderToken == string.Empty)
                {
                    cacheFolderToken = await cacheProvider.NewCacheFolderAsync();
                    cacheFolder = cacheProvider.GetCacheFolderByToken(cacheFolderToken);
                    ((BaseProgress)Progress).Reset();
                }
                else
                {
                    cacheFolder = cacheProvider.GetCacheFolderByToken(cacheFolderToken);
                    if (cacheFolder == null)
                    {
                        cacheFolderToken = await cacheProvider.NewCacheFolderAsync();
                        cacheFolder = (IStorageFolder)cacheProvider.GetCacheFolderByToken(cacheFolderToken);
                        ((BaseProgress)Progress).Reset();
                    }
                }
            }
        }

        private void ApplyCheckPoint(byte[] checkPointByteArray)
        {
            checkPoint = JsonConvert.DeserializeObject<InnerCheckPoint>(
                Encoding.ASCII.GetString(checkPointByteArray));
            Ensure.That(checkPoint.TaskKey, nameof(checkPoint.TaskKey)).IsEqualTo(DownloadTask.Key);

            fastResume = new FastResume(
                BEncodedDictionary.Decode<BEncodedDictionary>(
                    checkPoint.FastResumeData));

            if (checkPoint.TotalSize.HasValue)
                Progress = new BaseMeasurableProgress(checkPoint.TotalSize.Value);

            (Progress as BaseProgress).Reset();
            (Progress as BaseProgress).Increase(checkPoint.DownloadedSize);
            cacheFolderToken = checkPoint.CacheFolderToken;
        }

        private void D(string text) => Debug.WriteLine($"[{nameof(TorrentDownloader)} with task {DownloadTask.Key}] {text}");

        private class InnerCheckPoint
        {
            public string TaskKey;
            public string CacheFolderToken;
            public byte[] FastResumeData;
            public long DownloadedSize;
            public long? TotalSize;
        };

        private FastResume fastResume = null;
        private IStorageFolder cacheFolder = null;
        private string cacheFolderToken = string.Empty;
        private InnerCheckPoint checkPoint = null;
        private TorrentManager manager = null;
        private readonly ICacheStorageProvider cacheProvider = null;
        private readonly IFolderProvider folderProvider = null;
        private readonly ClientEngine engine = null;
        private CancellationTokenSource cancellationTokenSource = null;
        private Task downloadTask = null;

        private readonly int maximumConnections = 60;
        private readonly int maximumDownloadSpeed = 0;
        private readonly int maximumUploadSpeed = 0;
        private readonly int uploadSlots = 8;
        private static TimeSpan ProgressUpdateInterval = TimeSpan.FromSeconds(0.2);
    }
}
