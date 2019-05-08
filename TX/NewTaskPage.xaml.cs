﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using TX.Converters;
using TX.Downloaders;
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
        private object urlAnalyseLock = new object();

        private VisibilityAnimationManager ThreadLayoutVisibilityManager = null;
        private VisibilityAnimationManager ComboBoxLayoutVisibilityManager = null;

        private AbstractAnalyser analyser = null;
        private NewTaskPageVisualController controller = null;

        private ObservableCollection<LinkAnalysisMessage> linkAnalysisMessages 
            = new ObservableCollection<LinkAnalysisMessage>();

        private ObservableCollection<PlainTextComboBoxData> comboBoxItems
            = new ObservableCollection<PlainTextComboBoxData>();

        public NewTaskPage()
        {
            RequestedTheme = Settings.DarkMode ? ElementTheme.Dark : ElementTheme.Light;
            SetThemeChangedListener();
            ResetTitleBar();

            InitializeComponent();

            SetVisualManagers();
            controller = new NewTaskPageVisualController(linkAnalysisMessages,
                ThreadLayoutVisibilityManager,
                ComboBoxLayoutVisibilityManager,
                SubmitButton,
                RecommendedNameBlock,
                ComboBox,
                comboBoxItems);

            Windows.ApplicationModel.DataTransfer.Clipboard.ContentChanged += Clipboard_ContentChanged;

            RefreshUI();
        }

        private void RefreshUI()
        {
            //将UI恢复到初始值（窗口的循环利用机制）
            UrlBox.Text = "";
            NeedRenameButton.IsChecked = false;
            RenameBox.Text = Strings.AppResources.GetString("Unknown");
            RecommendedNameBlock.Text = RenameBox.Text;
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
            ComboBoxLayoutVisibilityManager = new VisibilityAnimationManager(ComboBoxLayout);
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
            string url = UrlBox.Text;
            
            analyser?.Dispose();
            analyser = UrlConverter.GetAnalyser(url);
            if (analyser == null) return;

            controller.RegistAnalyser(null, analyser);
            analyser.BindVisualController(controller);
            await analyser.SetURLAsync(url);
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            SubmitButton.IsEnabled = false;
            AbstractDownloader downloader = analyser.GetDownloader();

            Models.InitializeMessage im = new Models.InitializeMessage(
                analyser.URL,
                (bool)(NeedRenameButton.IsChecked) ? RenameBox.Text : analyser.GetRecommendedName(),
                (int)ThreadNumSlider.Value,
                analyser.GetStreamSize() > 0 ? (long?)analyser.GetStreamSize() : null,
                downloader.NeedTemporaryFilePath ? await StorageManager.GetTemporaryFileAsync() : null);

            downloader.SetDownloader(im);
            downloader.MaxiMaximumRetries = 10;
            MainPage.Current.AddDownloadBar(downloader);
            //由于软件的窗口管理机制要把控件的值重置以准备下次被打开
            RefreshUI();
            await ApplicationView.GetForCurrentView().TryConsolidateAsync();//关闭窗口
        }

        private void NeedRenameButton_ValueChanged(object sender, RoutedEventArgs e)
        {
            bool isChecked = (bool)((CheckBox)sender).IsChecked;
            RecommendedNameBlock.Visibility = isChecked ? Visibility.Collapsed : Visibility.Visible;
            RenameBox.Visibility = isChecked ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SetThemeChangedListener()
        {
            ((App)App.Current).ThemeChanged += async (theme) =>
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    this.RequestedTheme = theme;
                    ResetTitleBar();
                });
            };
        }

        private void ClearVisualStates()
        {
            SubmitButton.IsEnabled = false;
            ThreadLayoutVisibilityManager.Hide();
            ComboBoxLayoutVisibilityManager.Hide();
            RecommendedNameBlock.Text = Strings.AppResources.GetString("Unknown");
            RecommendedNameBlock.Opacity = 0.5;
            linkAnalysisMessages.Clear();
            comboBoxItems.Clear();
        }
    }
}
