using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.NetWork.NetWorkAnalysers;
using Windows.ApplicationModel.DataTransfer;

namespace TX.NetWork
{
    public class UrlConverter
    {
        /// <summary>
        /// 判断给出的url是否基于http协议
        /// </summary>
        public static bool IsHttpUrl(string url)
        {
            string p = url.ToLower();
            return p.StartsWith("https://") || p.StartsWith("http://");
        }

        /// <summary>
        /// 简易地检测是否合法
        /// </summary>
        public static bool MaybeLegal(string url)
        {
            return IsHttpUrl(url);
        }

        /// <summary>
        /// 转换迅雷链接（不是就返回自身）
        /// </summary>
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
        /// 返回用于该类型链接的分析器
        /// </summary>
        public static IAnalyser GetAnalyser(string url)
        {
            url = TranslateURLThunder(url);
            if (IsHttpUrl(url)) return new HttpAnalyser(url);
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
