using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using TX.StorageTools;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


namespace TX
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class NewTaskPage : Page
    {
        public NewTaskPage()
        {
            this.RequestedTheme = Settings.DarkMode ? ElementTheme.Dark : ElementTheme.Light;
            ResetTitleBar();

            this.InitializeComponent();
            
            Windows.ApplicationModel.DataTransfer.Clipboard.ContentChanged += Clipboard_ContentChanged;
            
            RefreshUI();
            Clipboard_ContentChanged(this, this);//检查一下剪贴板里有没有url
        }

        private void RefreshUI()
        {
            UrlBox.Text = "";
            NeedRenameButton.IsChecked = false;
            RenameBox.Text = Strings.AppResources.GetString("Null");
            OurAdviceBlock.Text = RenameBox.Text;
            ThreadNumSlider.Value = StorageTools.Settings.ThreadNumber;
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

        /// <summary>
        /// 剪贴板内容变化了，检查是否是有效字符串，当UrlBox内无效且剪贴板有效就更换UrlBox内容
        /// </summary>
        private async void Clipboard_ContentChanged(object sender, object e)
        {
            try
            {
                if (UrlBox.Text == String.Empty || !NetWork.UrlConverter.IsLegal(UrlBox.Text))
                {
                    string url = await NetWork.UrlConverter.CheckClipBoardAsync();
                    if (url != string.Empty) UrlBox.Text = url;
                    CheckUrlBox();
                    GenerateRecommendedName();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// 检查UrlBox中的内容是否合法
        /// </summary>
        private void CheckUrlBox()
        {
            SubmitButton.IsEnabled = false;
            string url = UrlBox.Text;
            url = NetWork.UrlConverter.TranslateURLThunder(url);
            if (url == null) return;
            if (NetWork.UrlConverter.IsLegal(url))
            {
                OurAdviceBlock.Text = System.IO.Path.GetFileName(url);
                OurAdviceBlock.Opacity = 0.5;
                SubmitButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// 输入内容时检测链接是否合法，合法则使submit按钮可用
        /// </summary>
        private void UrlBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckUrlBox();
            GenerateRecommendedName();
        }

        /// <summary>
        /// 显示根据Url推荐的文件名
        /// </summary>
        private void GenerateRecommendedName()
        {
            if (!SubmitButton.IsEnabled) return;
            string url = UrlBox.Text;
            if (url == null) return;
            Task.Run(async () =>
            {
                try
                {
                    HttpWebRequest req = (HttpWebRequest)WebRequest.CreateHttp(url);
                    using (HttpWebResponse rep = (HttpWebResponse)req.GetResponse())
                    {
                        string name = NetWork.HttpNetWorkMethods.GetResponseName(rep);
                        await OurAdviceBlock.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                         {
                             OurAdviceBlock.Text = name;
                             OurAdviceBlock.Opacity = 1;
                         });
                    }
                    if (req != null) req.Abort();
                }
                catch(Exception e)
                {
                    Debug.WriteLine(e.ToString());
                }
            });
        }

        /// <summary>
        /// 点击提交按钮（将关闭窗口）
        /// </summary>
        private async void  SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            string url = UrlBox.Text;
            url = NetWork.UrlConverter.TranslateURLThunder(url);
            Models.InitializeMessage im = new Models.InitializeMessage(url, (bool)(NeedRenameButton.IsChecked) ? RenameBox.Text : null, (int)ThreadNumSlider.Value);
            MainPage.Current.AddDownloadBar(im);
            //由于软件的窗口管理机制要把控件的值重置以准备下次被打开
            RefreshUI();
            await ApplicationView.GetForCurrentView().TryConsolidateAsync();//关闭窗口
        }

        /// <summary>
        /// I need to rename it的选项变化了
        /// </summary>
        private void NeedRenameButton_ValueChanged(object sender, RoutedEventArgs e)
        {
            bool isChecked = (bool)((CheckBox)sender).IsChecked;
            OurAdviceBlock.Visibility = isChecked ? Visibility.Collapsed : Visibility.Visible;
            RenameBox.Visibility = isChecked ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
