using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TX.Enums
{
    public enum DownloadState
    {
        /// <summary>
        /// 正在下载中
        /// </summary>
        Downloading,

        /// <summary>
        /// 发生错误
        /// </summary>
        Error,

        /// <summary>
        /// 等待中
        /// </summary>
        Pending,

        /// <summary>
        /// 已完成
        /// </summary>
        Done,

        /// <summary>
        /// 已经准备好开始
        /// </summary>
        Prepared,

        /// <summary>
        /// 暂停
        /// </summary>
        Pause
    }
}
