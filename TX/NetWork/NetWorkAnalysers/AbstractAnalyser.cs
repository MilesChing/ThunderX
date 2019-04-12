using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Downloaders;

namespace TX.NetWork.NetWorkAnalysers
{
    public abstract class AbstractAnalyser : IDisposable
    {
        public string URL { get; protected set; }

        /// <summary>
        /// 准备URL
        /// </summary>
        public abstract Task SetURLAsync(string url);

        /// <summary>
        /// 检查是否合法
        /// </summary>
        public abstract bool IsLegal();

        /// <summary>
        /// 获得推荐的文件全名
        /// </summary>
        public abstract string GetRecommendedName();

        /// <summary>
        /// 获取一个空下载器
        /// </summary>
        public abstract AbstractDownloader GetDownloader();

        /// <summary>
        /// 获取流长度，可能为-1
        /// </summary>
        public abstract long GetStreamSize();

        /// <summary>
        /// 获取URL对应的界面细节
        /// </summary>
        public abstract NewTaskPageVisualDetail GetVisualDetail();

        /// <summary>
        /// 释放资源
        /// </summary>
        public abstract void Dispose();
    }
}
