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
    public sealed partial class AboutPage : TXPage
    {
        App CurrentApplication = ((App)App.Current);

        public AboutPage()
        {
            this.InitializeComponent();
            LicenseChanged(((App)App.Current).AppLicense);
            SetVersionName();
        }

        protected override void LicenseChanged(StoreAppLicense license)
        {
            if (license == null) return;
            if (license.IsActive)
            {
                if (false)
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

        private async void RatingControl_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (((RatingControl)sender).Value < 4) return;
            var pfn = Windows.ApplicationModel.Package.Current.Id.FamilyName;
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-windows-store://review/?PFN=" + pfn));
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

        private async void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri(Settings.HelpLink));
        }
    }
}
