using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections;

namespace TX.Converters
{
    public static class StringConverter
    {
        /// <summary>
        /// 得到对应字节数的合适单位表示字符串
        /// </summary>
        /// <param name="size">传入的大小</param>
        public static string GetPrintSize(long size)
        {
            long rest = 0;
            if (size < 1024) return size.ToString() + "B";
            else size /= 1024;
            if (size < 1024) return size.ToString() + "KB";
            else
            {
                rest = size % 1024;
                size /= 1024;
            }
            if (size < 1024)
            {
                size = size * 100;
                return (size/100).ToString() + "." + ((rest * 100 / 1024 % 100)).ToString() + "MB";
            }
            else
            {
                size = size * 100 / 1024;
                return ((size / 100)).ToString() + "." + ((size % 100)).ToString() + "GB";
            }
        }

        /// <summary>
        /// 得到对应秒数的合适单位表示时间
        /// </summary>
        /// <param name="sec">输入秒数</param>
        public static string GetPrintTime(long sec)
        {
            long hour = sec / 3600, min = sec % 3600 / 60;
            sec = sec % 60;
            string time = "";
            if (hour > 0)
                time = hour + "h" + min + "m";
            else if (min > 0) time = min + "m" + sec + "s";
            else time = sec + "s";
            return time;
        }

        /// <summary>
        /// 从HTML页面代码解析超链接
        /// </summary>
        /// <param name="html">输入页面</param>
        public static string[] PickURLFromHTML(string html)
        {
            LinkedList<string> list = new LinkedList<string>();
            string pattern = "https{0,1}://[^\\n\\r\\s<>\"]{1,}";
            foreach (Match match in Regex.Matches(html, pattern))
                list.AddLast(match.Value);
            return list.ToArray();
        }
    }
}
