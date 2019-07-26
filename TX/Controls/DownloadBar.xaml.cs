﻿using System;
using System.Threading.Tasks;
using TX.Downloaders;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using TX.Converters;
using System.Diagnostics;
using Windows.System;
using Windows.Storage;
using System.IO;
using TX.Enums;

namespace TX.Controls
{
    public sealed partial class DownloadBar : UserControl
    {
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
            //在后台运行（挂起或最小化）不更新UI
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
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                Debug.WriteLine("State Change: " + state.ToString());
                VisualStateManager.GoToState(this, state.ToString(), false);
                if (state == Enums.DownloadState.Done)
                {
                    SizeBlock.Text = StringConverter.GetPrintSize(downloader.Message.DownloadSize);
                    SpeedBlock.Text = downloader.Message.FolderPath;

                    Models.DownloaderMessage message = downloader.Message;
                    NameBlock.Text = message.FileName + message.Extention;
                }
                else if (state == Enums.DownloadState.Prepared)
                {
                    Models.DownloaderMessage message = downloader.Message;
                    NameBlock.Text = message.FileName + message.Extention;
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

        public AbstractDownloader downloader { get; private set; }

        private async void DownloadCompleted(Models.DownloaderMessage message)
        {
            await MainPage.Current.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () => {
                    var collection = MainPage.Current.DownloadBarCollection;
                    int index = collection.IndexOf(this);
                    for (int i = index + 1; i<collection.Count; ++i)
                        if(collection[i].downloader.Message.IsDone)
                        {
                            collection.Move(index, i - 1);
                            return;
                        }
                    //没有return证明没有完成的任务记录，要把this移动到结尾
                    collection.Move(index, collection.Count - 1);
                });
        }

        private void DisplayError(Exception e)
        {
            Toasts.ToastManager.ShowSimpleToast(Strings.AppResources.GetString("SomethingWrong"), e.Message);
        }

        private void TopGlassLabel_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
                ShowGlassLabel_Begin();
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
            HideGlassLabel.Begin();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            downloader.Pause();
            HideGlassLabel.Begin();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            downloader.Refresh();
            HideGlassLabel.Begin();
        }

        private void TopGlassLabel_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse) return;
            if (TopGlassLabel.Opacity == 0) ShowGlassLabel_Begin();
            else if (TopGlassLabel.Opacity == 1) HideGlassLabel.Begin();
            else return;
        }

        private async void FileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = Path.Combine(downloader.Message.FolderPath,
                                downloader.Message.FileName + downloader.Message.Extention);
                var file = await StorageFile.GetFileFromPathAsync(path);

                StorageTools.StorageManager.LaunchFileAsync(file);
            }
            catch(Exception)
            {
                Toasts.ToastManager.ShowSimpleToast(Strings.AppResources.GetString("SomethingWrong"),
                    Strings.AppResources.GetString("FileNotExist"));
            }

            HideGlassLabel.Begin();
        }

        private async void FolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var folder = await StorageFolder.GetFolderFromPathAsync(downloader.Message.FolderPath);
                StorageTools.StorageManager.LaunchFolderAsync(folder);
            }
            catch (Exception)
            {
                Toasts.ToastManager.ShowSimpleToast(Strings.AppResources.GetString("SomethingWrong"),
                    Strings.AppResources.GetString("FolderNotExist"));
            }

            HideGlassLabel.Begin();
        }

        private void HideButton_Click(object sender, RoutedEventArgs e)
        {
            if (downloader != null) downloader.Dispose();
            MainPage.Current.DownloadBarCollection.Remove(this);
        }

        //为了避免在TopGlassLabel的Opacity为0时用户误触TopGlassLabel上面的按键
        //我们在Hide动画结束后把按键所在控件的Visibility改成不可见
        //在Show动画开始前把Visibility改为可见
        //这样会出现一个问题，如果Hide动画没结束而Show动画开始了，那么Show动画结束后，按钮将变为不可见
        //使用showing记录当前是否在执行Show动画，如果Hide动画结束时，发现Show动画在播放，那么就不作更改
        private bool showing = false;

        private void HideGlassLabel_Completed(object sender, object e)
        {
            if (showing) return;
            ButtonPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowGlassLabel_Begin()
        {
            showing = true;
            ButtonPanel.Visibility = Visibility.Visible;
            ShowGlassLabel.Begin();
        }

        private void ShowGlassLabel_Completed(object sender, object e)
        {
            showing = false;
        }

        private void TopGlassLabel_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            Debug.WriteLine("DoubleTapped");
            var state = downloader.State;
            if (state == Enums.DownloadState.Pause)
                downloader.Start();
            else if (state == Enums.DownloadState.Error)
                downloader.Refresh();
            else if (state == Enums.DownloadState.Downloading)
                downloader.Pause();
            else if (state == Enums.DownloadState.Done)
                FileButton_Click(null, null);
            else if (state == Enums.DownloadState.Prepared)
                downloader.Start();
        }

    }
}
