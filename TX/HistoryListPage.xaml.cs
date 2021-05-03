using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TX.Collections;
using TX.Core;
using TX.Core.Models.Contexts;
using TX.Core.Utils;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace TX
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class HistoryListPage : Page
    {
        App CurrentApp => ((App)App.Current);
        TXCoreManager Core => CurrentApp.Core;

        private readonly ObservableCollection<DownloadHistoryViewModel> VMCollection =
            new ObservableCollection<DownloadHistoryViewModel>();
        private readonly CollectionBind<DownloadHistory, DownloadHistoryViewModel> HistoryCollectionBind;

        public HistoryListPage()
        {
            this.InitializeComponent();
            HistoryCollectionBind = new CollectionBind<DownloadHistory, DownloadHistoryViewModel>(
                Core.Histories, VMCollection,
                (history) => new DownloadHistoryViewModel(history), 
                (history, historyVm) => history == historyVm.OriginalHistory);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            HistoryCollectionBind.IsEnabled = false;
            base.OnNavigatedFrom(e);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            HistoryCollectionBind.IsEnabled = true;
            HandleNavigationParameter(e.Parameter);
            base.OnNavigatedTo(e);
        }

        private void HandleNavigationParameter(object parameter)
        {
            if (parameter is string paramString)
            {
                var item = VMCollection.FirstOrDefault(vm => 
                    vm.TaskKey.Equals(paramString));
                if (item != null)
                {
                    HistoryViewList.ScrollIntoView(item);
                    HistoryViewList.SelectionMode = ListViewSelectionMode.Single;
                    HistoryViewList.SelectedItem = item;
                }
            }
        }

        private void Item_Holding(object sender, HoldingRoutedEventArgs e) =>
            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);

        private void Item_RightTapped(object sender, RightTappedRoutedEventArgs e) =>
            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);

        private void HistoryDeleted_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem fitem &&
                fitem.DataContext is DownloadHistoryViewModel vm)
                Core.RemoveHistory(vm.OriginalHistory);
        }

        private async void HistoryFileOpened_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem fitem &&
                fitem.DataContext is DownloadHistoryViewModel vm)
            {
                try
                {
                    var item = await StorageUtils.GetStorageItemAsync(
                        vm.OriginalHistory.DestinationPath);
                    if (item is StorageFolder folder)
                        await Launcher.LaunchFolderAsync(folder);
                    if (item is StorageFile file)
                        await Launcher.LaunchFileAsync(file);
                }
                catch (Exception) { }
            }
        }

        private async void HistoryFolderOpened_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem fitem &&
                fitem.DataContext is DownloadHistoryViewModel vm)
            {
                try
                {
                    await Launcher.LaunchFolderPathAsync(
                        Path.GetDirectoryName(vm.OriginalHistory.DestinationPath));
                }
                catch (Exception) { }
            }
        }

        private void ClearSelectionButton_Click(object sender, RoutedEventArgs e) =>
            HistoryViewList.SelectedItems.Clear();

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (DownloadHistoryViewModel item in HistoryViewList.SelectedItems)
                Core.RemoveHistory(item.OriginalHistory);
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectButton.IsChecked == true)
                VisualStateManager.GoToState(this, "MultipleSelection", false);
            else
                VisualStateManager.GoToState(this, "NoSelection", false);
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e) => HistoryViewList.SelectAll();

        private void HistoryViewList_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
            SelectionCountText.Text = HistoryViewList.SelectedItems.Count.ToString();
    }

    class DownloadHistoryViewModel : INotifyPropertyChanged
    {
        public DownloadHistoryViewModel(DownloadHistory originalHistory)
        {
            this.OriginalHistory = originalHistory;
            _ = PrepareAsync();
        }

        public string TaskKey { get; private set; }

        public long? HistoryFileSize { get; private set; }

        public ImageSource Source { get; private set; }

        public DownloadHistory OriginalHistory { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private async Task PrepareAsync()
        {
            try
            {
                TaskKey = OriginalHistory.TaskKey;

                IStorageItem item = null;

                try
                {
                    item = await StorageUtils.GetStorageItemAsync(
                        OriginalHistory.DestinationPath);
                }
                catch (Exception) { }

                if (item is StorageFile file)
                {
                    HistoryFileSize = await file.GetSizeAsync();
                    var source = new BitmapImage();
                    using (var thumbnail = await file.GetThumbnailAsync(
                        Windows.Storage.FileProperties.ThumbnailMode.SingleItem))
                        await source.SetSourceAsync(thumbnail);
                    Source = source;
                }
                else if (item is StorageFolder folder)
                {
                    HistoryFileSize = await folder.GetSizeAsync();
                    var source = new BitmapImage();
                    using (var thumbnail = await folder.GetThumbnailAsync(
                        Windows.Storage.FileProperties.ThumbnailMode.SingleItem))
                        await source.SetSourceAsync(thumbnail);
                    Source = source;
                }
                else { }
            }
            catch (Exception) { }
            finally
            {
                var propertyChanged = PropertyChanged;
                if (propertyChanged != null)
                {
                    var handlers = propertyChanged.GetInvocationList();
                    foreach (PropertyChangedEventHandler handler in handlers)
                    {
                        handler(this, new PropertyChangedEventArgs(nameof(TaskKey)));
                        handler(this, new PropertyChangedEventArgs(nameof(HistoryFileSize)));
                        handler(this, new PropertyChangedEventArgs(nameof(Source)));
                    }
                }
            }
        }
    }
}
