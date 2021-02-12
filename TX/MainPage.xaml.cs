using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Storage;
using TX.Core.Downloaders;
using TX.Core.Models.Sources;
using TX.Utils;
using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;
using Windows.UI;
using Windows.UI.Xaml.Navigation;

namespace TX
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        App CurrentApp => ((App)App.Current);

        public static MainPage Current { get; private set; }

        private static readonly Settings SettingEntries = new Settings();

        public MainPage()
        {
            Current = this;
            LeavePageEventHandler = LeaveEmptyPage;
            InitializeComponent();

            this.ActualThemeChanged += (sender, e) => UpdateTitleBar();
            RequestedTheme = SettingEntries.IsDarkModeEnabled 
                ? ElementTheme.Dark : ElementTheme.Default;
            UpdateTitleBar();

            LeftFrame.Navigate(typeof(TaskList));
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await CurrentApp.WaitForInitializingAsync();
            InvokeRightFrame(typeof(AboutPage), null);
            LoadingView.Visibility = Visibility.Collapsed;
        }

        private void UpdateTitleBar()
        {
            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = titleBar.BackgroundColor = Colors.Transparent;
            if (RequestedTheme == ElementTheme.Dark)
                titleBar.ButtonForegroundColor = Colors.White;
            else titleBar.ButtonForegroundColor = Colors.Black;
            Window.Current.SetTitleBar(AppTitleBar);
        }

        public void NavigateDownloaderDetailPage(AbstractDownloader downloader)
        {
            InvokeRightFrame(typeof(TaskDetailPage), downloader);
            DetailPage.Content = downloader.DownloadTask.Target.SuggestedName;
            DetailPage.Visibility = Visibility.Visible;
            (MainNavigationView.Parent as FrameworkElement)?.UpdateLayout();
            MainNavigationView.SelectedItem = DetailPage;
            LeavePageEventHandler = LeaveDownloaderDetailPage;
        }

        public void NavigateEmptyPage()
        {
            InvokeRightFrame(typeof(Page), null);
            MainNavigationView.SelectedItem = null;
            MainNavigationView.IsBackEnabled = false;
            LeavePageEventHandler = LeaveEmptyPage;
        }

        private Action LeavePageEventHandler = null;

        private void LeaveEmptyPage()
        {
            MainNavigationView.IsBackEnabled = true;
        }

        private void LeaveDownloaderDetailPage()
        {
            DetailPage.Content = null;
            DetailPage.Visibility = Visibility.Collapsed;
        }

        private void InvokeRightFrame(Type pageType, object parameter)
        {
            LeavePageEventHandler?.Invoke();
            LeavePageEventHandler = null;
            RightFrame.Navigate(pageType, parameter);
        }

        private void MainNavigationView_ItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem is string invokedStr)
            {
                if (invokedStr.Equals(ListItem.Content))
                    InvokeRightFrame(typeof(HistoryListPage), null);
                else if (invokedStr.Equals(AddItem.Content))
                    InvokeRightFrame(typeof(NewTaskPage), null);
                else if (invokedStr.Equals(SetItem.Content))
                    InvokeRightFrame(typeof(SetPage), null);
                else if (invokedStr.Equals(AboutItem.Content))
                    InvokeRightFrame(typeof(AboutPage), null);
            }
        }

        private void MainNavigationView_BackRequested(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewBackRequestedEventArgs args) =>
            NavigateEmptyPage();

        public string GetAppTitleFromSystem()
        {
            return Windows.ApplicationModel.Package.Current.DisplayName;
        }
    }
}

