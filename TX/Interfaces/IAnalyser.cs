using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Downloaders;

namespace TX.NetWork.NetWorkAnalysers
{
    public interface IAnalyser : IDisposable
    {
        string GetUrl();

        /// <summary>
        /// 发出请求并获取回复，必须先调用这个方法
        /// </summary>
        Task GetResponseAsync();

        /// <summary>
        /// 检查是否合法
        /// </summary>
        bool CheckUrl();

        /// <summary>
        /// 获得推荐的文件全名
        /// </summary>
        string GetRecommendedName();

        /// <summary>
        /// 获取一个空下载器
        /// </summary>
        IDownloader GetDownloader();

        /// <summary>
        /// 获取流长度，可能为-1
        /// </summary>
        long GetStreamSize();

        /// <summary>
        /// 获取URL对应的界面细节
        /// </summary>
        NewTaskPageVisualDetail GetVisualDetail();
    }
}
