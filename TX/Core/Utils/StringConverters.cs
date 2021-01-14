
using System.Collections;
using System.Linq;

namespace TX.Core.Utils
{
    static class StringConverters
    {
        public static string SizedString(this long size)
        {
            if (size < 1024) return size.ToString() + " B";
            else size /= 1024;
            long rest;
            if (size < 1024) return size.ToString() + " KB";
            else
            {
                rest = size % 1024;
                size /= 1024;
            }
            if (size < 1024)
            {
                size *= 100;
                return (size / 100).ToString() + "." + ((rest * 100 / 1024 % 100)).ToString() + " MB";
            }
            else
            {
                size = size * 100 / 1024;
                return ((size / 100)).ToString() + "." + ((size % 100)).ToString() + " GB";
            }
        }
    }
}
