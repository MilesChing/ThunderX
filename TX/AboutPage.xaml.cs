using Microsoft.Toolkit.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
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
    public sealed partial class AboutPage : Page
    {
        public AboutPage()
        {
            this.InitializeComponent();
        }

        private bool IsApplicationVersionNotTrail => !((App)App.Current).AppLicense.IsTrial;

        private Visibility ApplicationTrailVersionMessageVisibility =>
            IsApplicationVersionNotTrail ? Visibility.Collapsed : Visibility.Visible;

        private string VersionText =>
            "{0}.{1}.{2}.{3}".AsFormat(
                Package.Current.Id.Version.Major,
                Package.Current.Id.Version.Minor,
                Package.Current.Id.Version.Build,
                Package.Current.Id.Version.Revision);

        private async void BuyButton_Click(object sender, RoutedEventArgs e)
        {
            var pfn = Package.Current.Id.FamilyName;
            await Launcher.LaunchUriAsync(new Uri("ms-windows-store://pdp/?PFN=" + pfn));
        }

        private async void RatingControl_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var pfn = Package.Current.Id.FamilyName;
            await Launcher.LaunchUriAsync(new Uri("ms-windows-store://review/?PFN=" + pfn));
        }
    }
}
