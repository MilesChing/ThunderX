using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using TX.StorageTools;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Services.Store;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Newtonsoft.Json;

namespace TX
{
    /// <summary>
    /// 提供特定于应用程序的行为，以补充默认的应用程序类。
    /// </summary>
    sealed partial class App : Application
    {
        private StoreContext storeContext = StoreContext.GetDefault();

        /// <summary>
        /// 当前用户的许可证状态
        /// </summary>
        public StoreAppLicense AppLicense { get; private set; } = null;

        public bool First { get; private set; } = false;

        /// <summary>
        /// 在App主题在Dark/Light间切换时（根据用户设置）调用
        /// </summary>
        public event Action<ElementTheme> ThemeChanged;

        /// <summary>
        /// 用户的许可证状态发生变化
        /// </summary>
        public event Action<StoreAppLicense> LicenseChanged;

        /// <summary>
        /// 宣布一次主题切换，只能由设置Page调用
        /// </summary>
        public void CallThemeUpdate(ElementTheme newTheme)
        {
            ThemeChanged?.Invoke(newTheme);
        }

        /// <summary>
        /// 指示应用是否处于后台
        /// </summary>
        public bool InBackground { get; private set; }

        /// <summary>
        /// 初始化单一实例应用程序对象。这是执行的创作代码的第一行，
        /// 已执行，逻辑上等同于 main() 或 WinMain()。
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            this.EnteredBackground += OnEnteringBackground;
            this.LeavingBackground += OnLeavingBackground;
            this.Resuming += OnResuming;
            storeContext.OfflineLicensesChanged += async (s, e) =>
            {
                AppLicense = await s.GetAppLicenseAsync();
                LicenseChanged?.Invoke(AppLicense);
            };  //处理运行时版本信息更新
            InitializeAppLicense(); //初始化版本信息
            if (Settings.DownloadsFolderToken == null || !StorageApplicationPermissions.MostRecentlyUsedList
                .ContainsItem(Settings.DownloadsFolderToken))
                Settings.DownloadsFolderToken = null;
            TXDataFileIO.StartInitializeMessages(); //不管有没有都要Start
            Converters.ExtentionConverter.InitializeDictionary();
        }

        private async void InitializeAppLicense()
        {
            AppLicense = await storeContext.GetAppLicenseAsync();
            LicenseChanged?.Invoke(AppLicense);
        }

        /// <summary>
        /// 处理从其他方式激活进入软件的对应操作
        /// </summary>
        /// <param name="args"></param>
        protected override async void OnActivated(IActivatedEventArgs args)
        {
            base.OnActivated(args);
            //从Toast通知激活
            if(args.Kind == ActivationKind.ToastNotification)
            {
                //从事件参数中获取预定义的数据和用户输入
                string message = (args as ToastNotificationActivatedEventArgs).Argument;

                string[] arguments = message.Split('$');

                if(arguments.Length == 2 && arguments[0] == "file")
                {
                    StorageFile target;
                    try { target = await StorageFile.GetFileFromPathAsync(arguments[1]); }
                    catch(Exception)
                    {
                        Toasts.ToastManager.ShowSimpleToast(Strings.AppResources.GetString("SomethingWrong"),
                            Strings.AppResources.GetString("FileNotExist"));
                        return;
                    }
                    if(target != null) StorageManager.LaunchFileAsync(target);
                }
                else if (arguments.Length == 2 && arguments[0] == "folder")
                {
                    StorageFolder target;
                    try { target = await StorageFolder.GetFolderFromPathAsync(arguments[1]); }
                    catch (Exception)
                    {
                        Toasts.ToastManager.ShowSimpleToast(Strings.AppResources.GetString("SomethingWrong"),
                            Strings.AppResources.GetString("FolderNotExist"));
                        return;
                    }
                    if (target != null) StorageManager.LaunchFolderAsync(target);
                }
                //如果点击通知打开应用前是未运行状态，就退出程序（否则应保持原界面）
                
                if(args.PreviousExecutionState != ApplicationExecutionState.Running)
                    Current.Exit();
            }
        }

        private void OnResuming(object sender, object e)
        {
            Debug.WriteLine("OnResuming");
            MainPage.Current.DownloadBarManager.Invoke(
                (collection) => { foreach (var bar in collection) bar.downloader.Start(); }
            );
        }

        private void OnLeavingBackground(object sender, LeavingBackgroundEventArgs e)
        {
            InBackground = false;
            Debug.WriteLine("Foreground");
        }

        private void OnEnteringBackground(object sender, EnteredBackgroundEventArgs e)
        {
            InBackground = true;
            Debug.WriteLine("Background");
        }

        /// <summary>
        /// 在应用程序由最终用户正常启动时进行调用。
        /// 将在启动应用程序以打开特定文件等情况下使用。
        /// </summary>
        /// <param name="e">有关启动请求和过程的详细信息。</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // 不要在窗口已包含内容时重复应用程序初始化，
            // 只需确保窗口处于活动状态
            if (rootFrame == null)
            {
                // 创建要充当导航上下文的框架，并导航到第一页
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: 从之前挂起的应用程序加载状态
                }

                // 将框架放在当前窗口中
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // 当导航堆栈尚未还原时，导航到第一页，
                    // 并通过将所需信息作为导航参数传入来配置
                    // 参数
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // 确保当前窗口处于活动状态
                Window.Current.Activate();
            }
        }

        /// <summary>
        /// 导航到特定页失败时调用
        /// </summary>
        ///<param name="sender">导航失败的框架</param>
        ///<param name="e">有关导航失败的详细信息</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// 在将要挂起应用程序执行时调用。
        /// 无需知道应用程序会被终止还是会恢复，
        /// 并让内存内容保持不变。
        /// </summary>
        /// <param name="sender">挂起的请求的源。</param>
        /// <param name="e">有关挂起请求的详细信息。</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            int doneNum = 0;
            Debug.WriteLine("OnSuspending");
            var deferral = e.SuspendingOperation.GetDeferral();

            if (Settings.IsNotificationShownWhenApplicationSuspended)
                Toasts.ToastManager.ShowSimpleToast(
                    Strings.AppResources.GetString("ApplicationSuspendedTitle"),
                    Strings.AppResources.GetString("ApplicationSuspendedMessage")
                );

            //保存应用程序状态并停止任何后台活动
            List<Models.DownloaderMessage> list = new List<Models.DownloaderMessage>();
            MainPage.Current.DownloadBarManager.Invoke((collection) =>
            {
                foreach(Controls.DownloadBar bar in collection)
                {
                    if (bar.downloader.State == Enums.DownloadState.Disposed ||
                        bar.downloader.State == Enums.DownloadState.Uninitialized)
                        continue;

                    if(bar.downloader.State == Enums.DownloadState.Done)
                    {
                        doneNum++;
                        if (doneNum > Settings.NormalRecordNumberParser[Settings.MaximumRecordsIndex])
                            continue;
                    }

                    bar.downloader.Pause();
                    list.Add(bar.downloader.Message);
                }
            });
            
            await TXDataFileIO.SaveDownloadMessagesAsync(list);  //保存未完成的下载
            await StorageTools.StorageManager.GetCleanAsync();
            deferral.Complete();
        }

        private async void FirstRun()
        {
            First = true;
            await Launcher.LaunchUriAsync(new Uri(Settings.HelpLink));
        }
    }
}
