using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace TX
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class SetPage : Page
    {
        public SetPage()
        {
            //在InitializeComponent的时候会触发一次ValueChanged
            //所以需要用prev记录好内部存储的ThreadNumber
            //不然就在ValueChanged里面被更新了
            int prev = StorageTools.Settings.ThreadNumber;
            this.InitializeComponent();
            //赋初值
            ThreadNumSlider.Value = prev;
            NowFolderTextBlock.Text = StorageTools.Settings.DownloadFolderPath;
        }

        /// <summary>
        /// 选择文件夹按钮点击
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.FileTypeFilter.Add(".");
            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder == null) return;
            StorageTools.Settings.DownloadFolderPath = folder.Path;
            NowFolderTextBlock.Text = folder.Path;
            Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Clear();
            Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(folder);
        }

        /// <summary>
        /// 滑块值改变
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ThreadNumSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            StorageTools.Settings.ThreadNumber = (int)ThreadNumSlider.Value;
        }

        /// <summary>
        /// 投票控件点击的响应方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void RatingControl_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (((RatingControl)sender).Value < 4) return;
            var pfn = Windows.ApplicationModel.Package.Current.Id.FamilyName;
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-windows-store://review/?PFN=" + pfn));
        }

        /// <summary>
        /// 滚动栏
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void scrollViewer_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            int delta = e.GetCurrentPoint(this).Properties.MouseWheelDelta;
            scrollViewer.ChangeView(0, scrollViewer.VerticalOffset - delta/5.0, 1,false);
        }
    }
}
