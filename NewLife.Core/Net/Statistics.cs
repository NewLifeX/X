using System;
using System.Threading;

namespace NewLife.Net
{
    /// <summary>统计</summary>
    class Statistics : IStatistics
    {
        /// <summary>是否启用统计</summary>
        public Boolean Enable { get; set; }

        /// <summary>首次统计时间</summary>
        public DateTime First { get; private set; }

        /// <summary>最后统计时间</summary>
        public DateTime Last { get; private set; }

        private Int32 _Total;
        /// <summary>每分钟最大值</summary>
        public Int32 Total { get { return _Total; } }

        private Int32 _TotalPerMinute;
        /// <summary>每分钟总操作</summary>
        public Int32 TotalPerMinute { get { return _TotalPerMinute; } }

        private Int32 _TotalPerHour;
        /// <summary>每小时总操作</summary>
        public Int32 TotalPerHour { get { return _TotalPerHour; } }

        /// <summary>每分钟最大值</summary>
        public Int32 MaxPerMinute { get; private set; }

        /// <summary>父级统计</summary>
        public IStatistics Parent { get; set; }

        /// <summary>每秒平均</summary>
        public Int32 AveragePerSecond
        {
            get
            {
                if (Total <= 0) return 0;
                TimeSpan ts = Last - First;
                return ts.TotalSeconds > 0 ? (Int32)(0.5 + Total / ts.TotalSeconds) : 0;
            }
        }

        /// <summary>每分钟平均</summary>
        public Int32 AveragePerMinute
        {
            get
            {
                if (Total <= 0) return 0;
                TimeSpan ts = Last - First;
                return ts.TotalMinutes > 0 ? (Int32)(0.5 + Total / ts.TotalMinutes) : 0;
            }
        }

        private DateTime _NextPerMinute;
        private DateTime _NextPerHour;

        /// <summary>增加计数</summary>
        /// <param name="n"></param>
        public void Increment(Int32 n = 1)
        {
            if (Parent != null && Parent != this) Parent.Increment(n);
            //if (!Enable) return;

            Interlocked.Add(ref _Total, n);

            var now = DateTime.Now;
            Last = now;
            if (_Total <= 100 && First <= DateTime.MinValue) First = now;

            Interlocked.Add(ref _TotalPerMinute, n);
            Interlocked.Add(ref _TotalPerHour, n);

            if (_NextPerMinute < now)
            {
                if (_NextPerMinute != DateTime.MinValue) _TotalPerMinute = 0;
                _NextPerMinute = now.AddMinutes(1);
            }

            if (_NextPerHour < now)
            {
                if (_NextPerHour != DateTime.MinValue) _TotalPerHour = 0;
                _NextPerHour = now.AddHours(1);
            }

            if (_TotalPerMinute > MaxPerMinute) MaxPerMinute = _TotalPerMinute;
        }

        /// <summary>已重载。输出统计信息</summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "{0:n0}/{1:n0}/{2:n0}".F(AveragePerMinute, MaxPerMinute, TotalPerMinute);
        }
    }
}