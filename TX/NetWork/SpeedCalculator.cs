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
        private double interval = 250;
        private ulong time = 0;
        private Timer timer;
        private double currentValue = 0;
        private double lastValue = 0;
        private double currentSpeed = 0;
        private double averageSpeed = 0;
        private bool isEnabled = false;

        /// <summary>
        /// 速度信息已更新
        /// 调用时并非在原线程
        /// </summary>
        public event Action<SpeedCalculator> Updated;

        /// <summary>
        /// 当前值
        /// </summary>
        public double CurrentValue
        {
            get { return currentValue; }
            set { currentValue = value; }
        }

        /// <summary>
        /// 刷新的时间间隔，以毫秒表示，默认250
        /// </summary>
        public double Interval
        {
            get { return interval; }
        }

        /// <summary>
        /// 当前速度
        /// </summary>
        public double Speed
        {
            get { return currentSpeed; }
        }

        /// <summary>
        /// 平均速度
        /// </summary>
        public double AverageSpeed
        {
            get { return averageSpeed; }
        }

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
                        timer = new Timer(interval);
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

        /// <summary>
        /// 时间间隔到了
        /// </summary>
        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            time++;
            currentSpeed = (currentValue - lastValue) / interval * 1000;
            averageSpeed = (time == 0 ? 0 : ((averageSpeed * (time - 1) + currentSpeed) / time));
            lastValue = currentValue;
            Updated(this);
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
