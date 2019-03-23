using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace TX.NetWork
{
    public class UrlConverter
    {
        /// <summary>
        /// 判断给出的url是否基于http协议
        /// </summary>
        /// <param name="url">目标链接</param>
        /// <returns></returns>
        public static bool IsHttpUrl(string url)
        {
            string p = url.ToLower();
            return p.StartsWith("https://") || p.StartsWith("http://");
        }

        /// <summary>
        /// 判断url是否合法
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static bool IsLegal(string url)
        {
            return IsHttpUrl(url);
        }

        /// <summary>
        /// 根据给出的url类型返回下载器
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static IDownloader GetDownloader(string url)
        {
            if (IsHttpUrl(url)) return new Downloaders.HttpDownloader();

            return null;
        }

        /// <summary>
        /// 转换迅雷链接（不是就返回自身）
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string TranslateURLThunder(string url)
        {
            if (!url.ToLower().StartsWith("thunder://")) return url;
            if (!url.EndsWith('/')) url += '/';
            try
            {
                string t = System.Text.Encoding.ASCII.GetString(Convert.FromBase64String(url.Substring(10, url.Length - 11)));
                return t.Substring(2, t.Length - 4);
            }
            catch (Exception) { return null; }
        }

        /// <summary>
        /// 打开剪贴板查看是否包含合法url
        /// </summary>
        /// https://blog.csdn.net/lindexi_gd/article/details/50479180
        /// <returns>没有返回string.Empty</returns>
        public static async Task<string> CheckClipBoardAsync()
        {
            DataPackageView con = Clipboard.GetContent();
            string str = string.Empty;
            if (con.Contains(StandardDataFormats.Text))
            {
                str = await con.GetTextAsync();
                if(IsLegal(str))
                    return str;
            }
            return string.Empty;
        }
    }
}
