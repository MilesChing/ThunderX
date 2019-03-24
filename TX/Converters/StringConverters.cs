using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TX.Converters
{
    public static class StringConverters
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
    }
}
