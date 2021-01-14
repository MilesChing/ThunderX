using System;
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
            VMCollection.Clear();
            GC.Collect();
            base.OnNavigatedFrom(e);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Core.ObservableHistories.CollectionChanged += CollectionChanged;
            CollectionChanged(Core.Histories,
                new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Add,
                    Core.Histories.ToList()));
            base.OnNavigatedTo(e);
        }

        private async void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            => await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                async () =>
                {
                    if (e.OldItems != null)
                    {
                        var toBeRemoved = VMCollection.Where(
                            vm => e.OldItems.OfType<DownloadHistory>().Any(
                                hist => hist.TaskKey.Equals(vm.TaskKey))).ToList();
                        foreach (var vm in toBeRemoved)
                            VMCollection.Remove(vm);
                    }

                    if (e.NewItems != null)
                    {
                        foreach (DownloadHistory hist in e.NewItems)
                        {
                            var newVM = await GetNewDownloadHistoryViewModelAsync(hist);
                            
                            if (newVM != null)
                                VMCollection.Add(newVM);
                            else Core.RemoveHistory(hist);
                        }
                    }
                });

        private async Task<DownloadHistoryViewModel> GetNewDownloadHistoryViewModelAsync(DownloadHistory history)
        {
            try
            {
                if (!Core.Tasks.TryGetValue(history.TaskKey, out DownloadTask task))
                    return null;
                var file = await StorageFile.GetFileFromPathAsync(history.DestinationFilePath);
                var props = await file.GetBasicPropertiesAsync();
                var source = new BitmapImage();
                using (var thumbnail = await file.GetThumbnailAsync(
                    Windows.Storage.FileProperties.ThumbnailMode.SingleItem))
                    await source.SetSourceAsync(thumbnail);
                return new DownloadHistoryViewModel()
                {
                    TaskKey = task.Key,
                    HistoryFileName = file.Name,
                    HistoryFileSizeString = ((long)props.Size).SizedString(),
                    Source = source,
                    OriginalHistory = history,
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        private readonly ObservableCollection<DownloadHistoryViewModel> VMCollection =
            new ObservableCollection<DownloadHistoryViewModel>();

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
                    var file = await StorageFile.GetFileFromPathAsync(vm.OriginalHistory.DestinationFilePath);
                    await Launcher.LaunchFileAsync(file);
                }
                catch (Exception)
                {
                    return;
                }
            }
        }

        private async void HistoryFolderOpened_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem fitem &&
                fitem.DataContext is DownloadHistoryViewModel vm)
            {
                try
                {
                    var folder = await StorageFolder.GetFolderFromPathAsync(
                        Path.GetDirectoryName(vm.OriginalHistory.DestinationFilePath));
                    await Launcher.LaunchFolderAsync(folder);
                }
                catch (Exception)
                {
                    return;
                }
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

        public string HistoryFileName;

        public string HistoryFileSizeString;

        public ImageSource Source;

        public DownloadHistory OriginalHistory;
    }
}
