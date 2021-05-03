using System;
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
using TX.Resources.Strings;

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

            BasicLabelCollection.Add(new TaskDetailPageLabel(CreationTimeText,
                downloader.DownloadTask.CreationTime.ToLocalTime().ToString("F")));
            BasicLabelCollection.Add(new TaskDetailPageLabel(TargetTypeText,
                downloader.DownloadTask.Target.GetType().Name));
            BasicLabelCollection.Add(new TaskDetailPageLabel(DownloaderTypeText,
                downloader.GetType().Name));
            if (downloader.Progress is IMeasurableProgress progress)
                BasicLabelCollection.Add(new TaskDetailPageLabel(TotalSizeText,
                    progress.TotalSize.SizedString()));
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

        public void ClearDownloaderBinding()
        {
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

        private async void DeleteConfirmation_Click(object sender, RoutedEventArgs e)
        {
            DeleteConfirmationFlyout.Hide();
            await  Downloader.DisposeAsync();
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

        private static readonly string ProgressText = Loader.Get("Progress");
        private static readonly string SpeedText = Loader.Get("Speed");
        private static readonly string ErrorsText = Loader.Get("Errors");
        private static readonly string RetriesText = Loader.Get("Retries");
        private static readonly string OpenConnectionsText = Loader.Get("OpenConnections");
        private static readonly string AvailablePeersText = Loader.Get("AvailablePeers");
        private static readonly string RemainingTimeText = Loader.Get("RemainingTime");
        private static readonly string CreationTimeText = Loader.Get("CreationTime");
        private static readonly string TargetTypeText = Loader.Get("TargetType");
        private static readonly string DownloaderTypeText = Loader.Get("DownloaderType");
        private static readonly string TotalSizeText = Loader.Get("TotalSize");
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
}
