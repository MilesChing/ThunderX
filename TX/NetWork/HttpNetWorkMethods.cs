using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TX.Models;

namespace TX.NetWork
{
    /// <summary>
    /// 执行一些联网操作
    /// </summary>
    class HttpNetWorkMethods
    {
        /// <summary>
        /// 从HTTP应答的头部获取文件大小
        /// </summary>
        public static long GetResponseSize(HttpWebResponse response)
        {
            return response.ContentLength;
        }

        /// <summary>
        /// 从某url申请数据流（片段）
        /// </summary>
        /// <param name="url">目标连接</param>
        /// <param name="from">起点偏移</param>
        /// <param name="to">终点偏移</param>
        public static async Task<Stream> GetResponseStreamAsync(string url, long from, long to)
        {
            GC.Collect();
            HttpWebRequest req = WebRequest.CreateHttp(url);
            req.Method = "GET";
            req.Headers["range"] = "bytes=" + from.ToString() + "-" + (to+10).ToString();
            HttpWebResponse resp = (HttpWebResponse)(await req.GetResponseAsync());
            if (req != null)
            {
                req.Abort();
                req = null;
            }

            if (resp == null) return null;
            //返回响应流
            return resp.GetResponseStream();
        }
    }
}
