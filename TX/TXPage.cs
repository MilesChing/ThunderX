using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.StorageTools;
using Windows.Services.Store;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace TX
{
    /// <summary>
    /// 抽象出的页面类（非抽象类）
    /// 用于标题栏颜色更改以及
    /// 主题变更、License变更等全局事件的注册与消除
    /// </summary>
    public class TXPage : Page
    {
        public TXPage()
        {
            RequestedTheme = Settings.Instance.DarkMode ? ElementTheme.Dark : ElementTheme.Light;
            ResetTitleBar();
        }

        private async void TXPage_ThemeChanged(ElementTheme theme)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                this.RequestedTheme = theme;
                ResetTitleBar();
                RequestedThemeChanged();
            });
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ((App)App.Current).ThemeChanged += TXPage_ThemeChanged;
            ((App)App.Current).LicenseChanged += LicenseChanged;
            base.OnNavigatedTo(e);
        }

        protected virtual void LicenseChanged(StoreAppLicense license) { }

        protected virtual void RequestedThemeChanged() { }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            ((App)App.Current).LicenseChanged -= LicenseChanged;
            ((App)App.Current).ThemeChanged -= TXPage_ThemeChanged;
            base.OnNavigatedFrom(e);
        }

        private void ResetTitleBar()
        {
            var TB = ApplicationView.GetForCurrentView().TitleBar;
            byte co = (byte)(Settings.Instance.DarkMode ? 0x11 : 0xee);
            byte fr = (byte)(0xff - co);
            TB.BackgroundColor = Color.FromArgb(0xcc, co, co, co);
            TB.ButtonBackgroundColor = Color.FromArgb(0xcc, co, co, co);
            TB.ButtonForegroundColor = Color.FromArgb(0xcc, fr, fr, fr);
        }
    }
}
