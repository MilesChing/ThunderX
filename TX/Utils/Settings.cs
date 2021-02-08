using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace TX.Utils
{
    class Settings
    {
        #region Appearance Settings

        /// <summary>
        /// Is dark mode enabled.
        /// </summary>
        public bool IsDarkModeEnabled
        {
            get { return TryGetValue(nameof(IsDarkModeEnabled), false); }
            set { SetValue(nameof(IsDarkModeEnabled), value); }
        }

        #endregion

        #region Downloads Folder

        /// <summary>
        /// Token of the download folder.
        /// </summary>
        public string DownloadsFolderToken
        {
            get { return TryGetValue<string>(nameof(DownloadsFolderToken), null); }
            set { SetValue(nameof(DownloadsFolderToken), value); }
        }

        #endregion

        #region Downloader Settings

        /// <summary>
        /// Maximum retries after single task failed.
        /// </summary>
        public int MaximumRetries
        {
            get
            {
                if (IsApplicationVersionTrail) return 0;
                return TryGetValue(nameof(MaximumRetries), 8); 
            }
            set { SetValue(nameof(MaximumRetries), value); }
        }

        /// <summary>
        /// Maximum memory occupied by whole application.
        /// </summary>
        public long MemoryLimit
        {
            get
            {
                return TryGetValue(nameof(MemoryLimit), 256L * 1024L * 1024L);
            }
            set
            {
                SetValue(nameof(MemoryLimit), value);
            }
        }

        #endregion

        #region HTTP/HTTPS Settings

        /// <summary>
        /// Maximum thread used by single task.
        /// </summary>
        public int ThreadNumber
        {
            get
            {
                if (IsApplicationVersionTrail) return 1;
                return TryGetValue(nameof(ThreadNumber), 1);
            }
            set { SetValue(nameof(ThreadNumber), value); }
        }

        #endregion

        #region YouTube Settings

        /// <summary>
        /// Analyze YouTube URL automatically when creating new task.
        /// </summary>
        public bool IsYouTubeURLEnabled
        {
            get
            {
                if (IsApplicationVersionTrail) return false;
                return TryGetValue(nameof(IsYouTubeURLEnabled), true);
            }
            set { SetValue(nameof(IsYouTubeURLEnabled), value); }
        }

        #endregion

        #region Thunderbolt Settings

        /// <summary>
        /// Analyze Thunder URL automatically when creating new task.
        /// </summary>
        public bool IsThunderURLEnabled
        {
            get
            {
                if (IsApplicationVersionTrail) return false;
                return TryGetValue(nameof(IsThunderURLEnabled), true);
            }
            set { SetValue(nameof(IsThunderURLEnabled), value); }
        }

        #endregion

        #region Torrent Settings

        /// <summary>
        /// Is torrent downloader enabled.
        /// </summary>
        public bool IsTorrentEnabled
        {
            get
            {
                if (IsApplicationVersionTrail) return false;
                return TryGetValue(nameof(IsTorrentEnabled), true);
            }
            set { SetValue(nameof(IsTorrentEnabled), value); }
        }

        /// <summary>
        /// The maximum number of concurrent open connections for each torrent.
        /// </summary>
        public int MaximumConnections
        {
            get => TryGetValue(nameof(MaximumConnections), 60);
            set => SetValue(nameof(MaximumConnections), value);
        }

        /// <summary>
        /// The maximum download speed, in bytes per second, for each torrent. 
        /// A value of 0 means unlimited.
        /// </summary>
        public int MaximumDownloadSpeed
        {
            get => TryGetValue(nameof(MaximumDownloadSpeed), 0);
            set => SetValue(nameof(MaximumDownloadSpeed), value);
        }

        /// <summary>
        /// The maximum upload speed, in bytes per second, for each torrent. 
        /// A value of 0 means unlimited.
        /// </summary>
        public int MaximumUploadSpeed
        {
            get => TryGetValue(nameof(MaximumUploadSpeed), 0);
            set => SetValue(nameof(MaximumUploadSpeed), value);
        }

        #endregion

        #region Background Task

        // Background task will be activated in a minimum
        // freshness time of 15 minutes. 
        // Background task starts every task which has 
        // been permitted to be automatically downloaded 
        // in background once activated.

        /// <summary>
        /// Is background task enabled.
        /// </summary>
        public bool IsBackgroundTaskEnabled
        {
            get
            {
                if (IsApplicationVersionTrail) return false;
                return TryGetValue(nameof(IsBackgroundTaskEnabled), true);
            }
            set { SetValue(nameof(IsBackgroundTaskEnabled), value); }
        }

        /// <summary>
        /// The number of minutes between each running of 
        /// background task. Task will be activated within 
        /// 15 minutes after the BackgroundTaskFreshnessTime 
        /// has passed.
        /// </summary>
        public uint BackgroundTaskFreshnessTime
        {
            get
            {
                return TryGetValue(nameof(BackgroundTaskFreshnessTime), 15u);
            }
            set { SetValue(nameof(BackgroundTaskFreshnessTime), value); }
        }

        /// <summary>
        /// Controls if download task will only be activated
        /// when user is not present.
        /// </summary>
        public bool RunBackgroundTaskOnlyWhenUserNotPresent
        {
            get
            {
                return TryGetValue(nameof(RunBackgroundTaskOnlyWhenUserNotPresent), false);
            }
            set { SetValue(nameof(RunBackgroundTaskOnlyWhenUserNotPresent), value); }
        }

        /// <summary>
        /// Controls if download task will only be activated
        /// when background work cost is not high.
        /// </summary>
        public bool RunOnlyWhenBackgroundWorkCostNotHigh
        {
            get
            {
                return TryGetValue(nameof(RunOnlyWhenBackgroundWorkCostNotHigh), false);
            }
            set { SetValue(nameof(RunOnlyWhenBackgroundWorkCostNotHigh), value); }
        }

        #endregion

        #region Notifications

        /// <summary>
        /// Is notification enabled when any task is completed.
        /// </summary>
        public bool IsNotificationEnabledWhenTaskCompleted
        {
            get { return TryGetValue(nameof(IsNotificationEnabledWhenTaskCompleted), true); }
            set { SetValue(nameof(IsNotificationEnabledWhenTaskCompleted), value); }
        }

        /// <summary>
        /// Is notification enabled when any task is failed.
        /// </summary>
        public bool IsNotificationEnabledWhenFailed
        {
            get { return TryGetValue(nameof(IsNotificationEnabledWhenFailed), true); }
            set { SetValue(nameof(IsNotificationEnabledWhenFailed), value); }
        }

        /// <summary>
        /// Is notification enabled when application is suspended.
        /// </summary>
        public bool IsNotificationEnabledWhenApplicationSuspended
        {
            get { return TryGetValue(nameof(IsNotificationEnabledWhenApplicationSuspended), false); }
            set { SetValue(nameof(IsNotificationEnabledWhenApplicationSuspended), value); }
        }

        #endregion

        #region Tools

        /// <summary>
        /// Try to access the setting item.
        /// Returns default value if failed.
        /// </summary>
        /// <typeparam name="T">type of the setting item</typeparam>
        /// <param name="key">key of the setting item</param>
        /// <param name="defaultValue">default value of the setting item</param>
        private T TryGetValue<T>(string key, T defaultValue)
        {
            object value;
            if (ApplicationData.Current.LocalSettings.Values
                .TryGetValue(key, out value) && value is T)
                return (T)value;
            else return defaultValue;
        }

        /// <summary>
        /// Set value for setting item.
        /// </summary>
        /// <typeparam name="T">type of the setting item</typeparam>
        /// <param name="key">key of the setting item</param>
        /// <param name="value">new value of the setting item</param>
        private void SetValue<T>(string key, T value)
        {
            ApplicationData.Current
                .LocalSettings.Values[key] = value;
        }

        private bool IsApplicationVersionTrail => ((App)App.Current).AppLicense.IsTrial;
        #endregion
    }
}
