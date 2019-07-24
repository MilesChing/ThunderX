using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;

namespace TX.NetWork
{
    /// <summary>
    /// 速度计算器，根据传入的数据定时刷新速度信息
    /// </summary>
    class SpeedCalculator : IDisposable
    {
        private long time = 0;
        private Timer timer;
        /// <summary>
        /// 记录最后一次更新的值，当用于从非零数计算速度时设置此值
        /// </summary>
        public long LastValue { set; private get; } = 0;
        private bool isEnabled = false;

        /// <summary>
        /// 速度信息已更新
        /// 调用时并非在原线程
        /// </summary>
        public event Action<SpeedCalculator> Updated;

        /// <summary>
        /// 当前值
        /// </summary>
        public long CurrentValue { get; set; } = 0;

        /// <summary>
        /// 刷新的时间间隔，以毫秒表示，默认100
        /// </summary>
        public double Interval { get; } = 300;

        /// <summary>
        /// 当前速度
        /// </summary>
        public double Speed { get; private set; } = 0;

        /// <summary>
        /// 平均速度
        /// </summary>
        public double AverageSpeed { get; private set; } = 0;

        /// <summary>
        /// 是否刷新速度
        /// </summary>
        public bool IsEnabled {
            get { return isEnabled; }
            set {
                if (value == isEnabled) return;
                isEnabled = value;
                if (value)
                {
                    if(timer == null)
                    {
                        timer = new Timer(Interval);
                        timer.AutoReset = true;
                        timer.Elapsed += Timer_Elapsed;
                    }
                    timer.Start();
                }
                else
                {
                    if (timer == null) return;
                    timer.Stop();
                }
            }
        }
        
        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (time > 1e10) time = 1;
            time++;
            Speed = (CurrentValue - LastValue) / Interval * 1000;
            AverageSpeed = (time == 0 ? 0 : ((AverageSpeed * (time - 1) + Speed) / time));
            LastValue = CurrentValue;
            Updated?.Invoke(this);
        }
        
        public void Dispose()
        {
            if(timer != null)
            {
                timer.Stop();
                timer.Close();
                timer.Dispose();
                timer = null;
            }
        }
    }
}
