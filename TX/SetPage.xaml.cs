using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using TX.StorageTools;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Services.Store;
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
    public sealed partial class SetPage : TXPage
    {
        private App CurrentApplication = ((App)Application.Current);

        private bool UserModify = false;

        public SetPage()
        {
            this.InitializeComponent();
            ThreadNumSlider.Value = Settings.ThreadNumber;
            MaximumRetriesSlider.Value = Settings.MaximumRetries;
            DarkModeSwitch.IsOn = Settings.DarkMode;
            NowFolderTextBlock.Text = StorageTools.Settings.DownloadFolderPath;
            LicenseChanged(((App)App.Current).AppLicense);
            UserModify = true;
        }

        protected override void LicenseChanged(StoreAppLicense license)
        {
            base.LicenseChanged(license);
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

        //ValueChanged调用开始先检查UserModify

        private void ThreadNumSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (!UserModify) return;
            StorageTools.Settings.ThreadNumber = (int)ThreadNumSlider.Value;
        }

        private void DarkModeSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (!UserModify) return;
            Settings.DarkMode = DarkModeSwitch.IsOn;
            ((App)App.Current).CallThemeUpdate(DarkModeSwitch.IsOn ? ElementTheme.Dark : ElementTheme.Light);
        }

        private void MaximumRetriesSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (!UserModify) return;
            Settings.MaximumRetries = (int)e.NewValue;
        }
    }
}
