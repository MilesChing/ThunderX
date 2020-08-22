using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TX.Converters;
using TX.Downloaders;
using TX.Models;
using TX.NetWork;
using TX.NetWork.NetWorkAnalysers;
using TX.StorageTools;
using TX.VisualManager;
using Windows.Devices.PointOfService.Provider;
using Windows.Services.Store;
using Windows.Storage.AccessCache;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace TX
{
    public sealed partial class NewTaskPage : TXPage
    {
        public static NewTaskPage Current;

        private VisibilityAnimationManager ThreadLayoutVisibilityManager = null;
        private VisibilityAnimationManager ComboBoxLayoutVisibilityManager = null;

        private AbstractAnalyser analyser = null;

        private ObservableCollection<PlainTextMessage> linkAnalysisMessages 
            = new ObservableCollection<PlainTextMessage>();

        private ObservableCollection<ComboBoxData> comboBoxItems
            = new ObservableCollection<ComboBoxData>();

        private Dictionary<string, PlainTextMessage> existMessages
            = new Dictionary<string, PlainTextMessage>();

        private Action<ComboBoxData> comboBoxItemSelectedCallback;

        public NewTaskPage()
        {
            Current = this;

            InitializeComponent();

            SetVisualManagers();

            RefreshUI();
            LicenseChanged(((App)App.Current).AppLicense);
        }

        protected override void LicenseChanged(StoreAppLicense license)
        {
            base.LicenseChanged(license);
            if (license == null) return;
            if (license.IsActive)
            {
                if (license.IsTrial)
                {
                    ThreadLayout_TrialMessage.Visibility = Visibility.Visible;
                    ThreadNumSlider.IsEnabled = false;
                }
                else
                {
                    ThreadLayout_TrialMessage.Visibility = Visibility.Collapsed;
                    ThreadNumSlider.IsEnabled = true;
                }
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Windows.ApplicationModel.DataTransfer.Clipboard.ContentChanged += Clipboard_ContentChanged;
            syncURLTokenSource?.Dispose();
            syncURLTokenSource = new CancellationTokenSource();
            var token = syncURLTokenSource.Token;
            Task.Run(() => SyncURL(token));
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            Windows.ApplicationModel.DataTransfer.Clipboard.ContentChanged -= Clipboard_ContentChanged;
            syncURLTokenSource?.Cancel();
        }

        public void RefreshUI()
        {
            //将UI恢复到初始值（窗口的循环利用机制）
            StartLoadDownloadFolderPath();
            URLBox.Text = string.Empty;
            NeedRenameButton.IsChecked = false;
            RenameBox.Text = Strings.AppResources.GetString("Unknown");
            RecommendedNameBlock.Opacity = 0.5;
            RecommendedNameBlock.Text = RenameBox.Text;
            ThreadNumSlider.Value = Settings.Instance.ThreadNumber;
            currentFolderToken = Settings.Instance.DownloadsFolderToken;
            GC.Collect();
        }

        public async void StartLoadDownloadFolderPath()
        {
            NowFolderTextBlock.Text = StorageApplicationPermissions.MostRecentlyUsedList.ContainsItem(Settings.Instance.DownloadsFolderToken) ?
                (await StorageApplicationPermissions.MostRecentlyUsedList.GetFolderAsync(Settings.Instance.DownloadsFolderToken)).Path :
                Strings.AppResources.GetString("FolderNotExist");
        }

        private CancellationTokenSource syncURLTokenSource;
        private void SyncURL(CancellationToken token)
        {
            string lastCheckURL = "";
            while (!token.IsCancellationRequested)
            {
                string newURL = lastCheckURL;
                try {
                    Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        newURL = URLBox.Text).AsTask().Wait();

                    if (newURL == lastCheckURL)
                    {
                        Thread.Sleep(50);
                        continue;
                    }
                    analyser?.Dispose();
                    analyser = UrlConverter.GetAnalyser(newURL);
                    if (analyser != null) {
                        analyser.BindVisualController(this);
                        analyser.SetURLAsync(newURL).Wait();
                    }
                }
                catch(Exception e) {
                    Debug.WriteLine(e);
                    continue;
                }
                finally {
                    lastCheckURL = newURL;
                }
            }
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
                    if (url != string.Empty && !SubmitButton.IsEnabled)
                        URLBox.Text = url;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            SubmitButton.IsEnabled = false;
            AbstractDownloader downloader = analyser.GetDownloader();

            Debug.WriteLine(nameof(currentFolderToken) + ": " + currentFolderToken);

            Models.DownloaderSettings settings = new Models.DownloaderSettings()
            {
                Url = analyser.URL,
                FileName = (bool)(NeedRenameButton.IsChecked) ? RenameBox.Text : analyser.GetRecommendedName(),
                Size = analyser.GetStreamSize() > 0 ? (long?)analyser.GetStreamSize() : null,
                Threads = ThreadLayout.Visibility == Visibility.Visible ? (int?)ThreadNumSlider.Value : null,
                FilePath = downloader.NeedTemporaryFilePath ? await StorageManager.GetTemporaryFileAsync() : null,
                FolderToken = currentFolderToken
            };
            
            downloader.SetDownloader(settings);
            MainPage.Current.AddDownloadBar(downloader);
            //由于软件的窗口管理机制要把控件的值重置以准备下次被打开
            RefreshUI();
            
            await ApplicationViewSwitcher.SwitchAsync(MainPage.Current.ViewID);//拉起MainPage
            await ApplicationView.GetForCurrentView().TryConsolidateAsync();//关闭窗口
        }

        private void NeedRenameButton_ValueChanged(object sender, RoutedEventArgs e)
        {
            bool isChecked = (bool)((CheckBox)sender).IsChecked;
            RecommendedNameBlock.Visibility = isChecked ? Visibility.Collapsed : Visibility.Visible;
            RenameBox.Visibility = isChecked ? Visibility.Visible : Visibility.Collapsed;
        }

        public async void ForciblySetURL(string URL)
        {
            await URLBox.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                    () => { URLBox.Text = URL; });
        }

        private string currentFolderToken = Settings.Instance.DownloadsFolderToken;

        private async void FolderButton_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.FileTypeFilter.Add(".");
            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder == null) return;
            currentFolderToken = StorageApplicationPermissions.FutureAccessList.Add(folder);
            NowFolderTextBlock.Text = folder.Path;
        }

        private void ComboBox_SelectionChanged(object _, SelectionChangedEventArgs e) =>
            comboBoxItemSelectedCallback?.Invoke(ComboBox.SelectedItem as ComboBoxData);

        // methods for analyzer
        public void UpdateMessage(string key, PlainTextMessage message)
        {
            Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (existMessages.ContainsKey(key))
                {
                    PlainTextMessage intermes = existMessages[key];
                    if (intermes.Equals(message))
                        return;
                    for (int i = 0; i < linkAnalysisMessages.Count; ++i)
                        if (linkAnalysisMessages[i].Equals(intermes))
                        {
                            linkAnalysisMessages.RemoveAt(i);
                            linkAnalysisMessages.Insert(i, message);
                            break;
                        }
                }
                else linkAnalysisMessages.Add(message);
                existMessages[key] = message;
            }).AsTask().Wait();
        }

        public void RemoveMessage(string key)
        {
            Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (existMessages.ContainsKey(key))
                {
                    linkAnalysisMessages.Remove(existMessages[key]);
                    existMessages.Remove(key);
                }
            }).AsTask().Wait();
        }

        public void SetThreadLayoutVisibility(bool visible)
        {
            Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (visible) ThreadLayoutVisibilityManager.Show();
                else ThreadLayoutVisibilityManager.Hide();
            }).AsTask().Wait();
        }

        public void SetComboBoxLayoutVisibility(bool visible)
        {
            Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (visible) ComboBoxLayoutVisibilityManager.Show();
                else ComboBoxLayoutVisibilityManager.Hide();
            }).AsTask().Wait();
        }

        public void SetSubmitButtonEnabled(bool enable)
        {
            Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                SubmitButton.IsEnabled = enable;
            }).AsTask().Wait();
        }

        public void SetRecommendedName(string name, double opacity)
        {
            Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                RecommendedNameBlock.Text = name;
                RecommendedNameBlock.Opacity = opacity;
            }).AsTask().Wait();
        }

        public void SetVersionSelector(ComboBoxData[] items, Action<ComboBoxData> itemSelected)
        {
            Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                comboBoxItems.Clear();
                foreach (var item in items)
                    comboBoxItems.Add(item);
                comboBoxItemSelectedCallback = itemSelected;
            }).AsTask().Wait();
        }
    }
}
