using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Utils;
using Windows.ApplicationModel.Background;
using Windows.UI.Xaml;

namespace TX.Background
{
    /// <summary>
    /// CoreBackgroundTask starts available tasks in background once triggered.
    /// </summary>
    class CoreBackgroundTask : AbstractBackgroundTask
    {
        public override string Name => GetType().Name;

        public override async void Run(IBackgroundTaskInstance taskInstance)
        {
            if (Window.Current?.Content != null)
            {
                D("Application is running in foreground, aborted this background execution");
                return;
            }

            DateTime startTime = DateTime.Now;
            var deferral = taskInstance.GetDeferral();
            try
            {
                var Current = ((App)App.Current);
                await Current.WaitForInitializingAsync();
                var activeDownloaders = Current.Core.Downloaders.Where(
                    d => d.DownloadTask.IsBackgroundDownloadAllowed && d.CanStart).ToArray();
                foreach (var downloader in activeDownloaders)
                    downloader.Start();
                if (activeDownloaders.Length == 0)
                {
                    taskInstance.Task.Unregister(false);
                    D($"No downloader found. Unregistered background task");
                }
                else
                {
                    D($"{activeDownloaders.Length} downloader(s) activated");
                    await Task.Delay(startTime + TimeSpan.FromSeconds(9 * 60 + 55) - DateTime.Now);
                }
            }
            finally
            {
                D($"Finished after {DateTime.Now - startTime}");
                deferral.Complete();
            }
        }

        public override BackgroundTaskBuilder Build()
        {
            var settings = new Settings();
            var builder = new BackgroundTaskBuilder() { Name = Name };
            builder.SetTrigger(new MaintenanceTrigger(settings.BackgroundTaskFreshnessTime, true));
            builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));
            if (settings.RunBackgroundTaskOnlyWhenUserNotPresent)
                builder.AddCondition(new SystemCondition(SystemConditionType.UserNotPresent));
            if (settings.RunOnlyWhenBackgroundWorkCostNotHigh)
                builder.AddCondition(new SystemCondition(SystemConditionType.BackgroundWorkCostNotHigh));
            return builder;
        }

        private void D(string message) => Debug.WriteLine($"[{GetType().Name}] {message}");
    }
}
