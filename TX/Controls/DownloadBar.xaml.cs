using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

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
            dw.MessageComplete += MessageCompleted;
            dw.OnDownloadProgressChange += OnDownloadProgressChanged;
            dw.DownloadComplete += DownloadCompleted;
            dw.DownloadError += DownloadError;
            dw.Log += UpdateLog;
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
        /// 当Downloader完成初始化时填充显示信息
        /// </summary>
        /// <param name="message">加载完成事件的参数，DownloaderMessage</param>
        private void MessageCompleted(Models.DownloaderMessage message)
        {
            Task.Run(async () =>
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    NameBlock.Text = message.FileName + message.TypeName;
                    SizeBlock.Text = Converters.StringConverters.GetPrintSize(message.DownloadSize) + "/" + Converters.StringConverters.GetPrintSize(message.FileSize);
                    ProgressBlock.Text = ((int)((1.0 * message.DownloadSize) / message.FileSize * 100)).ToString() + "%";
                    Bar.Value = ((int)((1.0 * message.DownloadSize) / message.FileSize * 100));
                    PlayButton.IsEnabled = true;
                });
            });
        }

        /// <summary>
        /// 下载进度变化事件处理函数
        /// </summary>
        /// <param name="size">已下载进度</param>
        /// <param name="all">文件总大小</param>
        private void OnDownloadProgressChanged(long size, long all)
        {
            //在多线程内激发函数
            //防止跨线程修改UI
            Task.Run(async () =>
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    Bar.Value = (int)(1.0 * size / all * 100);
                    ProgressBlock.Text = ((int)(1.0 * size / all * 100)).ToString() + "%";
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
                && downloader.GetDownloadState() != Enums.DownloadState.Pending)
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
        }
    }
}
