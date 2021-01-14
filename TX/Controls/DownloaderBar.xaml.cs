using Microsoft.Toolkit.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using TX.Core.Downloaders;
using TX.Core.Models.Progresses;
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

        public AbstractDownloader Downloader { get; private set; } = null;

        public void BindDownloader(AbstractDownloader downloader)
        {
            if (Downloader != null) 
                ClearDownloaderBinding();

            if (downloader == null)
                return;

            Downloader = downloader;

            FileNameTextBlock.Text = Downloader.DownloadTask.DestinationFileName;

            if (Downloader.Progress is AbstractMeasurableProgress)
            {
                Downloader.Progress.ProgressChanged += MeasurableProgressChanged;
                MeasurableProgressChanged(Downloader.Progress);
            }
            else
            {
                Downloader.Progress.ProgressChanged += ProgressChanged;
                ProgressChanged(Downloader.Progress);
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
            if (Downloader.Progress is AbstractMeasurableProgress)
                Downloader.Progress.ProgressChanged -= MeasurableProgressChanged;
            else
                Downloader.Progress.ProgressChanged -= ProgressChanged;
            Downloader = null;
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
                VisualStateManager.GoToState(this, "PointerOut", false);
            PointerCaptureLost += (sender, e) =>
                VisualStateManager.GoToState(this, "PointerOut", false);
            PointerCanceled += (sender, e) =>
                VisualStateManager.GoToState(this, "PointerOut", false);
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

        private async void ProgressChanged(AbstractProgress obj) =>
            await SizeTextBlock.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, () =>
                    SizeTextBlock.Text = obj.DownloadedSize.SizedString()
                );

        private async void MeasurableProgressChanged(AbstractProgress obj) =>
            await Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, () => {
                    var prog = obj as AbstractMeasurableProgress;
                    SizeTextBlock.Text = "{0} of {1}".AsFormat(
                        prog.DownloadedSize.SizedString(),
                        prog.TotalSize.SizedString());
                    MainProgressBar.Value = prog.Percentage * 100;
                    ProgressTextBlock.Text = prog.Percentage.ToString("0%");
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

        private void DisposeButton_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => Downloader.Dispose());
        }
    }
}
