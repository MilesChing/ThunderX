using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using TX.Core.Downloaders;
using TX.Core.Models.Progresses;
using TX.Core.Models.Progresses.Interfaces;
using TX.Core.Utils;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace TX.Controls
{
    public sealed partial class DownloaderBar : UserControl, IDownloaderViewable
    {
        public DownloaderBar()
        {
            this.InitializeComponent();
            CapturePointer();
        }

        private void CapturePointer()
        {
            PointerEntered += (sender, e) =>
            {
                if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse
                    || e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Pen)
                    VisualStateManager.GoToState(this, "PointerOver", false);
            };
            PointerExited += (sender, e) =>
                VisualStateManager.GoToState(this, "PointerNormal", false);
            PointerCaptureLost += (sender, e) =>
                VisualStateManager.GoToState(this, "PointerNormal", false);
            PointerCanceled += (sender, e) =>
                VisualStateManager.GoToState(this, "PointerNormal", false);
        }

        public AbstractDownloader Downloader { get; private set; } = null;

        public void BindDownloader(AbstractDownloader downloader)
        {
            if (Downloader != null) 
                ClearDownloaderBinding();

            if (downloader == null)
                return;

            Downloader = downloader;

            FileNameTextBlock.Text = Downloader.DownloadTask.DestinationFileName;

            if (Downloader.Progress is IMeasurableProgress)
            {
                Downloader.Progress.ProgressChanged += MeasurableProgressChanged;
                MeasurableProgressChanged(Downloader.Progress, null);
            }
            else
            {
                Downloader.Progress.ProgressChanged += ProgressChanged;
                ProgressChanged(Downloader.Progress, null);
            }

            Downloader.Speed.Updated += SpeedUpdated;

            Downloader.StatusChanged += StatusChanged;
            StatusChanged(Downloader, Downloader.Status);
        }

        public void ClearDownloaderBinding()
        {
            if (Downloader == null) return;
            Downloader.StatusChanged -= StatusChanged;
            Downloader.Speed.Updated -= SpeedUpdated;
            if (Downloader.Progress is IMeasurableProgress)
                Downloader.Progress.ProgressChanged -= MeasurableProgressChanged;
            else
                Downloader.Progress.ProgressChanged -= ProgressChanged;
            Downloader = null;
        }

        private async void StatusChanged(
            AbstractDownloader downloader, DownloaderStatus status) =>
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                StartButton.IsEnabled = downloader.CanStart;
                CancelButton.IsEnabled = downloader.CanCancel;
                VisualStateManager.GoToState(this, status.ToString(), false);
            });

        private async void SpeedUpdated(SpeedCalculator obj) =>
            await SpeedTextBlock.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, () => {
                    SpeedTextBlock.Text = ((long)obj.Speed).SizedString() + "/s";
                });

        private async void ProgressChanged(IProgress prog, IProgressChangedEventArg _) =>
            await SizeTextBlock.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, () =>
                    SizeTextBlock.Text = prog.DownloadedSize.SizedString()
                );

        private async void MeasurableProgressChanged(IProgress prog, IProgressChangedEventArg _) =>
            await Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, () => {
                    var mprog = prog as IMeasurableProgress;
                    SizeTextBlock.Text = $"{mprog.DownloadedSize.SizedString()} of {mprog.TotalSize.SizedString()}";
                    MainProgressBar.Value = mprog.Progress * 100;
                });

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
}
