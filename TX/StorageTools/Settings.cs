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
        /// 尝试访问设置项，若未找到则返回默认值
        /// </summary>
        /// <typeparam name="T">设置项类型</typeparam>
        /// <param name="url">设置项的关键字</param>
        /// <param name="defaultValue">返回的默认值</param>
        private static T TryGetValue<T>(string key, T defaultValue)
        {
            object value;
            return ApplicationData.Current
                .LocalSettings.Values
                .TryGetValue(key, out value) ? (T)value : defaultValue;
        }

        /// <summary>
        /// 设置值
        /// </summary>
        private static void SetValue<T>(string key, T value)
        {
            ApplicationData.Current
                .LocalSettings.Values[key] = value;
        }

        /// <summary>
        /// 帮助页面链接
        /// </summary>
        public const string HelpLink = "https://milesching.github.io/thunder-x/2019/06/08/ThunderX_zh_cn.html";

        /// <summary>
        /// 下载文件夹Token
        /// 有关文件夹Token可参考
        /// https://docs.microsoft.com/en-us/uwp/api/Windows.Storage.AccessCache.StorageApplicationPermissions
        /// </summary>
        public static string DownloadsFolderToken
        {
            get { return TryGetValue<string>(nameof(DownloadsFolderToken), null); }
            set { SetValue(nameof(DownloadsFolderToken), value); }
        }

        /// <summary>
        /// 设置的线程数
        /// </summary>
        public static int ThreadNumber
        {
            get { return TryGetValue<int>(nameof(ThreadNumber), 10); }
            set { SetValue(nameof(ThreadNumber), value); }
        }

        /// <summary>
        /// 是否启动夜间模式
        /// </summary>
        public static bool DarkMode
        {
            get { return TryGetValue<bool>(nameof(DarkMode), false); }
            set { SetValue(nameof(DarkMode), value); }
        }

        /// <summary>
        /// 发生错误时的最大重试次数
        /// </summary>
        public static int MaximumRetries
        {
            get { return TryGetValue<int>(nameof(MaximumRetries), 40); }
            set { SetValue(nameof(MaximumRetries), value); }
        }

        /// <summary>
        /// 保存的最大历史记录数在NormalRecordNumberParser中的索引
        /// </summary>
        public static int MaximumRecordsIndex
        {
            get { return TryGetValue<int>(nameof(MaximumRecordsIndex), 1); }
            set { SetValue(nameof(MaximumRecordsIndex), value); }
        }
        //当MaximumRecords记录了k时，真实的上限是NormalRecordNumberParser[k]
        public static readonly int[] NormalRecordNumberParser = new int[4]{ 0, 50, 200, 1000 };
    }
}
