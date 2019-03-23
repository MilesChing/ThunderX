using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TX.Models
{
    public class ThreadMessage
    {
        /// <summary>
        /// 空构造函数
        /// </summary>
        public ThreadMessage()
        {
            ThreadNum = 0;
            ThreadTargetSize = new List<long>();
            ThreadSize = new List<long>();
            ThreadOffset = new List<long>();
        }

        /// <summary>
        /// 线程数
        /// </summary>
        public int ThreadNum;

        /// <summary>
        /// 每个线程需要下载的字节数
        /// </summary>
        public List<long> ThreadTargetSize;

        /// <summary>
        /// 每个线程已下载的字节数
        /// </summary>
        public List<long> ThreadSize;

        /// <summary>
        /// 线程偏移量，即下载开始的首位置
        /// </summary>
        public List<long> ThreadOffset;

        /// <summary>
        /// 安排线程大小
        /// </summary>
        /// <param name="size">大小</param>
        /// <param name="threadNum">线程数</param>
        public void ArrangeThreads(long size, int threadNum)
        {
            ThreadTargetSize.Clear();
            ThreadOffset.Clear();
            ThreadSize.Clear();

            ThreadNum = threadNum;
            long remain = size; //代表剩余字节数
            if (threadNum > 1)
            {
                for (int i = 1; i < threadNum; i++)
                {
                    ThreadTargetSize.Add(size / (threadNum - 1));
                    ThreadOffset.Add(size - remain);
                    remain -= size / (threadNum - 1);
                    ThreadSize.Add(0);
                }
            }
            ThreadTargetSize.Add(remain);
            ThreadOffset.Add(size - remain);
            ThreadSize.Add(0);
        }
    }
}

