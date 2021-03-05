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
        private Action LeavePageEventHandler = null;

        public MainPage()
        {
            Current = this;
            LeavePageEventHandler = LeaveEmptyPage;
            InitializeComponent();

            this.ActualThemeChanged += (sender, e) => UpdateTitleBar();
            RequestedTheme = SettingEntries.IsDarkModeEnabled 
                ? ElementTheme.Dark : ElementTheme.Default;
            UpdateTitleBar();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            LeftFrame.Navigate(typeof(TaskList));
            await CurrentApp.WaitForInitializingAsync();

            if (e.Parameter is Uri uri)
                NavigateRightFrame(typeof(NewTaskPage), uri);
            else NavigateRightFrame(typeof(AboutPage), null);

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
            NavigateRightFrame(typeof(TaskDetailPage), downloader);
            DetailPage.Content = downloader.DownloadTask.Target.SuggestedName;
            DetailPage.Visibility = Visibility.Visible;
            (MainNavigationView.Parent as FrameworkElement)?.UpdateLayout();
            MainNavigationView.SelectedItem = DetailPage;
            LeavePageEventHandler = LeaveDownloaderDetailPage;
        }

        public void NavigateEmptyPage()
        {
            NavigateRightFrame(typeof(Page), null);
            MainNavigationView.SelectedItem = null;
            MainNavigationView.IsBackEnabled = false;
            LeavePageEventHandler = LeaveEmptyPage;
        }

        public void NavigateNewTaskPage(Uri uri = null) =>
            NavigateRightFrame(typeof(NewTaskPage), uri);

        private void LeaveEmptyPage()
        {
            MainNavigationView.IsBackEnabled = true;
        }

        private void LeaveDownloaderDetailPage()
        {
            DetailPage.Content = null;
            DetailPage.Visibility = Visibility.Collapsed;
        }

        private void NavigateRightFrame(Type pageType, object parameter)
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
                    NavigateRightFrame(typeof(HistoryListPage), null);
                else if (invokedStr.Equals(AddItem.Content))
                    NavigateRightFrame(typeof(NewTaskPage), null);
                else if (invokedStr.Equals(SetItem.Content))
                    NavigateRightFrame(typeof(SetPage), null);
                else if (invokedStr.Equals(AboutItem.Content))
                    NavigateRightFrame(typeof(AboutPage), null);
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

