using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using TX.StorageTools;
using Windows.ApplicationModel;
using Windows.Services.Store;
using Windows.System;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace TX
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class AboutPage : Page
    {
        App CurrentApplication = ((App)App.Current);

        public AboutPage()
        {
            this.RequestedTheme = Settings.DarkMode ? ElementTheme.Dark : ElementTheme.Light;
            ResetTitleBar();
            this.InitializeComponent();
            SetVersionName();
            SetThemeChangedListener();
            CurrentApplication.LicenseChanged += CurrentApplication_LicenseChanged;
            CurrentApplication_LicenseChanged(CurrentApplication.AppLicense);
        }

        private void CurrentApplication_LicenseChanged(StoreAppLicense license)
        {
            if (license == null) return;
            if (license.IsActive)
            {
                if (license.IsTrial)
                {
                    TrialPanel.Visibility = Visibility.Visible;
                    ActivePanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    TrialPanel.Visibility = Visibility.Collapsed;
                    ActivePanel.Visibility = Visibility.Visible;
                }
            }
        }

        private async void PurchaseButton_Click(object sender, RoutedEventArgs e)
        {
            var pfn = Windows.ApplicationModel.Package.Current.Id.FamilyName;
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-windows-store://pdp/?PFN="+pfn));
        }

        private async void GithubButton_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri(@"https://github.com/MilesChing"));
        }

        private async void RatingControl_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (((RatingControl)sender).Value < 4) return;
            var pfn = Windows.ApplicationModel.Package.Current.Id.FamilyName;
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-windows-store://review/?PFN=" + pfn));
        }

        private void SetThemeChangedListener()
        {
            CurrentApplication.ThemeChanged += async (theme) =>
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    this.RequestedTheme = theme;
                    ResetTitleBar();
                });
            };
        }

        private void ResetTitleBar()
        {
            var TB = ApplicationView.GetForCurrentView().TitleBar;
            byte co = (byte)(Settings.DarkMode ? 0x11 : 0xee);
            byte fr = (byte)(0xff - co);
            TB.BackgroundColor = Color.FromArgb(0xcc, co, co, co);
            TB.ButtonBackgroundColor = Color.FromArgb(0xcc, co, co, co);
            TB.ButtonForegroundColor = Color.FromArgb(0xcc, fr, fr, fr);
        }

        private void SetVersionName()
        {
            string appVersion = string.Format("VERSION {0}.{1}.{2}.{3} ",
                    Package.Current.Id.Version.Major,
                    Package.Current.Id.Version.Minor,
                    Package.Current.Id.Version.Build,
                    Package.Current.Id.Version.Revision);
            VersionNameBlock.Text = appVersion;
        }
    }
}
