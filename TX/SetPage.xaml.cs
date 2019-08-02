using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using TX.StorageTools;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Services.Store;
using Windows.Storage;
using Windows.System;
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
            StartLoadDownloadFolderPath();
            ThreadNumSlider.Value = Settings.ThreadNumber;
            MaximumRetriesSlider.Value = Settings.MaximumRetries;
            DarkModeSwitch.IsOn = Settings.DarkMode;
            for (int i = 0; i < Settings.NormalRecordNumberParser.Length; ++i)
                ((TextBlock)MaximumRecordsComboBox.Items[i]).Text = Settings.NormalRecordNumberParser[i].ToString();
            MaximumRecordsComboBox.SelectedIndex = Settings.MaximumRecordsIndex;
            LicenseChanged(((App)App.Current).AppLicense);
            UserModify = true;
        }

        public async void StartLoadDownloadFolderPath()
        {
            string path = (await StorageManager.TryGetFolderAsync(Settings.DownloadsFolderToken))?.Path;
            if (path == null) path = Strings.AppResources.GetString("FolderNotExist");
            NowFolderTextBlock.Text = path;
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

        private async void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.FileTypeFilter.Add(".");
            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder == null) return;
            Settings.DownloadsFolderToken = StorageManager.AddToFutureAccessList(folder);
            NowFolderTextBlock.Text = folder.Path;
        }

        private async void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await Launcher.LaunchFolderAsync(await StorageFolder.GetFolderFromPathAsync(NowFolderTextBlock.Text));
            }
            catch (Exception)
            {
                Toasts.ToastManager.ShowSimpleToast(Strings.AppResources.GetString("SomethingWrong"),
                    Strings.AppResources.GetString("CheckDownloadFolder"));
            }
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

        private void MaximumRecordsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!UserModify) return;
            Settings.MaximumRecordsIndex = MaximumRecordsComboBox.SelectedIndex;
        }
    }
}
