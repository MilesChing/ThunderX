using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using TX.Core;
using TX.Core.Providers;
using TX.Core.Utils;
using TX.Utils;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.System;
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
        App CurrentApp => ((App)App.Current);
        private readonly Settings SettingEntries = new Settings();
        private readonly long[] MemoryLimits = new long[]
        {
            1024 * 1024 * 32,
            1024 * 1024 * 64,
            1024 * 1024 * 128,
            1024 * 1024 * 256,
            1024 * 1024 * 512,
        };

        private readonly TXCoreManager Core = ((App)App.Current).Core;

        private readonly ObservableCollection<string> AnnounceUrlsCollection = new ObservableCollection<string>();

        public SetPage()
        {
            this.InitializeComponent();
        }

        private void ReloadAnnounceUrls()
        {
            AnnounceUrlsCollection.Clear();
            foreach (var url in Core.CustomAnnounceURLs)
                AnnounceUrlsCollection.Add(url);
        }

        private async void RefreshStorageSize()
        {
            CacheFileSizeTextBlock.Opacity = 0.4;
            try
            {
                var cacheSize = await Core.CacheFolder.GetSizeAsync();
                CacheFileSizeTextBlock.Text = cacheSize.SizedString();
            }
            catch (Exception)
            {
                CacheFileSizeTextBlock.Text = Windows.ApplicationModel.Resources
                    .ResourceLoader.GetForCurrentView().GetString("Unknown");
            }
            finally
            {
                CacheFileSizeTextBlock.Opacity = 1.0;
            }
        }

        private async void InitializeDownloadFolder()
        {
            DownloadFolderPathTextBlock.Text = (await LocalFolderManager
                .GetOrCreateDownloadFolderAsync()).Path;
        }

        private void InitializeMemoryLimits()
        {
            MemoryUpperboundComboBox.ItemsSource = MemoryLimits.Select(m => m.SizedString()).ToArray();
            MemoryUpperboundComboBox.SelectedIndex = Math.Max(Array.IndexOf(
                MemoryLimits, SettingEntries.MemoryLimit), 0);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            RefreshStorageSize();
            InitializeDownloadFolder();
            InitializeMemoryLimits();
            ReloadAnnounceUrls();

            base.OnNavigatedTo(e);
        }

        private void DarkModeToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch tw)
            {
                MainPage.Current.RequestedTheme = tw.IsOn ?
                    ElementTheme.Dark : ElementTheme.Default;
            }
        }

        private void MemoryUpperboundComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb && 
                cb.SelectedIndex >= 0 && cb.SelectedIndex < MemoryLimits.Length)
            {
                SettingEntries.MemoryLimit = MemoryLimits[cb.SelectedIndex];
            }
        }

        private async void CleanUpButton_Click(object sender, RoutedEventArgs e)
        {
            await ((App)App.Current).Core.CleanCacheFolderAsync();
            RefreshStorageSize();
        }

        private async void EditFileItem_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchFileAsync(
                await StorageUtils.GetOrCreateAnnounceUrlsFileAsync());
        }

        private async void ReloadItem_Click(object sender, RoutedEventArgs e)
        {
            await Core.LoadAnnounceUrlsAsync();
            ReloadAnnounceUrls();
        }

        private async void DownloadFolderEditButton_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.FileTypeFilter.Add(".");
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads;
            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder == null) return;
            SettingEntries.DownloadsFolderToken = StorageApplicationPermissions.MostRecentlyUsedList.Add(folder);
            InitializeDownloadFolder();
        }

        private async void DownloadFolderOpenButton_Click(object sender, RoutedEventArgs e) =>
            await Launcher.LaunchFolderAsync(await LocalFolderManager.GetOrCreateDownloadFolderAsync());
    }
}
