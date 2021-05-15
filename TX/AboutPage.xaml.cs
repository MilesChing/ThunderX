using Microsoft.Toolkit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using TX.Resources.Strings;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Services.Store;
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
        private readonly App CurrentApp = ((App)App.Current);

        public AboutPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            InitializeTrailDaysRemainingText();
            base.OnNavigatedTo(e);
        }

        private void InitializeTrailDaysRemainingText()
        {
            int daysRemained = (int)CurrentApp.AppLicense.TrialTimeRemaining.TotalDays;
            TrailRemainingDatesText.Text = $"{(daysRemained <= 1 ? "≤1" : daysRemained.ToString())} {DaysText}";
        }

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

        private static readonly string DaysText = Loader.Get("Day(s)");
    }
}
