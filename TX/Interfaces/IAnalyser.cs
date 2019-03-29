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
        /// 检查是否合法
        /// </summary>
        Task<bool> CheckUrlAsync();

        /// <summary>
        /// 获得推荐的文件全名
        /// </summary>
        Task<string> GetRecommendedNameAsync();

        /// <summary>
        /// 获取一个空下载器
        /// </summary>
        Task<IDownloader> GetDownloaderAsync();
    }
}
