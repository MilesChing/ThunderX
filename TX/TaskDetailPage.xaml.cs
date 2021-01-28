using Microsoft.Toolkit.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using TX.Controls;
using TX.Core.Downloaders;
using TX.Core.Interfaces;
using TX.Core.Models.Progresses;
using TX.Core.Models.Progresses.Interfaces;
using TX.Core.Models.Targets;
using TX.Core.Utils;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
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
            Progress_Changed(downloader.Progress, null);

            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            BasicLabelCollection.Add(new TaskDetailPageLabel(
                resourceLoader.GetString("CreationTime"),
                downloader.DownloadTask.CreationTime.ToLocalTime().ToString("F")));
            BasicLabelCollection.Add(new TaskDetailPageLabel(
                resourceLoader.GetString("TargetType"),
                downloader.DownloadTask.Target.GetType().Name));
            BasicLabelCollection.Add(new TaskDetailPageLabel(
                resourceLoader.GetString("DownloaderType"),
                downloader.GetType().Name));
            if (downloader.Progress is IMeasurableProgress progress)
                BasicLabelCollection.Add(new TaskDetailPageLabel(
                    resourceLoader.GetString("TotalSize"),
                    progress.TotalSize.SizedString()));
            
            if (downloader.Progress is IVisibleProgress ipv)
            {
                ipv.VisibleRangeListChanged += BindedVisibleRangeListChanged;
                SetVisibleRangeListViewItemsSource(ipv);
            }
        }

        private void BindedVisibleRangeListChanged(IVisibleProgress sender)
        {
             VisibleRangeListView.Dispatcher.RunAsync(
                Windows.UI.Core.CoreDispatcherPriority.Normal,
                    () => SetVisibleRangeListViewItemsSource(sender))
             .AsTask().Wait();
        }

        private void SetVisibleRangeListViewItemsSource(IVisibleProgress progress)
        {
            if (VisibleRangeListView.ItemsSource is IEnumerable<TaskDetailVisibleRangeViewModel> vms)
            {
                foreach (var vm in vms)
                    vm.Dispose();
                VisibleRangeListView.ItemsSource = null;
            }

            if (progress != null)
            {
                VisibleRangeListView.ItemsSource = progress.VisibleRangeList.Select(
                    range => new TaskDetailVisibleRangeViewModel(range, Dispatcher)).ToList();
            }
        }

        public void ClearDownloaderBinding()
        {
            if (Downloader.Progress is IVisibleProgress ipv)
                ipv.VisibleRangeListChanged -= BindedVisibleRangeListChanged;
            SetVisibleRangeListViewItemsSource(null);

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

            BasicLabelCollection.Clear();
            Downloader = null;
        }

        private async void Progress_Changed(IProgress sender, IProgressChangedEventArg _)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    if (sender is IMeasurableProgress mprogress)
                    {
                        SizeTextBlock.Text = "{0} / {1}".AsFormat(
                            mprogress.DownloadedSize.SizedString(),
                            mprogress.TotalSize.SizedString());
                        ProgressTextBlock.Text = mprogress.Progress.ToString("0%");
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
                SpeedTextBlock.Text = ((long)sender.Speed).SizedString();
                if (Downloader != null)
                {
                    ErrorNumberTextBlock.Text = Downloader.Errors.Count.ToString();
                    RetryNumberTextBlock.Text = Downloader.Retries.ToString();
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
                            ErrorTextBlock.Text = "{0}: {1}".AsFormat(
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

    class TaskDetailVisibleRangeViewModel : IVisibleRange, IDisposable
    {
        public TaskDetailVisibleRangeViewModel(IVisibleRange range, CoreDispatcher dispatcher)
        {
            ParentRange = range;
            Dispatcher = dispatcher;
            Progress = range.Progress * 100;
            Total = range.Total * 400;
            range.PropertyChanged += ParentRangePropertyChanged;
        }

        private async void ParentRangePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Progress = ParentRange.Progress * 100;
            Total = ParentRange.Total * 400;
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => PropertyChanged(this, e));
        }

        public float Progress { get; private set; }

        public float Total { get; private set; }

        public IVisibleRange ParentRange { get; private set; }

        private readonly CoreDispatcher Dispatcher;

        public void Dispose()
        {
            if (ParentRange != null)
            {
                ParentRange.PropertyChanged -= ParentRangePropertyChanged;
                ParentRange = null;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };
    }
}
