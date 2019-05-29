using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using TX.Models;
using TX.NetWork.NetWorkAnalysers;
using TX.StorageTools;
using Windows.ApplicationModel.DataTransfer;
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

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace TX
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class WebBrowserPage : Page
    {
        private const int MAX_URL_MESSAGE_NUMBER = 20;

        private string CurrentURL = string.Empty;

        private ObservableCollection<URLMessage> URLMessageCollection = new ObservableCollection<URLMessage>();

        public WebBrowserPage()
        {
            this.NavigationCacheMode = NavigationCacheMode.Disabled;
            this.RequestedTheme = Settings.DarkMode ? ElementTheme.Dark : ElementTheme.Light;
            ResetTitleBar();

            this.InitializeComponent();
            
            SetThemeChangedListener();

            MainWebView.NavigationCompleted += MainWebView_NavigationCompleted;
            //将打开新标签页转换为在当前页面打开
            MainWebView.NewWindowRequested += (sender, args) =>
            {
                SafeNavigate(args.Uri.ToString());
                args.Handled = true;
            };
            //填入正确的URL
            URLBox.LostFocus += (sender, args) => { URLBox.Text = CurrentURL; };
            //注册回车键事件
            URLBox.KeyDown += (sender, args) =>
            {
                if (args.Key != Windows.System.VirtualKey.Enter) return;
                SafeNavigate(URLBox.Text);
            };

            MainWebView.NavigationFailed += (sender, e) => { MainProgressBar.IsIndeterminate = false; };
            MainWebView.NavigationStarting += (sender, e) => { MainProgressBar.IsIndeterminate = true; };
            MainWebView.NavigationCompleted += (sender, e) => { MainProgressBar.IsIndeterminate = false; };
            MainWebView.NavigationStarting += (sender, e) => 
            {
                BackwardButton.IsEnabled = MainWebView.CanGoBack;
                ForwardButton.IsEnabled = MainWebView.CanGoForward;
            };
        }

        private void MainWebView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            if (args == null || args.Uri == null) return;
            CurrentURL = args.Uri.ToString();
            URLBox.Text = CurrentURL;
            AddURL(CurrentURL);
        }

        private void HamburgButton_Click(object sender, RoutedEventArgs e)
        {
            MainSplitView.IsPaneOpen ^= true;
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await WebView.ClearTemporaryWebDataAsync();
            CurrentURL = URLBox.Text;
            SafeNavigate(URLBox.Text);
        }

        private void SafeNavigate(string Url)
        {
            try
            {
                string url = Url.ToString();
                if (!url.Contains("://")) url = "http://" + url;
                MainWebView.Navigate(new Uri(url));
            }
            catch (Exception exp)
            {
                MainWebView.NavigateToString(exp.ToString());
            }
        }

        private async void AddURL(string URL)
        {
            AbstractAnalyser analyser = Converters.UrlConverter.GetAnalyser(URL);
            await analyser.SetURLAsync(URL);
            if (analyser.IsLegal())
            {
                URLMessage message = new URLMessage();
                message.URL = URL;
                message.RecommendedFileName = analyser.GetRecommendedName();
                if (message.RecommendedFileName == null) message.RecommendedFileName = string.Empty;
                message.StreamSizeToString = Converters.StringConverter.GetPrintSize(analyser.GetStreamSize());
                URLMessageCollection.Insert(0, message);
                if (URLMessageCollection.Count == MAX_URL_MESSAGE_NUMBER)
                    URLMessageCollection.RemoveAt(MAX_URL_MESSAGE_NUMBER - 1);
            }
            analyser.Dispose();
        }

        /// <summary>
        /// 设置状态栏透明、扩展内容到状态栏
        /// </summary>
        private void ResetTitleBar()
        {
            var TB = ApplicationView.GetForCurrentView().TitleBar;
            byte co = (byte)(Settings.DarkMode ? 0x11 : 0xee);
            byte fr = (byte)(0xff - co);
            TB.BackgroundColor = Color.FromArgb(0xcc, co, co, co);
            TB.ButtonBackgroundColor = Color.FromArgb(0xcc, co, co, co);
            TB.ButtonForegroundColor = Color.FromArgb(0xcc, fr, fr, fr);
        }

        private void SetThemeChangedListener()
        {
            ((App)App.Current).ThemeChanged += WebBrowserPage_ThemeChanged; 
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            //不消除全局事件的话要内存泄漏的喔
            ((App)App.Current).ThemeChanged -= WebBrowserPage_ThemeChanged;
            //清理WebView缓存
            await WebView.ClearTemporaryWebDataAsync();
            base.OnNavigatedFrom(e);
        }

        private async void WebBrowserPage_ThemeChanged(ElementTheme theme)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                this.RequestedTheme = theme;
                ResetTitleBar();
            });
        }

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            AppBarButton button = (AppBarButton)sender;
            Grid parent = (Grid)button.Parent;
            TextBlock urlTextBlock = (TextBlock)parent.Children[0];
            DataPackage dp = new DataPackage();
            dp.SetText(urlTextBlock.Text);
            Clipboard.SetContent(dp);
        }

        private void RefreshPage()
        {
            URLMessageCollection.Clear();
        }

        private void BackwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainWebView.CanGoBack) MainWebView.GoBack();
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainWebView.CanGoForward) MainWebView.GoForward();
        }
    }
}
