using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace TX.StorageTools
{
    class Settings
    {
        /// <summary>
        /// Settings or its properties must not be static if used as Source in x:Bind
        /// https://stackoverflow.com/a/39981618
        /// So we have to implement a singular mode by hand
        /// </summary>
        private static Settings instance;
        public static Settings Instance
        {
            get
            {
                if (instance == null)
                    instance = new Settings();
                return instance;
            }
        }

        /// <summary>
        /// 尝试访问设置项，若未找到则返回默认值
        /// </summary>
        /// <typeparam name="T">设置项类型</typeparam>
        /// <param name="url">设置项的关键字</param>
        /// <param name="defaultValue">返回的默认值</param>
        private T TryGetValue<T>(string key, T defaultValue)
        {
            object value;
            if (ApplicationData.Current.LocalSettings.Values
                .TryGetValue(key, out value) && value is T)
                return (T)value;
            else return defaultValue;
        }

        /// <summary>
        /// 设置值
        /// </summary>
        private void SetValue<T>(string key, T value)
        {
            ApplicationData.Current
                .LocalSettings.Values[key] = value;
        }

        /// <summary>
        /// 帮助页面链接
        /// </summary>
        public const string HelpLink = @"https://milesching.github.io/2019/06/ThunderX_zh_cn.html";

        /// <summary>
        /// 下载文件夹Token
        /// 有关文件夹Token可参考
        /// https://docs.microsoft.com/en-us/uwp/api/Windows.Storage.AccessCache.StorageApplicationPermissions
        /// </summary>
        public string DownloadsFolderToken
        {
            get { return TryGetValue<string>(nameof(DownloadsFolderToken), null); }
            set { SetValue(nameof(DownloadsFolderToken), value); }
        }

        /// <summary>
        /// 设置的线程数
        /// </summary>
        public int ThreadNumber
        {
            get { return TryGetValue(nameof(ThreadNumber), 1); }
            set { SetValue(nameof(ThreadNumber), value); }
        }

        /// <summary>
        /// 是否启动夜间模式
        /// </summary>
        public bool DarkMode
        {
            get { return TryGetValue(nameof(DarkMode), false); }
            set { SetValue(nameof(DarkMode), value); }
        }

        /// <summary>
        /// 发生错误时的最大重试次数
        /// </summary>
        public int MaximumRetries
        {
            get { return TryGetValue(nameof(MaximumRetries), 40); }
            set { SetValue(nameof(MaximumRetries), value); }
        }

        /// <summary>
        /// 下载完成后是否播放通知
        /// </summary>
        public bool IsNotificationShownWhenTaskCompleted
        {
            get { return TryGetValue(nameof(IsNotificationShownWhenTaskCompleted), true); }
            set { SetValue(nameof(IsNotificationShownWhenTaskCompleted), value); }
        }

        /// <summary>
        /// 发生错误后是否播放通知
        /// </summary>
        public bool IsNotificationShownWhenError
        {
            get { return TryGetValue(nameof(IsNotificationShownWhenError), true); }
            set { SetValue(nameof(IsNotificationShownWhenError), value); }
        }

        /// <summary>
        /// 应用被休眠后是否播放通知
        /// </summary>
        public bool IsNotificationShownWhenApplicationSuspended
        {
            get { return TryGetValue(nameof(IsNotificationShownWhenApplicationSuspended), false); }
            set { SetValue(nameof(IsNotificationShownWhenApplicationSuspended), value); }
        }

        /// <summary>
        /// 保存的最大历史记录数在NormalRecordNumberParser中的索引
        /// </summary>
        public int MaximumRecords
        {
            get { return TryGetValue(nameof(MaximumRecords), 200); }
            set { SetValue(nameof(MaximumRecords), value); }
        }

        /// <summary>
        /// 单个线程的动态缓冲区可占用的最大空间（kB)
        /// </summary>
        public int MaximumDynamicBufferSize
        {
            get {
                return TryGetValue(nameof(MaximumDynamicBufferSize), 500);
            }
            set {
                SetValue(nameof(MaximumDynamicBufferSize), value);
            }
        }

        /// <summary>
        /// Analyze YouTube URL automatically when creating new task.
        /// </summary>
        public bool EnableYouTubeURLAnalyzer
        {
            get { return TryGetValue(nameof(EnableYouTubeURLAnalyzer), true); }
            set { SetValue(nameof(EnableYouTubeURLAnalyzer), value); }
        }

        /// <summary>
        /// Analyze Thunder URL automatically when creating new task.
        /// </summary>
        public bool EnableThunderURLAnalyzer
        {
            get { return TryGetValue(nameof(EnableThunderURLAnalyzer), true); }
            set { SetValue(nameof(EnableThunderURLAnalyzer), value); }
        }
    }
}
