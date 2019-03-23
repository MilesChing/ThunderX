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
        /// 访问设置项
        /// </summary>
        /// <typeparam name="T">设置项类型</typeparam>
        /// <param name="url">设置项的关键字</param>
        /// <returns></returns>
        private static T GetValue<T>(string url)
        {
            return (T)Windows.Storage.ApplicationData.Current.LocalSettings.Values[url];
        }

        /// <summary>
        /// 下载文件夹路径
        /// </summary>
        public static string DownloadFolderPath
        {
            get
            {
                string t = (string)Windows.Storage.ApplicationData.Current.LocalSettings.Values["DownloadFolderPath"];
                if (t == null) return ApplicationData.Current.LocalCacheFolder.Path;
                else return t;
            }
            set
            {
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["DownloadFolderPath"] = value;
            }
        }

        /// <summary>
        /// 设置的线程数，默认值10
        /// </summary>
        public static int ThreadNumber
        {
            get
            {
                try { return (int)ApplicationData.Current.LocalSettings.Values["ThreadNumber"]; }
                //如果取不到就用默认值
                catch (NullReferenceException) { return 2; }
            }
            set { ApplicationData.Current.LocalSettings.Values["ThreadNumber"] = value; }
        }

        /// <summary>
        /// 是否启动夜间模式
        /// </summary>
        public static bool DarkMode
        {
            get
            {
                try { return (bool)ApplicationData.Current.LocalSettings.Values["DarkMode"]; }
                //如果取不到就用默认值
                catch (NullReferenceException) { return false; }
            }
            set { ApplicationData.Current.LocalSettings.Values["DarkMode"] = value; }
        }
    }
}
