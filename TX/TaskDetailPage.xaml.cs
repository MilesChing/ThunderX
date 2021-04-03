﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using TX.Controls;
using TX.Core.Downloaders;
using TX.Core.Interfaces;
using TX.Core.Models.Contexts;
using TX.Core.Models.Progresses;
using TX.Core.Models.Progresses.Interfaces;
using TX.Core.Models.Targets;
using TX.Core.Utils;
using Windows.ApplicationModel.DataTransfer;
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
            if (downloader == Downloader)
                return;
            if (Downloader != null)
                ClearDownloaderBinding();
            Downloader = downloader;
            if (downloader == null) return;

            Downloader.DownloadTask.PropertyChanged += DownloadTask_PropertyChanged;
            DownloadTask_PropertyChanged(Downloader.DownloadTask, null);

            TaskNameTextBlock.Text = downloader.DownloadTask.DestinationFileName;
            if (downloader.DownloadTask.Target is HttpTarget httpTarget)
                TaskHyperlink.Text = httpTarget.Uri.ToString();
            else if (downloader.DownloadTask.Target is TorrentTarget torrentTarget)
                TaskHyperlink.Text = torrentTarget.DisplayedUri.ToString();

            DisposeButton.IsEnabled = true;
            Downloader.StatusChanged += StatusChanged;
            StatusChanged(downloader, downloader.Status);

            Downloader.Progress.ProgressChanged += ProgressChanged;
            ProgressChanged(downloader.Progress, null);

            Downloader.Speed.Updated += SpeedUpdated;
            SpeedUpdated(downloader.Speed);

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
                VisibleRangePanel.Visibility = Visibility.Visible;
            }
            else VisibleRangePanel.Visibility = Visibility.Collapsed;
        }

        private async void DownloadTask_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is DownloadTask task)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    string targetVisualState = "Unscheduled";
                    if (task.ScheduledStartTime.HasValue)
                    {
                        targetVisualState = "Scheduled";
                        ScheduledTimeText.Text = task.ScheduledStartTime.Value.ToString("f");
                    }

                    ScheduleTimePicker.SelectedTime = null;
                    ScheduleDatePicker.SelectedDate = null;
                    VisualStateManager.GoToState(this, targetVisualState, false);
                });
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
            VisibleRangePanel.Visibility = Visibility.Collapsed;
            if (Downloader.Progress is IVisibleProgress ipv)
                ipv.VisibleRangeListChanged -= BindedVisibleRangeListChanged;
            SetVisibleRangeListViewItemsSource(null);

            Downloader.DownloadTask.PropertyChanged -= DownloadTask_PropertyChanged;
            Downloader.StatusChanged -= StatusChanged;
            Downloader.Speed.Updated -= SpeedUpdated;
            Downloader.Progress.ProgressChanged -= ProgressChanged;

            TaskNameTextBlock.Text = string.Empty;
            TaskHyperlink.Text = string.Empty;
            StartButton.IsEnabled =
                CancelButton.IsEnabled =
                DisposeButton.IsEnabled = false;
            StatusTextBlock.Text = string.Empty;

            ProgressTextBlock.Text = string.Empty;
            DownloadTimeTextBlock.Text = string.Empty;

            BasicLabelCollection.Clear();
            DynamicLabelCollection.Clear();
            
            Downloader = null;
        }

        private async void ProgressChanged(IProgress sender, IProgressChangedEventArg _)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    if (sender is IMeasurableProgress mprogress)
                    {
                        UpdateIntoDynamicLabelCollection(ProgressText, 
                            $"{mprogress.DownloadedSize.SizedString()} / {mprogress.TotalSize.SizedString()}");
                        ProgressTextBlock.Text = mprogress.Progress.ToString("0%");
                    }
                    else
                        UpdateIntoDynamicLabelCollection(ProgressText, 
                            sender.DownloadedSize.SizedString());
                });
        }

        private async void SpeedUpdated(SpeedCalculator sender)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                DownloadTimeTextBlock.Text = sender.RunningTime.ToString(@"hh\:mm\:ss");
                UpdateIntoDynamicLabelCollection(SpeedText, 
                    $"{((long)sender.Speed).SizedString()} / s");

                if (Downloader != null)
                {
                    UpdateIntoDynamicLabelCollection(
                        $"{ErrorsText} / {RetriesText}", 
                        $"{Downloader.Errors.Count} / {Downloader.Retries}");

                    if (Downloader.Progress is IMeasurableProgress mprogress)
                    {
                        double averageSpeed = sender.AverageSpeed;
                        if (averageSpeed > 0.0)
                        {
                            double remainBytes = (mprogress.TotalSize - mprogress.DownloadedSize);
                            double secondsPrediction = remainBytes / averageSpeed;
                            UpdateIntoDynamicLabelCollection(RemainingTimeText,
                                $"{TimeSpan.FromSeconds(secondsPrediction):hh\\:mm\\:ss}");
                        }
                    }

                    if (Downloader is TorrentDownloader td)
                    {
                        UpdateIntoDynamicLabelCollection(OpenConnectionsText, 
                            td.OpenConnections.ToString());
                        UpdateIntoDynamicLabelCollection(AvailablePeersText, 
                            $"{td.Peers?.Available ?? 0}");
                    }
                }
            });
        }

        private async void StatusChanged(AbstractDownloader sender, DownloaderStatus status)
            => await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () => {
                    StartButton.IsEnabled = sender.CanStart;
                    CancelButton.IsEnabled = sender.CanCancel;
                    StatusTextBlock.Text = status.ToString();
                    
                    if (status == DownloaderStatus.Error)
                    {
                        var exp = sender.Errors.FirstOrDefault();
                        if (exp != null)
                        {
                            ErrorTextBlock.Text = $"{exp.HResult}: {exp.GetType().Name}";
                            ErrorDetailTextBlock.Text = exp.ToString();
                        }
                    }

                    VisualStateManager.GoToState(this, status.ToString(), true);
                });

        private void UpdateIntoDynamicLabelCollection(string key, string value)
        {
            var res = DynamicLabelCollection.FirstOrDefault(lab => lab.Key.Equals(key));
            if (res == null) DynamicLabelCollection.Add(new TaskDetailPageLabel(key, value));
            else res.Value = value;
        }

        private readonly ObservableCollection<TaskDetailPageLabel> BasicLabelCollection
            = new ObservableCollection<TaskDetailPageLabel>();
        private readonly ObservableCollection<TaskDetailPageLabel> DynamicLabelCollection
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

        private void SchedulerActionButton_Click(object sender, RoutedEventArgs e)
        {
            const string Unscheduled = nameof(Unscheduled);
            const string Scheduling = nameof(Scheduling);
            const string Scheduled = nameof(Scheduled);
            switch (SchedulingStateGroup.CurrentState?.Name) 
            {
                case null:
                    VisualStateManager.GoToState(this, "Unscheduled", true);
                    break;
                case Scheduling:
                    VisualStateManager.GoToState(this, "Unscheduled", true);
                    break;
                case Unscheduled:
                    VisualStateManager.GoToState(this, "Scheduling", true);
                    break;
                case Scheduled:
                    Downloader.DownloadTask.ScheduledStartTime = null;
                    break;
            }
        }

        private void ScheduleTimePicker_SelectedTimeChanged(TimePicker sender, TimePickerSelectedValueChangedEventArgs args)
            => HandleSelectedDateTimeChanged();

        private void ScheduleDatePicker_SelectedDateChanged(DatePicker sender, DatePickerSelectedValueChangedEventArgs args)
            => HandleSelectedDateTimeChanged();

        private void HandleSelectedDateTimeChanged()
        {
            if (ScheduleDatePicker.SelectedDate.HasValue &&
                ScheduleTimePicker.SelectedTime.HasValue)
                Downloader.DownloadTask.ScheduledStartTime =
                    ScheduleDatePicker.SelectedDate.Value.Date +
                    ScheduleTimePicker.SelectedTime.Value;
        }

        private void CopyItem_Click(object sender, RoutedEventArgs e)
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(TaskHyperlink.Text);
            Clipboard.SetContent(dataPackage);
        }

        private static readonly string ProgressText = Windows.ApplicationModel.Resources
            .ResourceLoader.GetForCurrentView().GetString("Progress");
        private static readonly string SpeedText = Windows.ApplicationModel.Resources
            .ResourceLoader.GetForCurrentView().GetString("Speed");
        private static readonly string ErrorsText = Windows.ApplicationModel.Resources
            .ResourceLoader.GetForCurrentView().GetString("Errors");
        private static readonly string RetriesText = Windows.ApplicationModel.Resources
            .ResourceLoader.GetForCurrentView().GetString("Retries");
        private static readonly string OpenConnectionsText = Windows.ApplicationModel.Resources
            .ResourceLoader.GetForCurrentView().GetString("OpenConnections");
        private static readonly string AvailablePeersText = Windows.ApplicationModel.Resources
            .ResourceLoader.GetForCurrentView().GetString("AvailablePeers");
        private static readonly string RemainingTimeText = Windows.ApplicationModel.Resources
            .ResourceLoader.GetForCurrentView().GetString("RemainingTime");
    }

    class TaskDetailPageLabel : INotifyPropertyChanged
    {
        public TaskDetailPageLabel(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public string Key
        {
            get => key;
            set
            {
                key = value;
                OnPropertyChanged();
            }
        }
        private string key;

        public string Value 
        {
            get => val;
            set
            {
                val = value;
                OnPropertyChanged();
            }
        }
        private string val;

        public void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler PropertyChanged = (sender, e) => { };
    }

    class TaskDetailVisibleRangeViewModel : IVisibleRange, IDisposable
    {
        public TaskDetailVisibleRangeViewModel(IVisibleRange range, CoreDispatcher dispatcher)
        {
            ParentRange = range;
            Dispatcher = dispatcher;
            range.PropertyChanged += ParentRangePropertyChanged;
        }

        private async void ParentRangePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => PropertyChanged(this, e));
        }

        public float Progress => ParentRange == null ? 0.0f : ParentRange.Progress * 100.0f;

        public float Total
        {
            get
            {
                if (ParentRange == null) return 0.0f;
                float total = ParentRange.Total;
                total = Math.Max(total, 0.01f);
                total = Math.Min(total, 0.2f);
                return total * 2000.0f;
            }
        }

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
