using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace TX.Strings
{
    public static class AppResources
    {
        private static ResourceLoader CurrentResourceLoader
        {
            get { return _loader ?? (_loader = ResourceLoader.GetForCurrentView("Resources")); }
        }

        private static ResourceLoader _loader;
        private static readonly Dictionary<string, string> ResourceCache = new Dictionary<string, string>();

        /// <summary>
        /// 从资源字典Resources里面取得字符串
        /// </summary>
        public static string GetString(string key)
        {
            string s;
            if (ResourceCache.TryGetValue(key, out s))
            {
                return s;
            }
            else
            {
                s = CurrentResourceLoader.GetString(key);
                ResourceCache[key] = s;
                return s;
            }
        }
    }
}
