using EnsureThat;
using Microsoft.Toolkit.Uwp.Notifications;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Core.Downloaders;
using TX.Core.Models.Contexts;
using TX.Core.Utils;
using Windows.ApplicationModel.Activation;
using Windows.Data.Xml.Dom;
using Windows.Storage;
using Windows.System;
using Windows.UI.Notifications;

namespace TX.Utils
{
    public static class ToastManager
    {
        /// <summary>
        /// Launch a toast for a downloader's failure.
        /// </summary>
        /// <param name="downloader">The failed downloader.</param>
        public static void DownloaderErrorToast(AbstractDownloader downloader)
        {
            var xml = new ToastContentBuilder()
                .AddText($"Task Failed: {downloader.DownloadTask.DestinationFileName}")
                .AddText($"{downloader.Errors.FirstOrDefault().Message}")
                .GetToastContent().GetXml();
            ToastNotificationManager.CreateToastNotifier().Show(
                new ToastNotification(xml));
            D($"Error toast for task {downloader.DownloadTask.Key} launched");
        }

        /// <summary>
        /// Launch a toast for a downloader's completion.
        /// </summary>
        /// <param name="downloader">The completed downloader.</param>
        public static async void DownloaderCompletionToast(AbstractDownloader downloader)
        {
            var xml = new ToastContentBuilder()
                .AddText($"Task Completed: {downloader.DownloadTask.DestinationFileName}")
                .AddText($"File Size {(await downloader.Result.GetSizeAsync()).SizedString()}")
                .AddText($"Duration {downloader.Speed.RunningTime:hh\\:mm\\:ss}")
                .AddButton("Open", ToastActivationType.Foreground,
                    EncodeActivationCommand(
                        ActivationCommandLaunchStorageItem, 
                        downloader.Result.Path)
                )
                .AddButton("Open Directory", ToastActivationType.Foreground,
                    EncodeActivationCommand(
                        ActivationCommandLaunchStorageItem, 
                        Path.GetDirectoryName(downloader.Result.Path))
                )
                .GetToastContent().GetXml();
            ToastNotificationManager.CreateToastNotifier().Show(
                new ToastNotification(xml));
            D($"Completion toast for task {downloader.DownloadTask.Key} launched");
        }

        /// <summary>
        /// Handle the activation of application by toast.
        /// </summary>
        /// <param name="args">Arguments of the activation</param>
        public static async Task HandleToastActivationAsync(ToastNotificationActivatedEventArgs args)
        {
            var command = DecodeActivationCommand(args?.Argument);
            D($"Command decoded: <{string.Join(' ', command)}>");

            if (command.Length > 0)
            {
                switch (command[0])
                {
                    case ActivationCommandLaunchStorageItem:
                        foreach (string path in command.Skip(1))
                        {
                            try
                            {
                                var item = await StorageUtils.GetStorageItemAsync(path);
                                if (item is StorageFolder folder)
                                    await Launcher.LaunchFolderAsync(folder);
                                if (item is StorageFile file)
                                    await Launcher.LaunchFileAsync(file);
                                D($"{path} launched");
                            }
                            catch (Exception e) { D($"{path} launching failed: {e.Message}"); }
                        }
                        RecoverApplicationState(args);
                        break;
                    default:
                        RecoverApplicationState(args);
                        break;
                }
            }
            else
            {
                D($"Empty command, abort");
                RecoverApplicationState(args);
            }
        }

        private static string EncodeActivationCommand(params string[] commands) =>
            JsonConvert.SerializeObject(commands);

        private static string[] DecodeActivationCommand(string command)
        {
            try
            {
                var res = JsonConvert.DeserializeObject<string[]>(command);
                if (res != null) return res;
                else return Array.Empty<string>();
            }
            catch (Exception) { return Array.Empty<string>(); }
        }

        private static void RecoverApplicationState(ToastNotificationActivatedEventArgs args)
        {
            if (args.PreviousExecutionState != ApplicationExecutionState.Running)
            {
                D($"Previous execution state: {args.PreviousExecutionState}, application exit");
                App.Current.Exit();
            }
        }

        private static void D(string message) => Debug.WriteLine($"[{nameof(ToastManager)}] {message}");

        private const string ActivationCommandLaunchStorageItem = "LAUNCH_STORAGE_ITEM";
    }
}
