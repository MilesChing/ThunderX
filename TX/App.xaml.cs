using System;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using TX.Core.Downloaders;
using System.Collections.ObjectModel;
using TX.Core.Providers;
using Windows.UI.ViewManagement;
using TX.Core.Models.Contexts;
using System.Collections.Generic;
using TX.Core;
using Windows.Storage;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Diagnostics;
using Windows.System;
using TX.Utils;
using Windows.ApplicationModel.Core;
using Windows.Services.Store;

namespace TX
{
    /// <summary>
    /// 提供特定于应用程序的行为，以补充默认的应用程序类。
    /// </summary>
    sealed partial class App : Application
    {
        public readonly TXCoreManager Core = new TXCoreManager();

        /// <summary>
        /// 初始化单一实例应用程序对象。这是执行的创作代码的第一行，
        /// 已执行，逻辑上等同于 main() 或 WinMain()。
        /// </summary>
        public App()
        {
            this.InitializeComponent(); 
            this.Suspending += OnSuspending;
            initializingTask = InitializeAsync();
        }
        
        public StoreAppLicense AppLicense { get; private set; } = null;

        public async Task WaitForInitializingAsync() => await initializingTask;

        private Task initializingTask;

        private async Task InitializeAsync()
        {
            try
            {
                var storeContext = StoreContext.GetDefault();
                AppLicense = await storeContext.GetAppLicenseAsync();

                var cacheFile = await GetCacheFileAsync();
                var buffer = await FileIO.ReadBufferAsync(cacheFile);
                Core.Initialize(buffer.ToArray());
            }
            catch (Exception e)
            {
                Debug.WriteLine("Cache file reading failed: " + e.Message);
            }
        }

        private async Task<StorageFile> GetCacheFileAsync()
            => await ApplicationData.Current.LocalFolder.CreateFileAsync(
                "TX_DATA.dat", CreationCollisionOption.OpenIfExists);

        private async void OnSuspending(object sender, Windows.ApplicationModel.SuspendingEventArgs s)
        {
            var deferral = s.SuspendingOperation.GetDeferral();
            try
            {
                try
                {
                    var cacheFile = await GetCacheFileAsync();
                    var props = await cacheFile.GetBasicPropertiesAsync();
                    if (props.Size > 0)
                    {
                        Debug.WriteLine("[App] recreating database");
                        await cacheFile.DeleteAsync();
                        cacheFile = await GetCacheFileAsync();
                        Debug.WriteLine("[App] database recreated");
                    }
                    await FileIO.WriteBytesAsync(
                        cacheFile,
                        Core.ToPersistentByteArray()
                    );
                    Debug.WriteLine("[App] database writen");
                }
                catch (Exception e)
                {
                    Debug.WriteLine("[App] database writing failed: " + e.Message);
                }

                Core.Dispose();

                if (new Settings().IsNotificationEnabledWhenApplicationSuspended)
                    ToastManager.ShowSimpleToast("Thunder X Suspended", "Running tasks have been temporarily cancelled.");
            }
            finally
            {
                deferral.Complete();
            }
        }

        protected override async void OnActivated(IActivatedEventArgs args)
        {
            base.OnActivated(args);
            if (args.Kind == ActivationKind.ToastNotification)
            {
                string message = (args as ToastNotificationActivatedEventArgs).Argument;
                string[] arguments = message.Split('$');
                if (arguments.Length == 2 && arguments[0] == "file")
                {
                    StorageFile target;
                    try { target = await StorageFile.GetFileFromPathAsync(arguments[1]); }
                    catch (Exception)
                    {
                        return;
                    }
                    if (target != null) await Launcher.LaunchFileAsync(target);
                }
                else if (arguments.Length == 2 && arguments[0] == "folder")
                {
                    StorageFolder target;
                    try { target = await StorageFolder.GetFolderFromPathAsync(arguments[1]); }
                    catch (Exception)
                    {
                        return;
                    }
                    if (target != null) 
                        await Launcher.LaunchFolderAsync(target);
                }

                if (args.PreviousExecutionState != ApplicationExecutionState.Running)
                    Current.Exit();
            }
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
    }
}
