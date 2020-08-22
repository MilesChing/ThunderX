using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.NetWork.NetWorkAnalysers;
using TX.StorageTools;
using Windows.ApplicationModel.DataTransfer;

namespace TX.Converters
{
    public static class UrlConverter
    {
        public static bool IsThunderURL(string url)
        {
            return url.ToLower().StartsWith("thunder://");
        }

        public static bool IsHttpURL(string url)
        {
            string p = url.ToLower();
            return p.StartsWith("https://") || p.StartsWith("http://");
        }

        public static bool IsYouTubeURL(string url)
        {
            return IsHttpURL(url) && url.Contains("www.youtube.com/watch?v=");
        }

        /// <summary>
        /// 简易地检测是否合法
        /// </summary>
        public static bool MaybeLegal(string url)
        {
            return IsHttpURL(url) || IsThunderURL(url);
        }

        public static string TranslateURLThunder(string url)
        {
            if (!url.ToLower().StartsWith("thunder://")) return url;
            if (!url.EndsWith('/')) url += '/';
            try
            {
                string t = System.Text.Encoding.ASCII.GetString(Convert.FromBase64String(url.Substring(10, url.Length - 11)));
                return t.Substring(2, t.Length - 4);
            }
            catch (Exception) { return string.Empty; }
        }

        /// <summary>
        /// 返回用于该类型链接的分析器
        /// </summary>
        public static AbstractAnalyser GetAnalyser(string url)
        {
            if (Settings.Instance.EnableYouTubeURLAnalyzer && IsYouTubeURL(url)) return new YouTubeAnalyser();
            else if (Settings.Instance.EnableThunderURLAnalyzer && IsThunderURL(url)) return new ThunderAnalyser();
            else if (IsHttpURL(url)) return new HttpAnalyser();
            else return null;
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
                if(MaybeLegal(str))
                    return str;
            }
            return string.Empty;
        }
    }
}
