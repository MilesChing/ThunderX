using System;
using System.Threading.Tasks;
using TX.Downloaders;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using TX.Converters;
using System.Diagnostics;

namespace TX.Controls
{
    public sealed partial class DownloadBar : UserControl
    {
        /// <summary>
        /// 空构造函数
        /// </summary>
        public DownloadBar()
        {
            this.InitializeComponent();
            Bar.Value = 0;
            NameBlock.Text = "...";
            SpeedBlock.Text = "";
            SizeBlock.Text = "";
            ProgressBlock.Text = "0%";
            downloader = null;
        }

        /// <summary>
        /// 提供下载器的引用，以显示其信息。
        /// </summary>
        public void SetDownloader(AbstractDownloader dw)
        {
            downloader = dw;

            dw.DownloadProgressChanged += DownloaderDownloadProgressChanged;
            dw.DownloadComplete += DownloadCompleted;
            dw.DownloadError += DisplayError;
            dw.StateChanged += DownloaderStateChanged;

            DownloaderStateChanged(dw.State);
        }

        private async void DownloaderDownloadProgressChanged(Models.Progress progress)
        {
            if (((App)App.Current).InBackground) return;
            int per = (int)((progress.TargetValue == null) ? 0
                : (100f * progress.CurrentValue / progress.TargetValue));
            //更新所有进度显示
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    ProgressBlock.Text = (progress.TargetValue == null) ? "-%" : (per + "%");
                    Bar.Value = per;
                    SizeBlock.Text = StringConverter.GetPrintSize(progress.CurrentValue)
                        + " / " + (progress.TargetValue == null ? "--" :
                        StringConverter.GetPrintSize((long)progress.TargetValue));
                    SpeedBlock.Text = StringConverter.GetPrintSize((long)progress.Speed) + "/s ";
                    if (progress.TargetValue != null && progress.AverageSpeed >= 1)
                    {
                        long time = (long)(((long)progress.TargetValue - progress.CurrentValue) / (progress.AverageSpeed + 0.001));
                        SpeedBlock.Text += Strings.AppResources.GetString("Prediction") + StringConverter.GetPrintTime(time);
                    }
                });
        }

        private async void DownloaderStateChanged(Enums.DownloadState state)
        {
            //在后台运行（挂起或最小化）不更新UI
            if (((App)App.Current).InBackground) return;
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (state == Enums.DownloadState.Pause)
                {
                    PauseButton.IsEnabled = false;
                    PlayButton.IsEnabled = true;
                    DeleteButton.IsEnabled = true;
                    RefreshButton.IsEnabled = true;
                    Bar.ShowPaused = true;
                }
                else if (state == Enums.DownloadState.Error)
                {
                    PauseButton.IsEnabled = false;
                    PlayButton.IsEnabled = false;
                    DeleteButton.IsEnabled = true;
                    RefreshButton.IsEnabled = true;
                    Bar.ShowPaused = true;
                }
                else if (state == Enums.DownloadState.Downloading)
                {
                    PauseButton.IsEnabled = true;
                    PlayButton.IsEnabled = false;
                    DeleteButton.IsEnabled = true;
                    RefreshButton.IsEnabled = true;
                    Bar.ShowPaused = false;
                }
                else if (state == Enums.DownloadState.Done)
                {
                    Bar.Value = 100;
                    ProgressBlock.Text = "100%";
                    PlayButton.IsEnabled = false;
                    PauseButton.IsEnabled = false;
                    DeleteButton.IsEnabled = false;
                    RefreshButton.IsEnabled = false;
                }
                else if (state == Enums.DownloadState.Prepared)
                {
                    HideGlassLabel.Begin();

                    Models.DownloaderMessage message = downloader.Message;
                    NameBlock.Text = message.FileName + message.Extention;
                    PauseButton.IsEnabled = false;
                    PlayButton.IsEnabled = true;
                    DeleteButton.IsEnabled = true;
                    RefreshButton.IsEnabled = false;
                    int per = (int)((message.FileSize == null) ? 0
                        : (100f * message.DownloadSize / message.FileSize));
                    ProgressBlock.Text = (message.FileSize == null) ? "-%" : (per + "%");
                    Bar.Value = per;
                    SizeBlock.Text = StringConverter.GetPrintSize(message.DownloadSize)
                        + " / " + (message.FileSize == null ? "--" :
                        StringConverter.GetPrintSize((long)message.FileSize));
                    SpeedBlock.Text = "-/s ";
                }
            });
        }

        /// <summary>
        /// 控件绑定的下载器
        /// </summary>
        public AbstractDownloader downloader { get; private set; }

        private async void DownloadCompleted(Models.DownloaderMessage message)
        {
            //必须Dispose(), 否则里面的Timer不会停止
            downloader.Dispose();
            await MainPage.Current.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () => { MainPage.Current.DownloadBarCollection.Remove(this); });
        }

        private void DisplayError(Exception e)
        {
            Toasts.ToastManager.ShowSimpleToast(Strings.AppResources.GetString("SomethingWrong"), e.Message);
        }

        private void TopGlassLabel_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
                ShowGlassLabel.Begin();
        }

        private void TopGlassLabel_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
                HideGlassLabel.Begin();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (downloader != null) downloader.Dispose();
            MainPage.Current.DownloadBarCollection.Remove(this);
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            downloader.Start();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            downloader.Pause();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            downloader.Refresh();
        }

        private void TopGlassLabel_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse) return;
            if (TopGlassLabel.Opacity == 0) ShowGlassLabel.Begin();
            else if (TopGlassLabel.Opacity == 1) HideGlassLabel.Begin();
            else return;
        }
    }
}
