using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TX.Core.Downloaders;
using TX.Core.Interfaces;
using TX.Core.Models.Progresses;
using TX.Core.Models.Targets;
using TX.Core.Models.Contexts;
using TX.Core.Utils;
using Windows.Storage;
using EnsureThat;

namespace TX.Core.Downloaders
{
    /// <summary>
    /// HttpDownloader downloads file from a task with HttpTarget.
    /// HttpDownloader linearly transfers data from responded stream to 
    /// destination file, which means data with smaller offset is always 
    /// transfered earlier.
    /// HttpDownloader does not support recovering from break point, 
    /// so it deletes the old temporary file when canceled and always 
    /// starts a new downloading when started.
    /// </summary>
    public class HttpDownloader : AbstractDownloader
    {
        /// <summary>
        /// Construct a HttpDownloader with given task and configurations.
        /// </summary>
        /// <param name="task">Download task, must with HttpTarget.</param>
        /// <param name="folderProvider">Folder provider must not be null.</param>
        /// <param name="cacheProvider">Cache provider must not be null.</param>
        /// <param name="bufferProvider">Buffer provider must not be null.</param>
        public HttpDownloader(
            DownloadTask task,
            IFolderProvider folderProvider,
            ICacheStorageProvider cacheProvider,
            IBufferProvider bufferProvider
        ) : base(task)
        {
            Ensure.That(task.Target is HttpTarget, null,
                opts => opts.WithMessage($"type of {nameof(task.Target)} must be {nameof(HttpTarget)}")
            ).IsTrue();
            Ensure.That(folderProvider, nameof(folderProvider)).IsNotNull();
            Ensure.That(cacheProvider, nameof(cacheProvider)).IsNotNull();
            Ensure.That(bufferProvider, nameof(bufferProvider)).IsNotNull();

            this.folderProvider = folderProvider;
            this.cacheProvider = cacheProvider;
            this.bufferProvider = bufferProvider;

            Progress = new BaseProgress();
            Speed = SharedSpeedCalculatorFactory.NewSpeedCalculator();
            Progress.ProgressChanged += (sender, arg) => Speed.CurrentValue = Progress.DownloadedSize;
        }

        /// <summary>
        /// Download file with cancellation token.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Downloading task.</returns>
        private Task<IStorageFile> DownloadAsync(CancellationToken token) =>
            Task.Run(async () =>
            {
                // create a new cache file
                cacheFile = await cacheProvider.GetCacheFileByTokenAsync(
                    await cacheProvider.NewCacheFileAsync());
                var httpTarget = DownloadTask.Target as HttpTarget;
                var progress = Progress as BaseProgress;
                // copy data from responsed stream to file stream
                using (var client = new HttpClient())
                using (var ostream = await cacheFile.OpenStreamForWriteAsync())
                using (var istream = await client.GetStreamAsync(httpTarget.Uri))
                    await istream.CopyToAsync(ostream, bufferProvider, token,
                        (size) => progress.Increase(size));
                // if canceled, return null
                if (token.IsCancellationRequested) return null;
                // get destination folder
                var folder = await folderProvider.GetFolderFromTokenAsync(
                    DownloadTask.DestinationFolderKey);
                // move cacheFile to destination folder
                await cacheFile.MoveAsync(folder, DownloadTask.DestinationFileName,
                    NameCollisionOption.GenerateUniqueName);
                var res = cacheFile;
                cacheFile = null;
                return res;
            });

        protected override async Task HandleStartAsync()
        {
            await Task.CompletedTask;
            Speed.IsEnabled = true;
            cancellationTokenSource = new CancellationTokenSource();
            downloadTask = new Task(async () =>
            {
                try
                {
                    var file = await DownloadAsync(cancellationTokenSource.Token);
                    if (file != null) ReportCompleted(file);
                    else return;
                }
                catch (Exception e)
                {
                    await ReportErrorAsync(e);
                }
                finally
                {
                    Speed.IsEnabled = false;
                }
            });
            downloadTask.RunSynchronously();
        }

        protected override async Task HandleCancelAsync() {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = null;
            downloadTask?.Wait();
            downloadTask = null;
            if (cacheFile != null)
                await cacheFile.DeleteAsync();
            cacheFile = null;
            ((BaseProgress)Progress).Reset();
        }

        protected override Task HandleDisposeAsync() => HandleCancelAsync();

        private Task downloadTask;
        private IStorageFile cacheFile;
        private CancellationTokenSource cancellationTokenSource;
        private readonly IFolderProvider folderProvider;
        private readonly ICacheStorageProvider cacheProvider;
        private readonly IBufferProvider bufferProvider;
    }
}
