using System;
using System.Threading.Tasks;
using TX.Downloaders;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

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
            MessageBlock.Text = "Pending";
            SizeBlock.Text = "";
            ProgressBlock.Text = "0%";
            downloader = null;
        }

        /// <summary>
        /// 提供下载器的引用，以显示其信息。
        /// </summary>
        public void SetDownloader(IDownloader dw)
        {
            downloader = dw;
            //注册事件
            dw.DownloadProgressChanged += DownloadProgressChanged;
            dw.DownloadComplete += DownloadCompleted;
            dw.DownloadError += DownloadError;
            dw.Log += UpdateLog;
            dw.StateChanged += Dw_StateChanged;

            var message = dw.GetDownloaderMessage();
            if (message == null) return;
            NameBlock.Text = message.FileName + message.TypeName;
            SizeBlock.Text = Converters.StringConverters.GetPrintSize(message.DownloadSize) + "/" + Converters.StringConverters.GetPrintSize(message.FileSize);
            ProgressBlock.Text = ((int)((1.0 * message.DownloadSize) / message.FileSize * 100)).ToString() + "%";
            Bar.Value = ((int)((1.0 * message.DownloadSize) / message.FileSize * 100));
            PlayButton.IsEnabled = true;
        }

        /// <summary>
        /// 更新界面信息以适应状态变化
        /// </summary>
        private void Dw_StateChanged(Enums.DownloadState state)
        {
            if(state == Enums.DownloadState.Pause)
            {
                Task.Run(async () =>
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                     {
                         PauseButton.IsEnabled = false;
                         PlayButton.IsEnabled = true;
                     });
                });
            }
            else if(state == Enums.DownloadState.Error)
            {
                Task.Run(async () =>
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        PauseButton.IsEnabled = false;
                        PlayButton.IsEnabled = false;
                    });
                });
            }
        }

        /// <summary>
        /// 更新消息框（速度、状态等等）
        /// </summary>
        private void UpdateLog(string log)
        {
            Task.Run(async () =>
            {
                await MessageBlock.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                    () =>
                    {
                        MessageBlock.Text = log;
                    });
            });
        }

        /// <summary>
        /// 下载器的引用
        /// </summary>
        public IDownloader downloader;

        /// <summary>
        /// 下载进度变化事件处理函数
        /// </summary>
        /// <param name="size">已下载进度</param>
        /// <param name="all">文件总大小</param>
        private void DownloadProgressChanged(long size, long all)
        {
            //在多线程内激发函数
            //防止跨线程修改UI
            Task.Run(async () =>
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    Bar.Value = (int)(1.0 * size / all * 100);
                    if (all != 0)
                    {
                        int progress = (int)(1.0 * size / all * 100);
                        if (progress <= 100 && progress >= 0)
                            ProgressBlock.Text = progress.ToString() + "%";
                        else ProgressBlock.Text = "?%";
                    }
                    SizeBlock.Text = Converters.StringConverters.GetPrintSize(size) + "/" + Converters.StringConverters.GetPrintSize(all);
                });
            });
        }

        /// <summary>
        /// 下载完成事件
        /// </summary>
        private async void DownloadCompleted(Models.DownloaderMessage message)
        {
            await MainPage.Current.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    MainPage.Current.DownloadBarCollection.Remove(this);
                });
        }

        /// <summary>
        /// 处理下载异常事件
        /// </summary>
        private void DownloadError(Exception e)
        {
            Task.Run(async () =>
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                    () => {
                        PauseButton.IsEnabled = false;
                        PlayButton.IsEnabled = false;
                    });
            });
            
            Toasts.ToastManager.ShowSimpleToast(Strings.AppResources.GetString("SomethingWrong"), e.Message);
        }

        /// <summary>
        /// 鼠标进入透明遮盖 显示操作项
        /// </summary>
        private void TopGlassLabel_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            ShowGlassLabel.Begin();
        }

        /// <summary>
        /// 鼠标离开 遮盖变为透明
        /// </summary>
        private void TopGlassLabel_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            HideGlassLabel.Begin();
        }

        /// <summary>
        /// 结束任务并删除控件
        /// </summary>
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (downloader != null) downloader.Dispose();
            MainPage.Current.DownloadBarCollection.Remove(this);
        }

        /// <summary>
        /// 开始
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (downloader.GetDownloadState() != Enums.DownloadState.Prepared
                && downloader.GetDownloadState() != Enums.DownloadState.Pause)
                return;
            PlayButton.IsEnabled = false;
            PauseButton.IsEnabled = true;
            RefreshButton.IsEnabled = true;
            downloader.Start();
        }

        /// <summary>
        /// 暂停
        /// </summary>
        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (downloader.GetDownloadState() != Enums.DownloadState.Downloading)
                return;
            PauseButton.IsEnabled = false;
            PlayButton.IsEnabled = true;
            downloader.Pause();
        }

        /// <summary>
        /// 完成动画结束，可以删除控件
        /// </summary>
        private void DoneAnimation_Completed(object sender, object e)
        {
            MainPage.Current.DownloadBarCollection.Remove(this);
        }

        /// <summary>
        /// 刷新
        /// </summary>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            downloader.Refresh();
            PauseButton.IsEnabled = true;
            PlayButton.IsEnabled = false;
        }
    }
}
