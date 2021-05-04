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
using MonoTorrent.Client;
using Windows.Storage.Pickers;
using Windows.Storage.AccessCache;
using MonoTorrent;
using TX.Background;
using Windows.ApplicationModel.Background;
using TX.Core.Utils;
using System.Linq;
using TX.PersistentActions;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using TX.Controls;

namespace TX
{
    /// <summary>
    /// 提供特定于应用程序的行为，以补充默认的应用程序类。
    /// </summary>
    sealed partial class App : Application
    {
        public readonly TXCoreManager Core = new TXCoreManager();
        public readonly PersistentActionManager PActionManager = new PersistentActionManager(30);
        public readonly StartUpActionManager StupActionManager = new StartUpActionManager();
        private readonly Settings settingEntries = new Settings();
        private readonly Task initializingTask;

        public App()
        {
            AppCenter.Start("5e098f14-cfbd-4f0f-9e27-715ca88e06b3",
                   typeof(Analytics), typeof(Crashes));
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            this.Resuming += OnResuming;
            initializingTask = InitializeAsync();
        }

#if DEBUG_TRAIL
        public class FakeTrailAppLicense 
        {
            public bool IsTrial => true;

            public TimeSpan TrialTimeRemaining => TimeSpan.FromDays(1.0);
        }

        public FakeTrailAppLicense AppLicense { get; } = new FakeTrailAppLicense();
#else
        public StoreAppLicense AppLicense { get; private set; } = null;
#endif

        public async Task WaitForInitializingAsync() => await initializingTask;

        private async Task InitializeAsync()
        {
            D("Application initializing");
            await InitializeAppLicenceAsync();
            await InitializeBackgroundTaskAsync();
            await Core.InitializeAsync(await ReadLocalStorageAsync());
            D("Application initialized");
        }

        private async Task<byte[]> ReadLocalStorageAsync()
        {
            try
            {
                var cacheFile = await GetCacheFileAsync();
                var res = (await FileIO.ReadBufferAsync(cacheFile)).ToArray();
                D($"Database loaded, totally {res.Length} bytes");
                return res;
            }
            catch (Exception e)
            {
                D($"Loading database failed: {e.Message}");
                return null;
            }
        }

        private async Task InitializeAppLicenceAsync()
        {
#if !DEBUG_TRAIL
            var storeContext = StoreContext.GetDefault();
            AppLicense = await storeContext.GetAppLicenseAsync();
#endif
            D("Application license obtained");
        }

        private async Task InitializeBackgroundTaskAsync()
        {
            switch (BackgroundExecutionManager.GetAccessStatus())
            {
                case BackgroundAccessStatus.AllowedSubjectToSystemPolicy:
                case BackgroundAccessStatus.AlwaysAllowed:
                    break;
                default:
                    BackgroundExecutionManager.RemoveAccess();
                    await BackgroundExecutionManager.RequestAccessAsync();
                    break;
            }

            D("Background execution access checked");
            BackgroundTaskManager.UnregisterTasks();
            BackgroundTaskManager.RegisterTasks();
            D("Background task reregistered");
        }

        private async Task<StorageFile> GetCacheFileAsync()
            => await ApplicationData.Current.LocalFolder.CreateFileAsync(
                "TX_DATA.dat", CreationCollisionOption.OpenIfExists);

        private async void OnSuspending(object sender, Windows.ApplicationModel.SuspendingEventArgs s)
        {
            D("Application suspending");
            var deferral = s.SuspendingOperation.GetDeferral();
            try
            {
                try
                {
                    var cacheFile = await GetCacheFileAsync();
                    var props = await cacheFile.GetBasicPropertiesAsync();
                    D($"Database file obtained, size {((long)props.Size).SizedString()}");
                    if (props.Size > 0)
                    {
                        await cacheFile.DeleteAsync();
                        cacheFile = await GetCacheFileAsync();
                        D("Database file refreshed");
                    }
                    await FileIO.WriteBytesAsync(
                        cacheFile,
                        Core.ToPersistentByteArray()
                    ); 
                    D("Database written");
                }
                catch (Exception e)
                {
                    D($"Database writting failed: {e.Message}");
                }

                await Core.SuspendAsync();
                D($"{nameof(Core)} suspended");

                PActionManager.Save();
                D($"{nameof(PActionManager)} saved");

                BackgroundTaskManager.UnregisterTasks();
                BackgroundTaskManager.RegisterTasks();
            }
            finally
            {
                D("Application suspended");
                deferral.Complete();
            }
        }

        private void OnResuming(object sender, object e)
        {
            D("Application resuming");
            Core.Resume();
            D("Application resumed");
        }

        protected override void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            base.OnBackgroundActivated(args);
            D("Application activated in background");
            BackgroundTaskManager.ActivateTask(args.TaskInstance);
        }

        protected override void OnFileActivated(FileActivatedEventArgs args)
        {
            List<Action> actions = new List<Action>();
            var targetFile = args.Files.FirstOrDefault();
            StorageApplicationPermissions.FutureAccessList.Add(targetFile);
            var targetFileUri = new Uri(targetFile.Path);
            StupActionManager.Register(() => MainPage.Current.NavigateNewTaskPage(targetFileUri));
            D($"Activated by file <{targetFile.Path}>");
            if (!EnsurePageCreatedAndActivate())
            {
                D($"Exist UI content, navigate to new task page");
                StupActionManager.Do();
            }
            base.OnFileActivated(args);
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            D($"Application activated by {args.Kind}");
            switch (args.Kind)
            {
                case ActivationKind.ToastNotification:
                    var argument = (args as ToastNotificationActivatedEventArgs)?.Argument;
                    ToastManager.HandleToastActivation(argument);
                    break;
                case ActivationKind.Protocol:
                    try
                    {
                        ProtocolActivatedEventArgs protocalArgs = args as ProtocolActivatedEventArgs;
                        D($"Activated by URI <{protocalArgs.Uri.OriginalString}>");
                        StupActionManager.Register(() => MainPage.Current.NavigateNewTaskPage(protocalArgs.Uri));
                    }
                    catch (Exception e)
                    {
                        D($"Handling protocol activation failed: {e.Message}");
                        StupActionManager.Register(() => MainPage.Current.NavigateNewTaskPage());
                        ToastManager.ProtocolActivationErrorToast(e);
                    }
                    break;
            }

            if (!EnsurePageCreatedAndActivate())
            {
                D($"Exist UI content, navigate to new task page");
                StupActionManager.Do();
            }

            base.OnActivated(args);
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            D("Application launched");
            EnsurePageCreatedAndActivate();
        }

        private bool EnsurePageCreatedAndActivate()
        {
            if (!(Window.Current.Content is Frame rootFrame))
            {
                D("Root frame is null, create it");
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                Window.Current.Content = rootFrame;
            }

            bool res = (rootFrame.Content == null);
            if (rootFrame.Content == null)
            {
                D("No content in root frame, navigate to MainPage");
                rootFrame.Navigate(typeof(MainPage));
            }
            Window.Current.Activate();
            return res;
        }

        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        private void D(string message) => Debug.WriteLine($"[{nameof(App)}] {message}");
    }
}
