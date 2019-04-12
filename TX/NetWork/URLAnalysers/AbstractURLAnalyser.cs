using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TX.NetWork.URLAnalysers
{
    public abstract class AbstractURLAnalyser
    {
        /// <summary>
        /// 原始URL
        /// </summary>
        public string OriginalURL { get { return url; } set { url = value; TransferedURL = Convert(value); } }
        private string url = null;

        /// <summary>
        /// 经过转换的URL，当无转换产生时，和原始URL相同
        /// </summary>
        public string TransferedURL { get; protected set; }

        /// <summary>
        /// 转换信息
        /// </summary>
        public string Message { get; protected set; }
        
        /// <summary>
        /// 对给定的URL进行转换并将
        /// 描述性信息输出至Message以展示给用户
        /// </summary>
        protected abstract string Convert(string url);
    }
}
