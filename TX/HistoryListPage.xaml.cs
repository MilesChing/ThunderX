using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

        public HistoryListPage()
        {
            this.InitializeComponent();
            VMCollection.CollectionChanged += VMCollection_CollectionChanged;
            VMCollection_CollectionChanged(VMCollection, null);
        }

        private void VMCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (sender is ObservableCollection<DownloadHistoryViewModel> oc)
            {
                if (oc.Count > 0)
                {
                    HistoryViewList.Visibility = Visibility.Visible;
                    EmptyView.Visibility = Visibility.Collapsed;
                }
                else
                {
                    HistoryViewList.Visibility = Visibility.Collapsed;
                    EmptyView.Visibility = Visibility.Visible;
                }
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Core.ObservableHistories.CollectionChanged -= CollectionChanged;
            base.OnNavigatedFrom(e);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            Core.ObservableHistories.CollectionChanged += CollectionChanged;
            var nowHistories = VMCollection.Select(view => view.OriginalHistory);
            var newItems = Core.Histories.Except(nowHistories).ToList();
            var oldItems = nowHistories.Except(Core.Histories).ToList();
            await SolveCollectionChangingAsync(Core.Histories,
                new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Replace, newItems, oldItems));
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

        private async void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            => await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                async () => await SolveCollectionChangingAsync(sender, e));

        private async Task SolveCollectionChangingAsync(object sender, NotifyCollectionChangedEventArgs e)
        {
            var oldItems = e.OldItems?.Cast<DownloadHistory>() ?? new List<DownloadHistory>();
            var newItems = e.NewItems?.Cast<DownloadHistory>() ?? new List<DownloadHistory>();
            var intersect = oldItems.Intersect(newItems);
            oldItems = oldItems.Except(intersect);
            newItems = newItems.Except(intersect);

            foreach (DownloadHistory hist in oldItems)
            {
                var toBeDeleted = VMCollection.FirstOrDefault(
                    vm => vm.TaskKey.Equals(hist.TaskKey));
                if (toBeDeleted != null) VMCollection.Remove(toBeDeleted);
            }

            foreach (DownloadHistory hist in newItems)
            {
                var newVM = await NewDownloadHistoryViewModelAsync(hist);
                if (newVM != null) VMCollection.Add(newVM);
                else Core.RemoveHistory(hist);
            }
        }

        private async Task<DownloadHistoryViewModel> NewDownloadHistoryViewModelAsync(DownloadHistory history)
        {
            try
            {
                IStorageItem item = null;

                try
                {
                    item = await StorageUtils.GetStorageItemAsync(history.DestinationPath);
                }
                catch (Exception)
                {
                    item = null;
                }

                long? size = null;
                ImageSource iconSource = null;
                string fileName = Path.GetFileName(history.DestinationPath);
                if (string.IsNullOrEmpty(fileName)) fileName = Path.GetDirectoryName(history.DestinationPath);
                if (string.IsNullOrEmpty(fileName)) fileName = history.DestinationPath;

                if (item is StorageFile file)
                {
                    size = await file.GetSizeAsync();
                    var source = new BitmapImage();
                    using (var thumbnail = await file.GetThumbnailAsync(
                        Windows.Storage.FileProperties.ThumbnailMode.SingleItem))
                        await source.SetSourceAsync(thumbnail);
                    iconSource = source;
                } 
                else if (item is StorageFolder folder)
                {
                    size = await folder.GetSizeAsync();
                    var source = new BitmapImage();
                    using (var thumbnail = await folder.GetThumbnailAsync(
                        Windows.Storage.FileProperties.ThumbnailMode.SingleItem))
                        await source.SetSourceAsync(thumbnail);
                    iconSource = source;
                } 
                else
                {
                }

                return new DownloadHistoryViewModel()
                {
                    OriginalHistory = history,
                    TaskKey = history.TaskKey,
                    HistoryFileSize = size,
                    Source = iconSource,
                };
            }
            catch (Exception)
            {
                return null;
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

        private void SelectAllButton_Click(object sender, RoutedEventArgs e) =>
            HistoryViewList.SelectAll();

        private void HistoryViewList_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
            SelectionCountText.Text = HistoryViewList.SelectedItems.Count.ToString();
    }

    class DownloadHistoryViewModel
    {
        public string TaskKey;

        public long? HistoryFileSize;

        public ImageSource Source;

        public DownloadHistory OriginalHistory;
    }
}
