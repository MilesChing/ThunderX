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

namespace TX
{
    /// <summary>
    /// 提供特定于应用程序的行为，以补充默认的应用程序类。
    /// </summary>
    sealed partial class App : Application
    {
        public readonly TXCoreManager Core = new TXCoreManager();
        public readonly PersistentActionManager PActionManager = new PersistentActionManager(30);
        private readonly Settings settingEntries = new Settings();
        private readonly Task initializingTask;

        public App()
        {
            this.InitializeComponent(); 
            this.Suspending += OnSuspending;
            this.Resuming += OnResuming;
            initializingTask = InitializeAsync();
        }

        public StoreAppLicense AppLicense { get; private set; } = null;

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
            var storeContext = StoreContext.GetDefault();
            AppLicense = await storeContext.GetAppLicenseAsync();
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

                Core.Suspend();
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
            actions.Add(() => MainPage.Current.NavigateNewTaskPage(targetFileUri));
            D($"Activated by file <{targetFile.Path}>");
            if (!EnsurePageCreatedAndActivate(actions))
            {
                D($"Exist UI content, navigate to new task page");
                foreach (var action in actions) action();
            }
            base.OnFileActivated(args);
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            D($"Application activated by {args.Kind}");
            List<Action> actions = new List<Action>();
            switch (args.Kind)
            {
                case ActivationKind.ToastNotification:
                    var argument = (args as ToastNotificationActivatedEventArgs)?.Argument;
                    actions.AddRange(ToastManager.HandleToastActivation(argument));
                    break;
                case ActivationKind.Protocol:
                    ProtocolActivatedEventArgs protocalArgs = args as ProtocolActivatedEventArgs;
                    D($"Activated by URI <{protocalArgs.Uri.OriginalString}>");
                    actions.Add(() => MainPage.Current.NavigateNewTaskPage(protocalArgs.Uri));
                    break;
            }

            if (!EnsurePageCreatedAndActivate(actions))
            {
                D($"Exist UI content, navigate to new task page");
                foreach (var action in actions) action();
            }

            base.OnActivated(args);
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            D("Application launched");
            EnsurePageCreatedAndActivate();
        }

        private bool EnsurePageCreatedAndActivate(object parameter = null)
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
                rootFrame.Navigate(typeof(MainPage), parameter);
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
