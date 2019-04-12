using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TX.NetWork.URLAnalysers
{
    class ThunderURLAnalyser : AbstractURLAnalyser
    {
        protected override string Convert(string url)
        {
            if (!url.ToLower().StartsWith("thunder://"))
            {
                Message = null;
                return url;
            }
            if (!url.EndsWith('/')) url += '/';
            try
            {
                string t = Encoding.ASCII.GetString(System.Convert.FromBase64String(url.Substring(10, url.Length - 11)));
                Message = "检测到Thunder链接：完成解析";
                return t.Substring(2, t.Length - 4);
            }
            catch (Exception) {
                Message = "检测到Thunder链接：解析出错";
                return url;
            }
        }
    }
}
