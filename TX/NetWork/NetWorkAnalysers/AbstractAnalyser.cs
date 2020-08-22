using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Downloaders;
using TX.VisualManager;

namespace TX.NetWork.NetWorkAnalysers
{
    public abstract class AbstractAnalyser : IDisposable
    {
        /// <summary>
        /// 视觉控制器，用于与界面进行交互
        /// </summary>
        protected NewTaskPage Visual { get; private set; }

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
        /// 释放资源，解除对控制器做的更改
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// 绑定界面控制器，用于与页面进行信息交换
        /// </summary>
        public void BindVisualController(NewTaskPage visual)
        {
            Visual = visual;
        }
    }
}
