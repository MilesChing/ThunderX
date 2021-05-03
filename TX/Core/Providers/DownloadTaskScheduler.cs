using EnsureThat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TX.Core.Downloaders;

namespace TX.Core.Providers
{
    class DownloadTaskScheduler
    {
        public DownloadTaskScheduler(IEnumerable<AbstractDownloader> downloaders)
        {
            Ensure.That(downloaders, nameof(downloaders)).IsNotNull();
            this.downloaders = downloaders;
        }

        public void Start()
        {
            if (schedulerTask != null) return;
            
            cancellationSource = new CancellationTokenSource();
            var token = cancellationSource.Token;

            schedulerTask = new Task(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        TimeSpan sleepTime = TimeSpan.FromMinutes(1);
                        DateTime now = DateTime.Now;
                        foreach (var downloader in downloaders)
                        {
                            var scheduledStartTime = downloader.
                                DownloadTask.ScheduledStartTime;
                            if (scheduledStartTime.HasValue)
                            {
                                if (scheduledStartTime.Value <= now)
                                    downloader.Start();
                                else
                                {
                                    var lastTime = scheduledStartTime.Value - now;
                                    if (lastTime < sleepTime)
                                        sleepTime = lastTime;
                                }
                            }
                        }
                        await Task.Delay(sleepTime, token);
                    }
                    catch (Exception) { }
                }
            });

            schedulerTask.RunSynchronously();
        }

        public async Task StopAsync()
        {
            if (cancellationSource != null) cancellationSource.Cancel();
            if (schedulerTask != null) await schedulerTask;
            schedulerTask = null;
            cancellationSource = null;
        }

        private CancellationTokenSource cancellationSource;
        private Task schedulerTask;
        private readonly IEnumerable<AbstractDownloader> downloaders;
    }
}
