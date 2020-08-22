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
using Windows.Storage.AccessCache;
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
        public SetPage()
        {
            this.InitializeComponent();
            DarkModeSwitch.Toggled += (sender, e) =>
                ((App)App.Current).CallThemeUpdate(DarkModeSwitch.IsOn ? 
                ElementTheme.Dark : ElementTheme.Light);
            StartLoadDownloadFolderPath();
            LicenseChanged(((App)App.Current).AppLicense);
        }

        private Settings SettingsInstance => Settings.Instance;

        public async void StartLoadDownloadFolderPath()
        {
            NowFolderTextBlock.Text = StorageApplicationPermissions.MostRecentlyUsedList.ContainsItem(Settings.Instance.DownloadsFolderToken) ?
                (await StorageApplicationPermissions.MostRecentlyUsedList.GetFolderAsync(Settings.Instance.DownloadsFolderToken)).Path : 
                Strings.AppResources.GetString("FolderNotExist");
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
            Settings.Instance.DownloadsFolderToken = StorageApplicationPermissions.MostRecentlyUsedList.Add(folder);
            System.Diagnostics.Debug.WriteLine(nameof(Settings.Instance.DownloadsFolderToken) + ": " + Settings.Instance.DownloadsFolderToken);
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
    }
}
