using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace TX.VisualManager
{
    /// <summary>
    /// 用于管理一个新建窗口，该窗口有固定的Page和Title
    /// 该类的每个对象对应一个窗口Page
    /// https://www.cnblogs.com/ms-uap/p/5535681.html
    /// </summary>
    class ApplicationWindowControl
    {
        private Type pageType;
        private string title;
        private bool viewShown = false;
        private bool viewClosed = false;
        private int newViewId;
        //private int currentViewId;
        private Frame frame;

        //CacheView指示了当窗口关闭后再次打开时是否显示之前的页面
        public bool CacheView = false;

        public ApplicationWindowControl(Type page,string targetTitle)
        {
            pageType = page;
            title = targetTitle;
        }

        /// <summary>
        /// 打开窗口，如果窗口已存在则显示窗口
        /// </summary>
        /// <returns></returns>
        public async Task OpenNewWindowAsync()
        {
            //https://yq.aliyun.com/articles/676624
            //https://www.cnblogs.com/ms-uap/p/5535681.html
            GC.Collect();
            CoreApplicationView newView = CoreApplication.CreateNewView();
            if (viewShown)
            {
                if (viewClosed)
                {
                    if (!CacheView) await frame.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { frame.Navigate(pageType); });
                    await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId);
                    viewClosed = false;
                }
                else await ApplicationViewSwitcher.SwitchAsync(newViewId);
            }
            else
            {
                await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    var newWindow = Window.Current;
                    var newAppView = ApplicationView.GetForCurrentView();
                    newAppView.Consolidated += NewAppView_Consolidated;
                    newAppView.Title = title;
                    newAppView.TitleBar.BackgroundColor = Windows.UI.ColorHelper.FromArgb(0xff, 0xee, 0xee, 0xee);
                    newAppView.TitleBar.ButtonBackgroundColor = Windows.UI.ColorHelper.FromArgb(0xff, 0xee, 0xee, 0xee);
                    frame = new Frame();
                    frame.CacheSize = 0;
                    frame.Navigate(pageType);
                    newWindow.Content = frame;
                    newWindow.Activate();
                    newViewId = newAppView.Id;
                });
                viewShown = await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId);
            }
        }

        /// <summary>
        /// 窗口已关闭
        /// </summary>
        private void NewAppView_Consolidated(ApplicationView sender, ApplicationViewConsolidatedEventArgs args)
        {
            viewClosed = true;
            if (!CacheView)
            {
                //选择不缓存的页面退出后导航到一个空白页
                frame.Navigate(typeof(Page));
                //清理Frame历史记录
                frame.BackStack.Clear();
                frame.ForwardStack.Clear();
            }
            GC.Collect();
        }
    }
}
