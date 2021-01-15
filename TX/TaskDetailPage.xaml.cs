using Microsoft.Toolkit.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using TX.Controls;
using TX.Core.Downloaders;
using TX.Core.Interfaces;
using TX.Core.Models.Progresses;
using TX.Core.Models.Targets;
using TX.Core.Utils;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace TX
{
    public sealed partial class TaskDetailPage : Page, IDownloaderViewable
    {
        public TaskDetailPage()
        {
            this.InitializeComponent();
        }

        public AbstractDownloader Downloader { get; private set; } = null;

        public void BindDownloader(AbstractDownloader downloader)
        {
            if (Downloader != null)
                ClearDownloaderBinding();
            Downloader = downloader;
            if (downloader == null) return;

            TaskNameTextBlock.Text = downloader.DownloadTask.DestinationFileName;
            if (downloader.DownloadTask.Target is HttpTarget target)
                TaskHyperlink.Text = target.Uri.ToString();

            DisposeButton.IsEnabled = true;
            Downloader.StatusChanged += StatusChanged;
            StatusChanged(downloader, downloader.Status);

            Downloader.Speed.Updated += Speed_Updated;
            Speed_Updated(downloader.Speed);

            Downloader.Progress.ProgressChanged += Progress_Changed;
            Progress_Changed(downloader.Progress);

            if (Downloader.Progress is AbstractMeasurableProgress mprogress)
            {
                for (int i = 0; i < 100; ++i)
                    ProgressCollection.Add(false);
            }

            BasicLabelCollection.Add(new TaskDetailPageLabel("Creation Time",
                downloader.DownloadTask.CreationTime.ToLocalTime().ToString("F")));
            BasicLabelCollection.Add(new TaskDetailPageLabel("Target Type",
                downloader.DownloadTask.Target.GetType().Name));
            BasicLabelCollection.Add(new TaskDetailPageLabel("Downloader Type",
                downloader.GetType().Name));
            if (downloader.Progress is AbstractMeasurableProgress progress)
                BasicLabelCollection.Add(new TaskDetailPageLabel("Total Size",
                    progress.TotalSize.SizedString()));
        }

        public void ClearDownloaderBinding()
        {
            Downloader.StatusChanged -= StatusChanged;
            Downloader.Speed.Updated -= Speed_Updated;
            Downloader.Progress.ProgressChanged -= Progress_Changed;

            TaskNameTextBlock.Text = string.Empty;
            TaskHyperlink.Text = string.Empty;
            StartButton.IsEnabled =
                CancelButton.IsEnabled =
                DisposeButton.IsEnabled = false;
            StatusTextBlock.Text = string.Empty;

            ProgressTextBlock.Text = string.Empty;
            DownloadTimeTextBlock.Text = string.Empty;
            SpeedTextBlock.Text = string.Empty;
            SizeTextBlock.Text = string.Empty;
            MainProgressBar.Value = 0;

            BasicLabelCollection.Clear();
            ProgressCollection.Clear();
            Downloader = null;
        }

        private async void Progress_Changed(AbstractProgress sender)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    if (sender is AbstractMeasurableProgress mprogress)
                    {
                        SizeTextBlock.Text = "{0} of {1}".AsFormat(
                            mprogress.DownloadedSize.SizedString(),
                            mprogress.TotalSize.SizedString());
                        MainProgressBar.Value = mprogress.Percentage * 100;
                        ProgressTextBlock.Text = mprogress.Percentage.ToString("0%");
                    }
                    else SizeTextBlock.Text = 
                        sender.DownloadedSize.SizedString();
                });
        }

        private async void Speed_Updated(SpeedCalculator sender)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
            () =>
            {
                DownloadTimeTextBlock.Text = sender.RunningTime.ToString(@"hh\:mm\:ss");
                SpeedTextBlock.Text = "Speed: " + ((long)sender.Speed).SizedString() + "/s";
                if (Downloader != null)
                {
                    RuntimeTextBlock.Text = "{0} Errors, {1} Retries".AsFormat(
                        Downloader.Errors.Count, Downloader.Retries);
                }
            });
        }

        private async void StatusChanged(AbstractDownloader sender, DownloaderStatus status)
            => await Dispatcher.RunAsync(
                Windows.UI.Core.CoreDispatcherPriority.Normal,
                () => {
                    VisualStateManager.GoToState(this, status.ToString(), true);
                    StartButton.IsEnabled = sender.CanStart;
                    CancelButton.IsEnabled = sender.CanCancel;
                    StatusTextBlock.Text = status.ToString();

                    if (status == DownloaderStatus.Disposed)
                        DisposeButton.IsEnabled = false;
                    
                    if (status == DownloaderStatus.Error)
                    {
                        var exp = sender.Errors.FirstOrDefault();
                        if (exp != null)
                        {
                            ErrorStackPanel.Visibility = Visibility.Visible;
                            ErrorTextBlock.Text = "Error {0}: {1}".AsFormat(
                                exp.HResult, exp.GetType().Name);
                            ErrorDetailTextBlock.Text = exp.ToString();
                        }
                    }
                    else
                    {
                        ErrorStackPanel.Visibility = Visibility.Collapsed;
                    }
                });

        private readonly ObservableCollection<TaskDetailPageLabel> BasicLabelCollection
            = new ObservableCollection<TaskDetailPageLabel>();
        private readonly ObservableCollection<bool> ProgressCollection
            = new ObservableCollection<bool>();

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is AbstractDownloader)
                BindDownloader((AbstractDownloader)e.Parameter);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            ClearDownloaderBinding();
            base.OnNavigatedFrom(e);
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (Downloader.CanStart)
                Downloader.Start();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (Downloader.CanCancel)
                Downloader.Cancel();
        }

        private void DeleteConfirmation_Click(object sender, RoutedEventArgs e)
        {
            DeleteConfirmationFlyout.Hide();
            Task.Run(() => Downloader.Dispose());
        }
    }

    class TaskDetailPageLabel
    {
        public TaskDetailPageLabel(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public readonly string Key;

        public readonly string Value;
    }
}
