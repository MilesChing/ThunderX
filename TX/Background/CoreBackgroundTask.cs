using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Utils;
using Windows.ApplicationModel.Background;

namespace TX.Background
{
    public static class CoreBackgroundTask
    {
        public static async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();
            try
            {
                var Current = ((App)App.Current);
                await Current.WaitForInitializingAsync();
                var activeDownloaders = Current.Core.Downloaders.Where(
                    d => d.DownloadTask.IsBackgroundDownloadAllowed).ToArray();
                foreach (var downloader in activeDownloaders)
                    downloader.Start();
                if (activeDownloaders.Length == 0)
                {
                    taskInstance.Task.Unregister(false);
                    Debug.WriteLine($"[{nameof(CoreBackgroundTask)}] no downloader found. Unregister background task.");
                }
                else Debug.WriteLine($"[{nameof(CoreBackgroundTask)}] {activeDownloaders.Length} downloader(s) activated");
            }
            finally
            {
                deferral.Complete();
            }
        }

        public static void RefreshBackgroundTask(
            bool backgroundTaskEnabled = true,
            uint backgroundTaskFreshnessTime = 15,
            bool runOnlyWhenUserNotPresent = false,
            bool runOnlyWhenBackgroundWorkCostNotHigh = false)
        {
            var thisTaskName = nameof(CoreBackgroundTask);
            var exists = BackgroundTaskRegistration.AllTasks.FirstOrDefault(
                task => task.Value.Name.Equals(thisTaskName));
            exists.Value?.Unregister(false);

            if (backgroundTaskEnabled)
            {
                var builder = new BackgroundTaskBuilder();
                builder.Name = thisTaskName;
                builder.SetTrigger(new TimeTrigger(backgroundTaskFreshnessTime, false));
                builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));
                if (runOnlyWhenUserNotPresent)
                    builder.AddCondition(new SystemCondition(SystemConditionType.UserNotPresent));
                if (runOnlyWhenBackgroundWorkCostNotHigh)
                    builder.AddCondition(new SystemCondition(SystemConditionType.BackgroundWorkCostNotHigh));
                builder.Register();
            }
        }
    }
}
