﻿using EnsureThat;
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
using Windows.ApplicationModel.Resources;
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
                .AddAppLogoOverride(new Uri("ms-appx:///Assets/IconWarning.png"))
                .AddText($"{DownloaderErrorTitlePrefix}{downloader.DownloadTask.DestinationFileName}")
                .AddText($"{downloader.Errors.FirstOrDefault().Message}")
                .AddToastActivationInfo(
                    EncodeActivationCommand(
                        ActivationCommandNavigateToTaskDetail,
                        downloader.DownloadTask.Key
                    ), ToastActivationType.Foreground
                )
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
                .AddAppLogoOverride(new Uri("ms-appx:///Assets/IconComplete.png"))
                .AddText($"{DownloaderCompletionTitlePrefix}{downloader.DownloadTask.DestinationFileName}")
                .AddText($"{DownloaderCompletionDownloaded} {(await downloader.Result.GetSizeAsync()).SizedString()}")
                .AddText($"{DownloaderCompletionDuration} {downloader.Speed.RunningTime:hh\\:mm\\:ss}")
                .AddToastActivationInfo(
                    EncodeActivationCommand(
                        ActivationCommandNavigateToTaskHistory,
                        downloader.DownloadTask.Key
                    ), ToastActivationType.Foreground
                )
                .GetToastContent().GetXml();
            ToastNotificationManager.CreateToastNotifier().Show(
                new ToastNotification(xml));
            D($"Completion toast for task {downloader.DownloadTask.Key} launched");
        }

        /// <summary>
        /// Launch a toast reporting failure handling protocol activation.
        /// </summary>
        /// <param name="e">The exception captured during handling.</param>
        public static void ProtocolActivationErrorToast(Exception e)
        {
            var xml = new ToastContentBuilder()
                .AddAppLogoOverride(new Uri("ms-appx:///Assets/IconWarning.png"))
                .AddText(ProtocolActivationErrorTitle)
                .AddText(e.Message)
                .GetToastContent().GetXml();
            ToastNotificationManager.CreateToastNotifier().Show(
                new ToastNotification(xml));
            D($"Error toast for protocol activation exception launched");
        }

        /// <summary>
        /// Handle the activation of application by toast.
        /// </summary>
        /// <param name="argument">Argument of the activation</param>
        /// <returns>Actions to be done after mainpage has been navigated to.</returns>
        public static void HandleToastActivation(string argument)
        {
            var command = DecodeActivationCommand(argument);
            D($"Command decoded: <{string.Join(' ', command)}>");
            var startUpManager = ((App)App.Current).StupActionManager;
            try
            {
                switch (command.FirstOrDefault())
                {
                    case ActivationCommandNavigateToTaskDetail:
                        startUpManager.Register(() =>
                        {
                            var taskKey = command.Skip(1).FirstOrDefault();
                            if (taskKey != null)
                            {
                                var downloader = ((App)App.Current).Core.Downloaders.FirstOrDefault(
                                    d => d.DownloadTask.Key.Equals(taskKey));
                                if (downloader != null)
                                {
                                    D($"{downloader.GetType().Name} found with task {taskKey}");
                                    MainPage.Current.NavigateDownloaderDetailPage(downloader);
                                }
                                else
                                {
                                    D($"Downloader with task {taskKey} not found, checking history");
                                    bool historyExist = ((App)App.Current).Core.Histories.Any(
                                        hist => string.Equals(hist.TaskKey, taskKey));
                                    if (historyExist)
                                    {
                                        D("Found history record, navigate to history page");
                                        MainPage.Current.NavigateHistoryPage();
                                    }
                                    else D("History record not found, abort");
                                }
                            }
                            else D("Command format illegal: no task key");
                        });
                        break;
                    case ActivationCommandNavigateToTaskHistory:
                        startUpManager.Register(() =>
                        {
                            var taskKey = command.Skip(1).FirstOrDefault();
                            if (taskKey != null)
                            {
                                MainPage.Current.NavigateHistoryPage(taskKey);
                            }
                            else D("Command format illegal: no task key");
                        });
                        break;
                    default:
                        D($"Unrecognized command, abort");
                        break;
                }
            }
            catch (Exception) { }
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

        private static void D(string message) => Debug.WriteLine($"[{nameof(ToastManager)}] {message}");

        private const string ActivationCommandNavigateToTaskDetail = "NAVIGATE_TO_TASK_DETAIL";
        private const string ActivationCommandNavigateToTaskHistory = "NAVIGATE_TO_TASK_HISTORY";
        // this resource loader must be created here, since it will be called in the background
        private readonly static ResourceLoader RSLoader = new ResourceLoader();
        private readonly static string DownloaderErrorTitlePrefix = RSLoader.GetString("Toast_DownloaderError_TitlePrefix");
        private readonly static string DownloaderCompletionTitlePrefix = RSLoader.GetString("Toast_DownloaderCompletion_TitlePrefix");
        private readonly static string DownloaderCompletionDownloaded = RSLoader.GetString("Toast_DownloaderCompletion_Downloaded");
        private readonly static string DownloaderCompletionDuration = RSLoader.GetString("Toast_DownloaderCompletion_Duration");
        private readonly static string DownloaderCompletionOpen = RSLoader.GetString("Toast_DownloaderCompletion_Open");
        private readonly static string DownloaderCompletionOpenFolder = RSLoader.GetString("Toast_DownloaderCompletion_OpenFolder");
        private readonly static string ProtocolActivationErrorTitle = RSLoader.GetString("Toast_ProtocolActivationError_Title");
    }
}
