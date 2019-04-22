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
                if (ApplicationData.Current.LocalSettings.Values.ContainsKey("DownloadFolderPath"))
                    return (string)ApplicationData.Current.LocalSettings.Values["DownloadFolderPath"];
                else return ApplicationData.Current.LocalFolder.Path;
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
                if (ApplicationData.Current.LocalSettings.Values.ContainsKey("ThreadNumber"))
                    return (int)ApplicationData.Current.LocalSettings.Values["ThreadNumber"];
                else return 1;
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
                if(ApplicationData.Current.LocalSettings.Values.ContainsKey("DarkMode"))
                    return (bool)ApplicationData.Current.LocalSettings.Values["DarkMode"];
                else return false;
            }
            set { ApplicationData.Current.LocalSettings.Values["DarkMode"] = value; }
        }

        /// <summary>
        /// 发生错误时的最大重试次数
        /// </summary>
        public static uint MaximumRetries
        {
            get
            {
                if (ApplicationData.Current.LocalSettings.Values.ContainsKey("MaximumRetries"))
                    return (uint)ApplicationData.Current.LocalSettings.Values["MaximumRetries"];
                else return 0;
            }
            set { ApplicationData.Current.LocalSettings.Values["MaximumRetries"] = value; }
        }
    }
}
