using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Threading;

namespace NewLife.Net.Common
{
    /// <summary>统计</summary>
    class Statistics : IStatistics
    {
        private Boolean _Enable;
        /// <summary>是否启用统计</summary>
        public Boolean Enable { get { return _Enable; } set { _Enable = value; } }

        private DateTime _First;
        /// <summary>首次统计时间</summary>
        public DateTime First { get { return _First; } set { _First = value; } }

        private DateTime _Last;
        /// <summary>最后统计时间</summary>
        public DateTime Last { get { return _Last; } }

        private Int32 _Total;
        /// <summary>每分钟最大值</summary>
        public Int32 Total { get { return _Total; } set { _Total = value; } }

        private Int32 _TotalPerMinute;
        /// <summary>每分钟总操作</summary>
        public Int32 TotalPerMinute { get { return _TotalPerMinute; } }

        private Int32 _TotalPerHour;
        /// <summary>每小时总操作</summary>
        public Int32 TotalPerHour { get { return _TotalPerHour; } }

        private Int32 _MaxPerMinute;
        /// <summary>每分钟最大值</summary>
        public Int32 MaxPerMinute { get { return _MaxPerMinute; } set { _MaxPerMinute = value; } }

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
        public void Increment()
        {
            _Total++;

            DateTime now = DateTime.Now;
            _Last = now;
            if (_Total <= 1 && _First <= DateTime.MinValue) _First = now;

            if (!Enable) return;

            _TotalPerMinute++;
            _TotalPerHour++;

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

            if (_TotalPerMinute > _MaxPerMinute) _MaxPerMinute = _TotalPerMinute;
        }
    }
}