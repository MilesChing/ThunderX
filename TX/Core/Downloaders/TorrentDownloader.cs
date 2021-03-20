using EnsureThat;
using MonoTorrent;
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
        /// <param name="customAnnounceUrls">Custom announce URLs.</param>
        public TorrentDownloader(
            DownloadTask task,
            ClientEngine engine,
            IFolderProvider folderProvider,
            ICacheStorageProvider cacheProvider,
            byte[] checkPoint = null,
            int maximumConnections = 60,
            int maximumDownloadSpeed = 0,
            int maximumUploadSpeed = 0,
            int uploadSlots = 8,
            IEnumerable<string> customAnnounceUrls = null
        ) : base (task)
        {
            Ensure.That(task.Target is TorrentTarget).IsTrue();
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
            this.customAnnounceUrls = customAnnounceUrls;

            TorrentTarget realTarget = (TorrentTarget)task.Target;

            Progress = new BaseMeasurableProgress(
                realTarget.Torrent.Files.Sum(
                    file => file.Priority == Priority.DoNotDownload ? 0 : file.Length));
            Speed = SharedSpeedCalculatorFactory.NewSpeedCalculator();
            Progress.ProgressChanged += (sender, arg) => Speed.CurrentValue = Progress.DownloadedSize;

            if (checkPoint != null) ApplyCheckPoint(checkPoint);
        }

        public byte[] ToPersistentByteArray()
        {
            byte[] fastResumeData = null;

            try
            {
                using (var stream = new MemoryStream())
                {
                    manager?.SaveFastResume()?.Encode(stream);
                    fastResumeData = stream.ToArray();
                }
            }
            catch (Exception) { }

            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(
                new InnerCheckPoint()
                {
                    TaskKey = DownloadTask.Key,
                    CacheFolderToken = cacheFolderToken,
                    DownloadedSize = Progress.DownloadedSize,
                    FastResumeData = fastResumeData,
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

                var realTarget = (TorrentTarget) DownloadTask.Target;
                var torrent = realTarget.Torrent;

                if (customAnnounceUrls != null && !torrent.AnnounceUrls.IsReadOnly)
                    torrent.AnnounceUrls.Add(new RawTrackerTier(customAnnounceUrls));

                manager = new TorrentManager(
                    torrent,
                    cacheFolder.Path,
                    settings);

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
                    var mprog = Progress as BaseMeasurableProgress;
                    while (!token.IsCancellationRequested)
                    {
                        try
                        {
                            Task.Delay(ProgressUpdateInterval).Wait();
                            long nowVal = (long)(manager.PartialProgress / 100.0 * mprog.TotalSize);
                            long delta = nowVal - mprog.DownloadedSize;
                            if (delta < 0)
                            {
                                delta = nowVal;
                                mprog.Reset();
                            }
                            mprog.Increase(delta);
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
                D($"Piece hashed: {e.PieceIndex} - {(e.HashPassed ? "Pass" : "Fail")}");
            };

            // Every time the tracker's state changes, this is fired
            manager.TrackerManager.AnnounceComplete += (sender, e) =>
                D($"{(e.Successful ? "Tracker connected" : "Tracker connection failed")}: {e.Tracker}");
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
                    downloadTask?.Wait();
                    downloadTask = null;
                    await manager.StopAsync();
                    break;
                case TorrentState.Stopped:
                    if (((BaseMeasurableProgress)Progress).IsCompleted)
                    {
                        try
                        {
                            D("Downloading completed, creating destination folder...");
                            var folder = await folderProvider.GetFolderFromTokenAsync(DownloadTask.DestinationFolderKey);
                            var targetFolder = await folder.CreateFolderAsync(
                                DownloadTask.DestinationFileName,
                                CreationCollisionOption.GenerateUniqueName);
                            D($"Destination folder created <{targetFolder.Path}>");
                            D("Coping files from cache folder to destination folder...");
                            await cacheFolder.CopyContentToAsync(targetFolder);
                            D("Deleting cache folder...");
                            await cacheFolder.DeleteAsync();
                            cacheFolder = null;
                            ReportCompleted(targetFolder);
                        }
                        catch (Exception ex) { ReportError(ex, true); }
                    }
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
                        cacheFolder = cacheProvider.GetCacheFolderByToken(cacheFolderToken);
                        ((BaseProgress)Progress).Reset();
                    }
                }
            }
        }

        private void ApplyCheckPoint(byte[] checkPointByteArray)
        {
            checkPoint = JsonConvert.DeserializeObject<InnerCheckPoint>(
                Encoding.UTF8.GetString(checkPointByteArray));
            Ensure.That(checkPoint.TaskKey, nameof(checkPoint.TaskKey)).IsEqualTo(DownloadTask.Key);

            if (checkPoint.FastResumeData != null)
            {
                try
                {
                    fastResume = new FastResume(
                        BEncodedValue.Decode<BEncodedDictionary>(
                            checkPoint.FastResumeData));
                }
                catch (Exception) { }
            }

            (Progress as BaseProgress).Reset();
            (Progress as BaseProgress).Increase(checkPoint.DownloadedSize);
            cacheFolderToken = checkPoint.CacheFolderToken;
        }

        private class InnerCheckPoint
        {
            public string TaskKey;
            public string CacheFolderToken;
            public byte[] FastResumeData;
            public long DownloadedSize;
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
        private readonly IEnumerable<string> customAnnounceUrls = null;

        private readonly static TimeSpan ProgressUpdateInterval = TimeSpan.FromSeconds(0.2);
    }
}
