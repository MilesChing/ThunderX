using System;
using System.Collections.ObjectModel;
using TX.Controls;
using TX.VisualManager;
using TX.Downloaders;
using TX.StorageTools;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Core;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using System.Collections.Generic;
using Windows.UI.Popups;
using Windows.Storage.Pickers;
using Windows.Storage.AccessCache;

namespace TX
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        /// <summary>
        /// 指向当前MainPage的引用
        /// </summary>
        public static MainPage Current;

        /// <summary>
        /// 在程序运行的过程中，MainPage始终只有一个View，因此有固定的引用和ViewID
        /// </summary>
        public int ViewID = ApplicationView.GetApplicationViewIdForWindow(Window.Current.CoreWindow);


        public MainPage()
        {
            Current = this;
            this.RequestedTheme = Settings.DarkMode ? ElementTheme.Dark : ElementTheme.Light;
            ResetTitleBar();//设置标题栏颜色
            SetThemeChangedListener();
            ApplicationView.GetForCurrentView().Consolidated += MainPage_Consolidated;
            DownloadBarCollection.CollectionChanged += DownloadBarCollection_CollectionChanged;
            DownloadBarManager = new DownloadBarManager(DownloadBarCollection, downloadBarCollectionLock);
            InitializeComponent();
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            //恢复上次关闭时保存的控件
            var list = await TXDataFileIO.GetMessagesAsync();
            if (list != null)
            {
                foreach (Models.DownloaderMessage ms in list)
                {
                    DownloadBar db = new DownloadBar();
                    DownloadBarCollection.Add(db);
                    AbstractDownloader dw = AbstractDownloader.GetDownloaderFromType(ms.DownloaderType);
                    db.SetDownloader(dw);
                    dw.SetDownloaderFromBreakpoint(ms);
                }
            }

            if(Settings.DownloadsFolderToken == null)
            {
                var contentDialog = new ContentDialog()
                {
                    Title = Strings.AppResources.GetString("DownloadFolderPathIllegal"),
                    Content = Strings.AppResources.GetString("SetDownloadFolder"),
                    PrimaryButtonText = Strings.AppResources.GetString("Select"),
                    SecondaryButtonText = Strings.AppResources.GetString("Cancel"),
                    FullSizeDesired = false,
                };

                contentDialog.PrimaryButtonClick += async (sender, e) =>
                {
                    var folderPicker = new FolderPicker();
                    folderPicker.FileTypeFilter.Add(".");
                    StorageFolder folder = null;
                    folder = await folderPicker.PickSingleFolderAsync();
                    if (folder == null) App.Current.Exit();
                    else Settings.DownloadsFolderToken = StorageApplicationPermissions
                        .MostRecentlyUsedList.Add(folder);
                };

                contentDialog.SecondaryButtonClick += (sender, e) =>
                    App.Current.Exit();

                await contentDialog.ShowAsync();
            }
        }

        /// <summary>
        /// 主窗口退出时，其他窗口跟着退出
        /// </summary>
        private void MainPage_Consolidated(ApplicationView sender, ApplicationViewConsolidatedEventArgs args)
        {
            Application.Current.Exit();
        }

        /// <summary>
        /// 下载器控件集合元素变化，用于给新加入的控件设置绑定
        /// </summary>
        private void DownloadBarCollection_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            GC.Collect();
            if (DownloadBarCollection.Count == 0 && viewbox.Opacity == 0) ShowLogo.Begin();
            if(e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                foreach (Controls.DownloadBar db in e.NewItems)
                {
                    SetWidthBind(db);//为新加入的控件设置绑定
                    if (viewbox.Opacity != 0)
                        HideLogo.Begin();
                }
            }
        }

        /// <summary>
        /// 用于显示在界面中的下载器控件集合
        /// </summary>
        private ObservableCollection<Controls.DownloadBar> DownloadBarCollection = new ObservableCollection<Controls.DownloadBar>();
        private object downloadBarCollectionLock = new object();

        /// <summary>
        /// For other modules and pages to observe and modify download bars
        /// </summary>
        public DownloadBarManager DownloadBarManager;

        /// <summary>
        /// 将新加入GridView的控件与WidthBindTool的宽度做绑定
        /// WidthBindTool的作用是根据GridView宽度调整自身宽度，使控件总能填充GridView
        /// </summary>
        private void SetWidthBind(Controls.DownloadBar db)
        {
            Binding binding = new Binding();
            binding.Source = WidthBindTool;
            binding.Path = new PropertyPath("Width");
            db.SetBinding(WidthProperty, binding);
        }

        /// <summary>
        /// 计算GridView的控件宽度，用于设置控件宽度使得无论何时控件都能填充GridView
        /// </summary>
        /// <param name="width">实际空间大小</param>
        /// <param name="max">宽度的最大可能值</param>
        /// <param name="min">宽度的最小可能值</param>
        /// <param name="offset">偏移量（间距）</param>
        /// <returns>控件的计算宽度</returns>
        private static double GetWidth(double width, int max, int min, int offset = 8)
        {
            if (offset < 0 || offset > 12)
                offset = 8;
            double w = 1;
            int column = 1;
            int maxcolumn = (int)width / min;
            double i2 = width / min;
            for (int i = 1; i <= maxcolumn; i++)
                if (Math.Abs(i - i2) < 1)
                    column = (int)Math.Truncate(i2) == 0 ? 1 : (int)Math.Truncate(i2);
            w = width / column;
            w -= offset * column;
            return w;
        }

        /// <summary>
        /// 窗口尺寸变化处理程序
        /// </summary>
        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            WidthBindTool.Width = GetWidth(gv.ActualWidth, 800, 500);
        }

        /// <summary>
        /// 设置状态栏透明、扩展内容到状态栏
        /// </summary>
        private void ResetTitleBar()
        {
            var TB = ApplicationView.GetForCurrentView().TitleBar;
            byte co = (byte)(Settings.DarkMode ? 0x11 : 0xee);
            byte fr = (byte)(0xff - co);
            TB.BackgroundColor = Color.FromArgb(0xcc,co,co,co);
            TB.ButtonBackgroundColor = Color.FromArgb(0xcc, co, co, co);
            TB.ButtonForegroundColor = Color.FromArgb(0xcc, fr, fr, fr);
            //var t = Windows.ApplicationModel.Core.CoreApplication.GetCurrentView().TitleBar;
            //t.ExtendViewIntoTitleBar = true;
        }

        /// <summary>
        /// 添加一个DownloadBar到主界面，为了避免线程问题写在这里
        /// 这段代码包含建立下载器的细节
        /// </summary>
        public async void AddDownloadBar(AbstractDownloader downloader)
        {
            GC.Collect();
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
             {
                 DownloadBar db = new DownloadBar();
                 DownloadBarCollection.Insert(0, db);
                 db.SetDownloader(downloader);
             });
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

        /// <summary>
        /// 用于打开窗口的控制器，这两个控制器包含设置窗口和新建项目窗口
        /// 用于处理新窗口的各种问题，包括窗口未关闭等等
        /// </summary>
        private ApplicationWindowControl newTaskPageControl = new ApplicationWindowControl(typeof(NewTaskPage), Strings.AppResources.GetString("NewTaskPageName")) { CacheView = true };
        private ApplicationWindowControl setPageControl = new ApplicationWindowControl(typeof(SetPage), Strings.AppResources.GetString("SetPageName")) { CacheView = true };
        private ApplicationWindowControl aboutPageControl = new ApplicationWindowControl(typeof(AboutPage), Strings.AppResources.GetString("AboutPageName")) { CacheView = true };
        private ApplicationWindowControl browserPageControl = new ApplicationWindowControl(typeof(WebBrowserPage), Strings.AppResources.GetString("WebBrowserPageName"));

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            await newTaskPageControl.OpenNewWindowAsync();
            await NewTaskPage.Current.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                ()=>NewTaskPage.Current.RefreshUI());
        }

        private async void SetButton_Click(object sender, RoutedEventArgs e)
        {
            await setPageControl.OpenNewWindowAsync();
        }

        private async void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            await aboutPageControl.OpenNewWindowAsync();
        }

        private async void BrowserButton_Click(object sender, RoutedEventArgs e)
        {
            await browserPageControl.OpenNewWindowAsync();
        }

        public async void OpenNewTask(string URL)
        {
            await newTaskPageControl.OpenNewWindowAsync();
            NewTaskPage.Current.ForciblySetURL(URL);
        }

        private ApplicationViewMode _viewMode = ApplicationViewMode.Default;

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            ApplicationViewMode target = _viewMode == ApplicationViewMode.CompactOverlay ?
                ApplicationViewMode.Default : ApplicationViewMode.CompactOverlay;
            _viewMode = target;
            await ApplicationViewSwitcher.TryShowAsViewModeAsync(ViewID, target);
            VisualStateManager.GoToState(this, target.ToString(), false);
        }
    }
}

