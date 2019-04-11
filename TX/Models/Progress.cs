using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TX.Models
{
    public class Progress
    {
        /// <summary>
        /// 当前速度
        /// </summary>
        public double Speed;

        /// <summary>
        /// 平均速度
        /// </summary>
        public double AverageSpeed;

        /// <summary>
        /// 当前进度值
        /// </summary>
        public long CurrentValue;

        /// <summary>
        /// 目标值
        /// </summary>
        public long? TargetValue;
    }
}
