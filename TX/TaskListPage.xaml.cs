using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using TX.Collections;
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

        private readonly ObservableCollection<DownloaderBar> DownloaderBars =
            new ObservableCollection<DownloaderBar>();
        private readonly CollectionBind<AbstractDownloader, DownloaderBar> DownloaderCollectionBind;

        public TaskList()
        {
            InitializeComponent();
            DownloaderCollectionBind = 
                new CollectionBind<AbstractDownloader, DownloaderBar>(
                Core.Downloaders, DownloaderBars, 
                (downloader) => {
                    var newBar = new DownloaderBar();
                    newBar.BindDownloader(downloader);
                    newBar.PointerPressed += VM_Clicked;
                    newBar.Height = 72;
                    return newBar;
                }, 
                (downloader, downloaderBar) => downloaderBar.Downloader == downloader);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            DownloaderCollectionBind.IsEnabled = true;
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            DownloaderCollectionBind.IsEnabled = false;
            base.OnNavigatedFrom(e);
        }

        private void VM_Clicked(object sender, PointerRoutedEventArgs e)
        {
            if (sender is DownloaderBar db)
                MainPage.Current.NavigateDownloaderDetailPage(db.Downloader);
        }
    }
}
