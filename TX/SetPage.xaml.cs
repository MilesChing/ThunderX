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
        private App CurrentApplication = ((App)Application.Current);

        public SetPage()
        {
            this.RequestedTheme = Settings.DarkMode ? ElementTheme.Dark : ElementTheme.Light;
            ResetTitleBar();

            //在InitializeComponent的时候会触发一次ValueChanged
            //所以需要用prev记录好内部存储的ThreadNumber
            //不然就在ValueChanged里面被更新了
            int prev_th = Settings.ThreadNumber;
            int prev_re = Settings.MaximumRetries;
            bool dark = Settings.DarkMode;
            this.InitializeComponent();
            SetThemeChangedListener();
            //赋初值
            ThreadNumSlider.Value = prev_th;
            MaximumRetriesSlider.Value = prev_re;
            DarkModeSwitch.IsOn = dark;
            NowFolderTextBlock.Text = StorageTools.Settings.DownloadFolderPath;

            CurrentApplication.LicenseChanged += CurrentApplication_LicenseChanged;
            CurrentApplication_LicenseChanged(CurrentApplication.AppLicense);
        }

        private void CurrentApplication_LicenseChanged(Windows.Services.Store.StoreAppLicense license)
        {
            if (license == null) return;
            if (license.IsActive)
            {
                if (license.IsTrial)
                {
                    ThreadLayout_TrialMessage.Visibility = Visibility.Visible;
                    ThreadNumSlider.IsEnabled = false;
                }
                else
                {
                    ThreadLayout_TrialMessage.Visibility = Visibility.Collapsed;
                    ThreadNumSlider.IsEnabled = true;
                }
            }
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

        private void ThreadNumSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            StorageTools.Settings.ThreadNumber = (int)ThreadNumSlider.Value;
        }

        private void DarkModeSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.DarkMode = DarkModeSwitch.IsOn;
            ((App)App.Current).CallThemeUpdate(DarkModeSwitch.IsOn ? ElementTheme.Dark : ElementTheme.Light);
        }

        private void SetThemeChangedListener()
        {
            ((App)App.Current).ThemeChanged += async (theme) =>
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    this.RequestedTheme = theme;
                    ResetTitleBar();
                });
            };
        }

        private void MaximumRetriesSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            Settings.MaximumRetries = (int)e.NewValue;
        }
    }
}
