using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using TX.Converters;
using TX.Models;
using TX.NetWork;
using TX.NetWork.NetWorkAnalysers;
using TX.StorageTools;
using TX.VisualManager;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


namespace TX
{
    public sealed partial class NewTaskPage : Page
    {
        private VisibilityAnimationManager ThreadLayoutVisibilityManager = null;
        private AbstractAnalyser analyser = null;
        private ObservableCollection<LinkAnalysisMessage> linkAnalysisMessages = new ObservableCollection<LinkAnalysisMessage>();

        public NewTaskPage()
        {
            RequestedTheme = Settings.DarkMode ? ElementTheme.Dark : ElementTheme.Light;
            ResetTitleBar();

            InitializeComponent();
            SetVisualManagers();

            Windows.ApplicationModel.DataTransfer.Clipboard.ContentChanged += Clipboard_ContentChanged;

            RefreshUI();
            Clipboard_ContentChanged(this, this);//检查一下剪贴板里有没有url
        }

        private void RefreshUI()
        {
            //将UI恢复到初始值（窗口的循环利用机制）
            UrlBox.Text = "";
            NeedRenameButton.IsChecked = false;
            RenameBox.Text = Strings.AppResources.GetString("Unknown");
            OurAdviceBlock.Text = RenameBox.Text;
            ThreadNumSlider.Value = StorageTools.Settings.ThreadNumber;
        }

        private void ResetTitleBar()
        {
            // 设置状态栏透明、扩展内容到状态栏
            var TB = ApplicationView.GetForCurrentView().TitleBar;
            byte co = (byte)(Settings.DarkMode ? 0x11 : 0xee);
            byte fr = (byte)(0xff - co);
            TB.BackgroundColor = Color.FromArgb(0xcc, co, co, co);
            TB.ButtonBackgroundColor = Color.FromArgb(0xcc, co, co, co);
            TB.ButtonForegroundColor = Color.FromArgb(0xcc, fr, fr, fr);
        }

        private void SetVisualManagers()
        {
            //设置相关视觉控制器，在构造方法中调用
            ThreadLayoutVisibilityManager = new VisibilityAnimationManager(ThreadLayout);
        }

        private async void Clipboard_ContentChanged(object sender, object e)
        {
            try
            {
                if (!SubmitButton.IsEnabled)
                {
                    string url = await UrlConverter.CheckClipBoardAsync();
                    if (url != string.Empty)
                    {
                        UrlBox.Text = url;
                        UrlBox_TextChanged(null, null);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        private async void UrlBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SubmitButton.IsEnabled = false;
            string url = UrlBox.Text;
            AbstractAnalyser manalyser = UrlConverter.GetAnalyser(url);
            if (manalyser == null)
            {
                ApplyVisualDetail(new NewTaskPageVisualDetail());
                return;
            }
            await manalyser.SetURLAsync(url);
            if (url == UrlBox.Text)
            {
                //确保url不会发生变化
                lock (this)
                {
                    analyser?.Dispose();
                    analyser = manalyser;
                    OurAdviceBlock.Text = System.IO.Path.GetFileName(url);
                    OurAdviceBlock.Opacity = 0.5;
                    if (OurAdviceBlock.Text == string.Empty) OurAdviceBlock.Text = Strings.AppResources.GetString("Unknown");

                    ApplyVisualDetail(analyser.GetVisualDetail());

                    if (analyser.IsLegal())
                    {
                        OurAdviceBlock.Text = analyser.GetRecommendedName();
                        OurAdviceBlock.Opacity = 1;
                        SubmitButton.IsEnabled = true;
                    }
                    else
                    {
                        analyser.Dispose();
                        analyser = null;
                    }
                }
            }
            else manalyser.Dispose();
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            Models.InitializeMessage im = new Models.InitializeMessage(
                analyser.URL,
                (bool)(NeedRenameButton.IsChecked) ? RenameBox.Text : analyser.GetRecommendedName(),
                (int)ThreadNumSlider.Value,
                analyser.GetStreamSize() > 0 ? (long?)analyser.GetStreamSize() : null,
                await StorageManager.GetTemporaryFileAsync());

            MainPage.Current.AddDownloadBar(im, analyser.GetDownloader());
            //由于软件的窗口管理机制要把控件的值重置以准备下次被打开
            RefreshUI();
            await ApplicationView.GetForCurrentView().TryConsolidateAsync();//关闭窗口
        }

        private void NeedRenameButton_ValueChanged(object sender, RoutedEventArgs e)
        {
            bool isChecked = (bool)((CheckBox)sender).IsChecked;
            OurAdviceBlock.Visibility = isChecked ? Visibility.Collapsed : Visibility.Visible;
            RenameBox.Visibility = isChecked ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// 根据当前界面细节做出目标更改
        /// </summary>
        private void ApplyVisualDetail(NewTaskPageVisualDetail detail)
        {
            if (detail.NeedThreadsSlider ^ (ThreadLayoutVisibilityManager.Element.Visibility == Visibility.Visible))
            {
                if (detail.NeedThreadsSlider) ThreadLayoutVisibilityManager.Show();
                else ThreadLayoutVisibilityManager.Hide();
            }

            if (detail.LinkAnalysisMessages != null && linkAnalysisMessages.Count == detail.LinkAnalysisMessages.Length)
            {
                for (int i = 0; i < linkAnalysisMessages.Count; i++)
                {
                    if (detail.LinkAnalysisMessages[i].Message != linkAnalysisMessages[i].Message) break;
                    if (i == linkAnalysisMessages.Count - 1) return;
                }
            }

            linkAnalysisMessages.Clear();

            if (detail.LinkAnalysisMessages != null && detail.LinkAnalysisMessages.Length != 0)
                foreach (LinkAnalysisMessage mes in detail.LinkAnalysisMessages)
                    linkAnalysisMessages.Add(mes);
        }
    }

    /// <summary>
    /// 包含对新任务页面布局做出调整的细节
    /// 用于对目标URL进行额外的界面调整
    /// </summary>
    public class NewTaskPageVisualDetail
    {
        public NewTaskPageVisualDetail(
            bool needThreadsSlider = false,
            LinkAnalysisMessage[] linkAnalysisMessages = null)
        {
            NeedThreadsSlider = needThreadsSlider;
            LinkAnalysisMessages = linkAnalysisMessages;
        }

        /// <summary>
        /// 是否需要选择线程数的滑动栏
        /// </summary>
        public bool NeedThreadsSlider { get; set; }

        /// <summary>
        /// 展示给用户的多行文字提示，每行展示一条
        /// </summary>
        public LinkAnalysisMessage[] LinkAnalysisMessages { get; set; }
    }
}
