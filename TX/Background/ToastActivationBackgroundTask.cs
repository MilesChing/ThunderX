using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Utils;
using Windows.ApplicationModel.Background;
using Windows.UI.Notifications;

namespace TX.Background
{
    /// <summary>
    /// ToastActivationBackgroundTask handles the activation of application
    /// in background caused by interactions with toast notifications.
    /// It uses ToastManager to decode interaction arguments and ignores the
    /// returned actions, since it is running in background.
    /// </summary>
    class ToastActivationBackgroundTask : AbstractBackgroundTask
    {
        public override string Name => GetType().Name;

        public override async void Run(IBackgroundTaskInstance taskInstance)
        {
            if (taskInstance.TriggerDetails is ToastNotificationActionTriggerDetail details)
            {
                var deferral = taskInstance.GetDeferral();
                try { await ToastManager.HandleToastActivationAsync(details.Argument); }
                finally { deferral.Complete(); }
            }
            else
            {
                D("Trigger type must be ToastNotificationActionTriggerDetail, aborted");
                return;
            }
        }

        public override BackgroundTaskBuilder Build()
        {
            var builder = new BackgroundTaskBuilder { Name = Name };
            builder.SetTrigger(new ToastNotificationActionTrigger());
            return builder;
        }

        private void D(string message) => Debug.WriteLine($"[{GetType().Name}] {message}");
    }
}
