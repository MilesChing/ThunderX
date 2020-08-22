using AngleSharp.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;

namespace TX.NetWork
{
    /// <summary>
    /// Speed calculator is the utility for a downloader to calculate its speed.
    /// The speed and average speed inside a calculater will be periodically calculated
    /// using current value and values before.
    /// </summary>
    public abstract class SpeedCalculator
    {
        /// <summary>
        /// Current value for speed calculating.
        /// </summary>
        public long CurrentValue { get; set; } = 0;

        /// <summary>
        /// Rate of change of the current value.
        /// </summary>
        public double Speed { get; private set; } = 0;

        /// <summary>
        /// Average speed during calculator is enabled.
        /// </summary>
        public double AverageSpeed { get; private set; } = 0;

        /// <summary>
        /// Is the calculator enabled. A disabled calculator won't update speed periodically.
        /// </summary>
        public bool IsEnabled { get; set; } = false;

        /// <summary>
        /// Event for showing speed metrics have been updated
        /// </summary>
        public event Action<SpeedCalculator> Updated;
        protected void SetSpeed(double speed, double averageSpeed)
        {
            Speed = speed;
            AverageSpeed = averageSpeed;
            Updated?.Invoke(this);
        }

        public abstract void Dispose();
    }

    /// <summary>
    /// SharedSpeedCalculatorFactory provides SpeedCalculator instances for speed calculation.
    /// The speed and average speed inside a calculater will be periodically resynced and the
    /// resync will be triggered by the same timer.
    /// </summary>
    public static class SharedSpeedCalculatorFactory
    {
        static SharedSpeedCalculatorFactory()
        {
            // initialize timer
            timer = new Timer()
            {
                Interval = UpdateInterval.TotalMilliseconds,
                AutoReset = true,
            };

            timer.Start();
            timer.Elapsed += Timer_Elapsed;
        }

        public static SpeedCalculator NewSpeedCalculator()
        {
            var cal = new InnerSpeedCalculator();
            calculators.Add(cal);
            return cal;
        }

        private class InnerSpeedCalculator : SpeedCalculator
        {
            public long Time = 0;
            public double LastValue = 0;
            public new void SetSpeed(double speed, double averageSpeed) =>
                base.SetSpeed(speed, averageSpeed);
            public override void Dispose() =>
                calculators.Remove(this);
        }

        private static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            foreach (var calculator in calculators)
                if (calculator.IsEnabled)
                    SyncSpeedCalculator(calculator);
        }

        public static TimeSpan UpdateInterval
            => TimeSpan.FromMilliseconds(500);

        private static Timer timer;
        private static readonly List<InnerSpeedCalculator> calculators =
            new List<InnerSpeedCalculator>();
        private static void SyncSpeedCalculator(InnerSpeedCalculator calculator)
        {
            double aspeed = 0.0;
            if (calculator.Time != 0)
                aspeed = calculator.CurrentValue / calculator.Time /
                    UpdateInterval.TotalSeconds;
            double speed = (calculator.CurrentValue - calculator.LastValue) /
                UpdateInterval.TotalSeconds;
            calculator.LastValue = calculator.CurrentValue;
            calculator.Time++;
            calculator.SetSpeed(speed, aspeed);
        }
    }
}
