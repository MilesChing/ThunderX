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

        /// <summary>
        /// Token of the download folder.
        /// </summary>
        public string DownloadsFolderToken
        {
            get { return TryGetValue<string>(nameof(DownloadsFolderToken), null); }
            set { SetValue(nameof(DownloadsFolderToken), value); }
        }

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

        /// <summary>
        /// Is dark mode enabled.
        /// </summary>
        public bool IsDarkModeEnabled
        {
            get { return TryGetValue(nameof(IsDarkModeEnabled), false); }
            set { SetValue(nameof(IsDarkModeEnabled), value); }
        }

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

        /// <summary>
        /// Maximum memory occupied by whole application.
        /// </summary>
        public long MemoryLimit
        {
            get {
                return TryGetValue(nameof(MemoryLimit), 256L * 1024L * 1024L);
            }
            set {
                SetValue(nameof(MemoryLimit), value);
            }
        }

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

        private bool IsApplicationVersionTrail => ((App)App.Current).AppLicense.IsTrial;
    }
}
