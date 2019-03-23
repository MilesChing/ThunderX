using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using TX.Controls;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

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
        public MainPage()
        {
            Current = this;//设置Current指针（以便在全局访问）
            InitializeComponent();
            ResetTitleBar();//设置标题栏颜色
            Task.Run(async () =>
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                    async () =>
                    {
                        DownloadBarCollection = new ObservableCollection<Controls.DownloadBar>();//创建控件集合
                        DownloadBarCollection.CollectionChanged += DownloadBarCollection_CollectionChanged;//订阅内容变化事件
                        gv.DataContext = DownloadBarCollection;//设置绑定
                        newTaskPageControl = new Controls.ApplicationWindowControl(typeof(NewTaskPage), Strings.AppResources.GetString("NewTaskPageName"));
                        setPageControl = new Controls.ApplicationWindowControl(typeof(SetPage), Strings.AppResources.GetString("SetPageName"));
                        //恢复上次关闭时保存的控件
                        var list = await StorageTools.StorageManager.GetMessagesAsync();
                        if (list != null)
                            foreach (Models.DownloaderMessage ms in list)
                            {
                                IDownloader dw = NetWork.UrlConverter.GetDownloader(ms.URL);
                                Controls.DownloadBar db = new Controls.DownloadBar();
                                db.SetDownloader(dw);
                                dw.SetDownloader(ms);
                                MainPage.Current.DownloadBarCollection.Add(db);
                            }
                    });
            });
        }
        
        private void DownloadBarCollection_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (DownloadBarCollection.Count == 0 && viewbox.Opacity == 0) ShowLogo.Begin();
            if (e.NewItems == null) return;
            foreach (Controls.DownloadBar db in e.NewItems)
            {
                SetWidthBind(db);//为新加入的控件设置绑定
                if (viewbox.Opacity != 0)
                    HideLogo.Begin();
            }
        }

        /// <summary>
        /// 用于显示在界面中的下载器控件集合
        /// </summary>
        public ObservableCollection<Controls.DownloadBar> DownloadBarCollection;

        /// <summary>
        /// 将新加入GridView的控件与WidthBindTool的宽度做绑定
        /// WidthBindTool的作用是根据GridView宽度调整自身宽度，使控件总能填充GridView
        /// </summary>
        /// <param name="db">加入的对象，只能是DownlaodBar</param>
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
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
            TB.ButtonBackgroundColor = Color.FromArgb(0xcc, 0xee, 0xee, 0xee);
            TB.BackgroundColor = Color.FromArgb(0xcc, 0xee, 0xee, 0xee);
            //var t = Windows.ApplicationModel.Core.CoreApplication.GetCurrentView().TitleBar;
            //t.ExtendViewIntoTitleBar = true;
        }

        /// <summary>
        /// 用于打开窗口的控制器，这两个控制器包含设置窗口和新建项目窗口
        /// 用于处理新窗口的各种问题，包括窗口未关闭等等
        /// </summary>
        private Controls.ApplicationWindowControl newTaskPageControl;
        private Controls.ApplicationWindowControl setPageControl;

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            await newTaskPageControl.OpenNewWindowAsync();
        }

        private async void SetButton_Click(object sender, RoutedEventArgs e)
        {
            await setPageControl.OpenNewWindowAsync();
        }

        /// <summary>
        /// 添加一个DownloadBar到主界面，为了避免线程问题写在这里
        /// 这段代码包含建立下载器的细节
        /// </summary>
        public async void AddDownloadBar(Models.InitializeMessage message)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
             {
                 DownloadBar db = new DownloadBar();
                 IDownloader dw = NetWork.UrlConverter.GetDownloader(message.Url);
                 db.SetDownloader(dw);
                 await dw.SetDownloaderAsync(message);
                 DownloadBarCollection.Add(db);
             });
        }
    }
}

