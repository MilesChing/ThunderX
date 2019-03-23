using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace TX.Controls
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
            CoreApplicationView newView = CoreApplication.CreateNewView();
            if (viewShown)
            {
                if (viewClosed)
                {
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
                    frame.HorizontalContentAlignment = HorizontalAlignment.Left;
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
        }
    }
}
