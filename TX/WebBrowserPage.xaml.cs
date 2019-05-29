using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using TX.Models;
using TX.NetWork.NetWorkAnalysers;
using TX.StorageTools;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
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
    public sealed partial class WebBrowserPage : TXPage
    {
        private const int MAX_URL_MESSAGE_NUMBER = 20;

        private string CurrentURL = string.Empty;

        private ObservableCollection<URLMessage> URLMessageCollection = new ObservableCollection<URLMessage>();

        public WebBrowserPage()
        {
            this.NavigationCacheMode = NavigationCacheMode.Disabled;
            this.InitializeComponent();
            SetWebView();
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
                //填充问题页
                GetHtmlTemplateAndRun((content) =>
                {
                    string html = content.Replace("Title", Strings.AppResources.GetString("SomethingWrong"))
                                        .Replace("Text", string.Join(" ", exp.Message, "HResult: " + exp.HResult, "HelpLink: " + exp.HelpLink));
                    MainWebView.NavigateToString(html);
                });
            }
        }

        private async void AddURL(string URL)
        {
            foreach (URLMessage mes in URLMessageCollection)
                if (mes.URL.Equals(URL))
                    return;
            AbstractAnalyser analyser = Converters.UrlConverter.GetAnalyser(URL);
            await analyser.SetURLAsync(URL);
            if (analyser.IsLegal())
            {
                URLMessage message = new URLMessage();
                message.URL = analyser.URL;
                message.RecommendedFileName = analyser.GetRecommendedName();
                if (message.RecommendedFileName == null) message.RecommendedFileName = string.Empty;
                message.StreamSizeToString = Converters.StringConverter.GetPrintSize(analyser.GetStreamSize());
                URLMessageCollection.Insert(0, message);
                if (URLMessageCollection.Count == MAX_URL_MESSAGE_NUMBER)
                    URLMessageCollection.RemoveAt(MAX_URL_MESSAGE_NUMBER - 1);
            }
            analyser.Dispose();
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            await WebView.ClearTemporaryWebDataAsync();
            base.OnNavigatedFrom(e);
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

        private void BackwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainWebView.CanGoBack) MainWebView.GoBack();
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainWebView.CanGoForward) MainWebView.GoForward();
        }

        private void SetWebView()
        {
            //填充空白页
            GetHtmlTemplateAndRun((content) =>
            {
                string html = content.Replace("Title", Strings.AppResources.GetString("WebBrowserPage_Title"))
                                    .Replace("Text", Strings.AppResources.GetString("WebBrowserPage_GuideText"));
                MainWebView.NavigateToString(html);
            });

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

            MainWebView.NavigationFailed += MainWebView_NavigationFailed;
            MainWebView.NavigationStarting += (sender, e) => { MainProgressBar.IsIndeterminate = true; };
            MainWebView.NavigationCompleted += (sender, e) => { MainProgressBar.IsIndeterminate = false; };
            MainWebView.NavigationStarting += (sender, e) =>
            {
                BackwardButton.IsEnabled = MainWebView.CanGoBack;
                ForwardButton.IsEnabled = MainWebView.CanGoForward;
            };
        }

        private void MainWebView_NavigationFailed(object sender, WebViewNavigationFailedEventArgs e)
        {
            //填充问题页
            GetHtmlTemplateAndRun((content) =>
            {
                string html = content.Replace("Title", "ERROR " + (int)e.WebErrorStatus)
                                    .Replace("Text", e.WebErrorStatus + ": " + e.Uri.ToString());
                MainWebView.NavigateToString(html);
            });
            MainProgressBar.IsIndeterminate = false;
        }

        private static string HTMLTemplateContent = null;

        private async Task loadHTMLTemplateAsync()
        {
            Uri uri = new Uri("ms-appx:///Resources/HTMLs/EmptyHtML.html");
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            HTMLTemplateContent = await FileIO.ReadTextAsync(file);
        }

        private async void GetHtmlTemplateAndRun(Action<string> function)
        {
            if (HTMLTemplateContent == null) await loadHTMLTemplateAsync();
            function(HTMLTemplateContent);
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView listView = (ListView)sender;
            listView.SelectedItems.Clear();
        }
    }
}
