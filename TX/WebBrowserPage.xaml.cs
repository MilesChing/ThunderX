using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
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
        private const int MAX_URL_MESSAGE_NUMBER = 100;

        private string CurrentURL = string.Empty;

        private ObservableCollection<URLMessage> URLMessageCollection = new ObservableCollection<URLMessage>();

        public WebBrowserPage()
        {
            this.NavigationCacheMode = NavigationCacheMode.Disabled;
            this.InitializeComponent();
            SetWebView();
        }

        private void SafeNavigateAsync(string Url)
        {
            //不使用异步写法的话，MainWebView.Navigate可能阻塞线程
            Task.Run(async () =>
            {
                try
                {
                    string url = Url.ToString();
                    if (!url.Contains("://")) url = "http://" + url;
                    await MainWebView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                        () => { MainWebView.Navigate(new Uri(url)); });                   
                }
                catch (Exception exp)
                {
                    //填充问题页
                    GetHtmlTemplateAndRun(async (content) =>
                    {
                        string html = content.Replace("Title", Strings.AppResources.GetString("SomethingWrong"))
                                            .Replace("Text", string.Join(" ", exp.Message, "HResult: " + exp.HResult, "HelpLink: " + exp.HelpLink));
                        await MainWebView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                            () => { MainWebView.NavigateToString(html); });
                    });
                }
            });
        }

        //得到了一个新URL，分析其文件名，文件大小
        private async void AddURL(string URL)
        {
            AbstractAnalyser analyser = Converters.UrlConverter.GetAnalyser(URL);
            await analyser.SetURLAsync(URL);
            if (analyser.IsLegal())
            {
                URLMessage message = new URLMessage();
                message.URL = analyser.URL;
                message.StreamSize = analyser.GetStreamSize();
                message.RecommendedFileName = analyser.GetRecommendedName();
                if (message.RecommendedFileName == null) message.RecommendedFileName = string.Empty;
                message.StreamSizeToString = Converters.StringConverter.GetPrintSize(analyser.GetStreamSize());
                AddListViewItem(message);
                LimitListLength();
            }
            analyser.Dispose();
        }

        //定义了向Collection中加入URL的排序规则（插入排序）
        private void AddListViewItem(URLMessage message)
        {
            if (message.IsHTML()) return;

            if (URLMessageCollection.Count == 0)
            {
                URLMessageCollection.Add(message);
                return;
            }

            for (int i = 0; i < URLMessageCollection.Count; ++i)
            {
                var cur = URLMessageCollection[i];
                if (cur.URL.Equals(message.URL))
                    return;
                if (message.StreamSize > cur.StreamSize)
                {
                    URLMessageCollection.Insert(i, message);
                    return;
                }
            }

            URLMessageCollection.Append(message);
        }

        //约束链接列表最大数量
        private void LimitListLength()
        {
            if (URLMessageCollection.Count > MAX_URL_MESSAGE_NUMBER)
                URLMessageCollection.RemoveAt(MAX_URL_MESSAGE_NUMBER);
        }

        //分析当前页
        private async void AnalyseWebPage()
        {
            SideBarProgress.IsIndeterminate = true;
            try
            {
                var html = await MainWebView.InvokeScriptAsync("eval", new string[] { "document.documentElement.outerHTML;" });
                var urls = Converters.StringConverter.PickURLFromHTML(html);
                foreach (string p in urls)
                    AddURL(p);
            }
            catch (Exception) { }
            SideBarProgress.IsIndeterminate = false;
        }

        //进行MainWebView的初始化设置
        private void SetWebView()
        {
            //填充空白页
            GetHtmlTemplateAndRun((content) =>
            {
                string html = content.Replace("Title", Strings.AppResources.GetString("WebBrowserPage_Title"))
                                    .Replace("Text", Strings.AppResources.GetString("WebBrowserPage_GuideText"));
                MainWebView.NavigateToString(html);
            });
            MainWebView.NavigationFailed += MainWebView_NavigationFailed;
            MainWebView.NavigationCompleted += MainWebView_NavigationCompleted;

            //将打开新标签页转换为在当前页面打开
            MainWebView.NewWindowRequested += (sender, args) =>
            {
                SafeNavigateAsync(args.Uri.ToString());
                args.Handled = true;
            };

            //注册回车键事件
            URLBox.KeyDown += (sender, args) =>
            {
                if (args.Key != Windows.System.VirtualKey.Enter) return;
                SafeNavigateAsync(URLBox.Text);
            };

            MainWebView.NavigationStarting += (sender, e) => { MainProgressBar.Value = 0.2; MainProgressBar.Opacity = 1; };
            MainWebView.NavigationCompleted += (sender, e) => { MainProgressBar.Value = 1; HideProgressBarStoryboard.Begin(); };
            MainWebView.NavigationStarting += (sender, e) =>
            {
                BackwardButton.IsEnabled = MainWebView.CanGoBack;
                ForwardButton.IsEnabled = MainWebView.CanGoForward;
            };
        }

        private static string _HTMLTemplateContent = null;

        private async Task _loadHTMLTemplateAsync()
        {
            Uri uri = new Uri("ms-appx:///Resources/HTMLs/EmptyHtML.html");
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            _HTMLTemplateContent = await FileIO.ReadTextAsync(file);
        }

        //获得一段HTML模板，包含标题和正文，并调用function函数
        private async void GetHtmlTemplateAndRun(Action<string> function)
        {
            if (_HTMLTemplateContent == null) await _loadHTMLTemplateAsync();
            function(_HTMLTemplateContent);
        }

        private void MainWebView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            if (args == null || args.Uri == null || !args.IsSuccess) return;
            URLMessageCollection.Clear();
            CurrentURL = args.Uri.ToString();
            URLBox.Text = CurrentURL;
            AddURL(CurrentURL);
            AnalyseWebPage();
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

        private void HamburgButton_Click(object sender, RoutedEventArgs e)
        {
            MainSplitView.IsPaneOpen ^= true;
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await WebView.ClearTemporaryWebDataAsync();
            CurrentURL = URLBox.Text;
            SafeNavigateAsync(URLBox.Text);
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            await WebView.ClearTemporaryWebDataAsync();
            base.OnNavigatedFrom(e);
        }

        private void AddNewButton_Click(object sender, RoutedEventArgs e)
        {
            AppBarButton button = (AppBarButton)sender;
            Grid parent = (Grid)button.Parent;
            TextBlock urlTextBlock = (TextBlock)parent.Children[0];
            DataPackage dp = new DataPackage();
            dp.SetText(urlTextBlock.Text);
            Clipboard.SetContent(dp);
            MainPage.Current.OpenNewTask(urlTextBlock.Text);
        }

        private void BackwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainWebView.CanGoBack) MainWebView.GoBack();
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainWebView.CanGoForward) MainWebView.GoForward();
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView listView = (ListView)sender;
            listView.SelectedItems.Clear();
        }
    }
}
