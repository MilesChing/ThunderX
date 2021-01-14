using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using TX.Controls;
using TX.Core;
using TX.Core.Downloaders;
using TX.Core.Interfaces;
using TX.Core.Models.Contexts;
using TX.Core.Models.Sources;
using TX.Core.Models.Targets;
using TX.Core.Providers;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
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
    public sealed partial class TaskList : Page
    {
        App CurrentApp => ((App)App.Current);
        TXCoreManager Core => CurrentApp.Core;

        public TaskList()
        {
            this.InitializeComponent();
            AllVMs.CollectionChanged += AllVMs_CollectionChanged;
            AllVMs_CollectionChanged(AllVMs, null);
        }

        private void AllVMs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (sender is ObservableCollection<DownloaderBar> oc)
            {
                if (oc.Count > 0)
                {
                    DownloaderViewList.Visibility = Visibility.Visible;
                    EmptyView.Visibility = Visibility.Collapsed;
                }
                else
                {
                    DownloaderViewList.Visibility = Visibility.Collapsed;
                    EmptyView.Visibility = Visibility.Visible;
                }
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Core.ObservableDownloaders.CollectionChanged += Downloaders_CollectionChanged;
            Downloaders_CollectionChanged(Core.Downloaders,
                new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Add,
                    Core.Downloaders.ToList())
                );
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Core.ObservableDownloaders.CollectionChanged -= Downloaders_CollectionChanged;
            foreach (IDownloaderViewable vm in AllVMs)
                vm.ClearDownloaderBinding();
            AllVMs.Clear();
            GC.Collect();
            base.OnNavigatedFrom(e);
        }

        private async void Downloaders_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () => {
                    if (e.OldItems != null)
                    {
                        var toBeRemoved = AllVMs.Where(
                            bar => e.OldItems.Contains(bar.Downloader)).ToList();
                        foreach (var bar in toBeRemoved)
                        {
                            AllVMs.Remove(bar);
                            bar.PointerPressed -= VM_Clicked;
                            bar.ClearDownloaderBinding();
                        }
                    }

                    if (e.NewItems != null)
                    {
                        foreach (AbstractDownloader down in e.NewItems)
                        {
                            var newBar = new DownloaderBar();
                            newBar.BindDownloader(down);
                            newBar.PointerPressed += VM_Clicked;
                            newBar.Height = 72;
                            AllVMs.Add(newBar);
                        }
                    }
                });
        }

        private void VM_Clicked(object sender, PointerRoutedEventArgs e)
        {
            if (sender is DownloaderBar db)
                MainPage.Current.NavigateDownloaderDetailPage(db.Downloader);
        }

        private readonly ObservableCollection<DownloaderBar> AllVMs =
            new ObservableCollection<DownloaderBar>();

        private void Sort<TKey>(Func<DownloaderBar, TKey> keySelector)
        {
            var arr = AllVMs.ToArray();
            AllVMs.Clear();
            foreach (var bar in arr.OrderBy(keySelector))
                AllVMs.Add(bar);
        }

        private void SortAlphabetically_Click(object sender, RoutedEventArgs e)
            => Sort(bar => bar.Downloader.DownloadTask.Target.SuggestedName);

        private void SortByCreationTime_Click(object sender, RoutedEventArgs e)
            => Sort(bar => bar.Downloader.DownloadTask.CreationTime);

        private void SortByStatus_Click(object sender, RoutedEventArgs e)
            => Sort(bar => bar.Downloader.Status);
    }
}
