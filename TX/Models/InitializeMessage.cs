using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TX.Models
{
    /// <summary>
    /// 结构体，用于存储初始化新任务的信息
    /// </summary>
    public class InitializeMessage
    {
        public InitializeMessage(string url, string fileName, int threads, long size, string path)
        {
            Url = url ?? throw new Exception("InitializeMessage_URL_NULL");
            FileName = fileName;
            Threads = threads;
            Size = size;
            FilePath = path;
        }
        /// <summary>
        /// 任务链接
        /// </summary>
        public string Url;

        /// <summary>
        /// 重命名为
        /// </summary>
        public string FileName;

        /// <summary>
        /// 需要使用的线程数
        /// </summary>
        public int Threads;

        /// <summary>
        /// 文件大小，获取不到则可能为-1
        /// </summary>
        public long Size;

        /// <summary>
        /// 目标文件的地址
        /// </summary>
        public string FilePath;
    }
}
