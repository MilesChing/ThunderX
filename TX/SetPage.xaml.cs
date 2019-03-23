using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using TX.StorageTools;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.ViewManagement;
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
            this.RequestedTheme = Settings.DarkMode ? ElementTheme.Dark : ElementTheme.Light;
            ResetTitleBar();

            //在InitializeComponent的时候会触发一次ValueChanged
            //所以需要用prev记录好内部存储的ThreadNumber
            //不然就在ValueChanged里面被更新了
            int prev = StorageTools.Settings.ThreadNumber;
            bool dark = Settings.DarkMode;
            this.InitializeComponent();
            //赋初值
            ThreadNumSlider.Value = prev;
            DarkModeSwitch.IsOn = dark;
            NowFolderTextBlock.Text = StorageTools.Settings.DownloadFolderPath;
        }

        /// <summary>
        /// 设置状态栏透明、扩展内容到状态栏
        /// </summary>
        private void ResetTitleBar()
        {
            var TB = ApplicationView.GetForCurrentView().TitleBar;
            byte co = (byte)(Settings.DarkMode ? 0x11 : 0xee);
            byte fr = (byte)(0xff - co);
            TB.BackgroundColor = Color.FromArgb(0xcc, co, co, co);
            TB.ButtonBackgroundColor = Color.FromArgb(0xcc, co, co, co);
            TB.ButtonForegroundColor = Color.FromArgb(0xcc, fr, fr, fr);
        }

        /// <summary>
        /// 选择文件夹按钮点击
        /// </summary>
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
        private void ThreadNumSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            StorageTools.Settings.ThreadNumber = (int)ThreadNumSlider.Value;
        }

        /// <summary>
        /// 投票控件点击的响应方法
        /// </summary>
        private async void RatingControl_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (((RatingControl)sender).Value < 4) return;
            var pfn = Windows.ApplicationModel.Package.Current.Id.FamilyName;
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-windows-store://review/?PFN=" + pfn));
        }
        
        /// <summary>
        /// 黑暗模式开关切换
        /// </summary>
        private void DarkModeSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.DarkMode = DarkModeSwitch.IsOn;
        }
    }
}
