using System;
using System.Threading;
using NewLife.Log;

namespace NewLife.Net
{
    /// <summary>统计</summary>
    public class Statistics : IStatistics
    {
        /// <summary>是否启用统计</summary>
        public Boolean Enable { get; set; }

        /// <summary>统计周期，单位秒</summary>
        public Int32 Period { get; set; }

        /// <summary>首次统计时间</summary>
        public DateTime First { get; private set; }

        /// <summary>最后统计时间</summary>
        public DateTime Last { get; private set; }

        private Int32 _Total;
        /// <summary>总数</summary>
        public Int32 Total { get { return _Total; } }

        private Int32 _Times;
        /// <summary>次数</summary>
        public Int32 Times { get { return _Times; } }

        /// <summary>周期最大值</summary>
        public Int32 Max { get; private set; }

        /// <summary>周期速度</summary>
        public Int32 Speed
        {
            get
            {
                if (_PeriodTotal <= 0 || _Cur <= DateTime.MinValue) return 0;

                var ts = DateTime.Now - _Cur;
                if (ts.TotalSeconds < 1) return 0;

                return (Int32)(0.5 + _PeriodTotal / ts.TotalSeconds);
            }
        }

        /// <summary>父级统计</summary>
        public IStatistics Parent { get; set; }

        static Statistics()
        {
            XTrace.WriteLine("统计信息格式：每秒速度×周期逝去时间/周期最大值/总次数/总数值");
        }

        /// <summary>实例化一个统计对象</summary>
        public Statistics()
        {
            Period = 10;
        }

        private DateTime _Cur;
        private DateTime _Next;
        private Int32 _PeriodTotal;

        /// <summary>增加计数</summary>
        /// <param name="n"></param>
        public void Increment(Int32 n = 1)
        {
            if (Parent != null && Parent != this) Parent.Increment(n);
            //if (!Enable) return;

            Interlocked.Add(ref _Total, n);
            Interlocked.Increment(ref _Times);

            var now = DateTime.Now;
            Last = now;
            if (First <= DateTime.MinValue) First = now;

            Interlocked.Add(ref _PeriodTotal, n);

            if (_Next < now)
            {
                if (_Next != DateTime.MinValue) _PeriodTotal = 0;
                _Cur = now;
                _Next = now.AddSeconds(Period);
            }

            if (_PeriodTotal > Max) Max = _PeriodTotal;
        }

        /// <summary>已重载。输出统计信息</summary>
        /// <returns></returns>
        public override string ToString()
        {
            var s = 0;
            if (_Cur > DateTime.MinValue)
            {
                var ts = DateTime.Now - _Cur;
                if (ts.TotalSeconds >= 1) s = (Int32)ts.TotalSeconds;
            }
            return "{0:n0}×{4}/{1:n0}/{2:n0}/{3:n0}".F(Speed, Max, Times, Total, s);
        }
    }
}