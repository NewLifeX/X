using System;
using System.Threading;
using NewLife.Log;
using NewLife.Threading;

namespace NewLife.Net
{
    /// <summary>统计</summary>
    public class Statistics : IStatistics
    {
        /// <summary>启用统计</summary>
        public Boolean Enable { get; set; } = true;

        /// <summary>统计周期，默认30秒</summary>
        public Int32 Period { get; set; } = 30;

        /// <summary>首次统计时间</summary>
        public DateTime First { get; private set; }

        /// <summary>最后统计时间</summary>
        public DateTime Last { get; private set; }

        private Int32 _Total;
        /// <summary>总数</summary>
        public Int32 Total => _Total;

        private Int32 _Times;
        /// <summary>次数</summary>
        public Int32 Times => _Times;

        /// <summary>最大速度</summary>
        public Int32 Max { get; private set; }

        /// <summary>当前速度</summary>
        public Int32 Speed
        {
            get
            {
                if (_PeriodTimes <= 0 || _Cur <= DateTime.MinValue) return 0;

                var ts = TimerX.Now - _Cur;
                //if (ts.TotalSeconds < 1 || ts.TotalSeconds > Period) return 0;
                // 即使超过周期，也继续计算速度，保持平滑
                if (ts.TotalSeconds < 1) return 0;

                return (Int32)(0.5 + _PeriodTimes / ts.TotalSeconds);
            }
        }

        /// <summary>父级统计</summary>
        public IStatistics Parent { get; set; }

        static Statistics()
        {
            //XTrace.WriteLine("统计信息格式：速度/最高速度/总次数/总数值");
            XTrace.WriteLine("统计信息格式：速度/最高速度/总次数");
        }

        /// <summary>实例化一个统计对象</summary>
        public Statistics() { }

        private DateTime _Cur;
        private DateTime _Next;
        private Int32 _PeriodTimes;

        /// <summary>增加计数</summary>
        /// <param name="n"></param>
        public void Increment(Int32 n = 1)
        {
            var p = Parent;
            if (p != null && p != this) p.Increment(n);
            if (!Enable) return;

            // 累加总次数和总数值
            Interlocked.Add(ref _Total, n);
            Interlocked.Increment(ref _Times);

            // 更新首次和最后一次时间
            var now = TimerX.Now;
            Last = now;
            if (First <= DateTime.MinValue) First = now;

            // 开始新一轮统计
            if (_Next < now)
            {
                if (_Next != DateTime.MinValue) _PeriodTimes = 0;
                _Cur = now;
                _Next = now.AddSeconds(Period);
            }

            // 当前周期累加
            Interlocked.Increment(ref _PeriodTimes);

            // 更新最大速度
            var sp = Speed;
            if (sp > Max) Max = sp;
        }

        /// <summary>已重载。输出统计信息</summary>
        /// <returns></returns>
        public override String ToString()
        {
            //return "{0:n0}/{1:n0}/{2:n0}/{3:n0}".F(Speed, Max, Times, Total);
            return "{0:n0}/{1:n0}/{2:n0}".F(Speed, Max, Times);
        }
    }
}