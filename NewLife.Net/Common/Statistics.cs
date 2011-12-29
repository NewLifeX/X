using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Threading;

namespace NewLife.Net.Common
{
    /// <summary>统计</summary>
    class Statistics : DisposeBase, IStatistics
    {
        private Boolean _Enable;
        /// <summary>是否启用统计计数器</summary>
        public Boolean Enable
        {
            get { return _Enable; }
            set
            {
                _Enable = value;
                if (value && computeTimer != null)
                {
                    computeTimer.Dispose();
                    computeTimer = null;
                }
            }
        }

        private Int32 _Period = NetHelper.Debug ? 10000 : 20000;
        /// <summary>定时器统计周期。单位毫秒，默认20000ms（Debug时10000ms），小于0时关闭定时器，采用实时计算。越小越准确，但是性能损耗也很大。</summary>
        public Int32 Period
        {
            get { return computeTimer != null ? computeTimer.Period : _Period; }
            set
            {
                _Period = value;
                // 实时更新定时器的时间间隔
                if (computeTimer != null)
                {
                    if (value > 1000)
                        computeTimer.Period = value;
                    else if (value < 0)
                    {
                        computeTimer.Dispose();
                        computeTimer = null;
                    }
                }
            }
        }

        private Int32 _TotalPerMinute;
        /// <summary>每分钟总操作</summary>
        public Int32 TotalPerMinute { get { return _TotalPerMinute; } }

        private Int32 _TotalPerHour;
        /// <summary>每小时总操作</summary>
        public Int32 TotalPerHour { get { return _TotalPerHour; } }

        private Int32 _MaxPerMinute;
        /// <summary>每分钟最大值</summary>
        public Int32 MaxPerMinute { get { return _MaxPerMinute; } set { _MaxPerMinute = value; } }

        private DateTime _NextPerMinute;
        private DateTime _NextPerHour;

        /// <summary>增加计数</summary>
        public void Increment()
        {
            if (!Enable) return;

            if (Period > 0 && computeTimer == null) computeTimer = new TimerX(Compute, null, 0, Period, false);

            _TotalPerMinute++;
            _TotalPerHour++;

            if (Period < 0) Compute(null);
        }

        /// <summary>统计操作计时器</summary>
        private TimerX computeTimer;

        /// <summary>统计操作</summary>
        void Compute(Object state)
        {
            DateTime now = DateTime.Now;

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

        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            if (computeTimer != null)
            {
                computeTimer.Dispose();
                computeTimer = null;
            }
        }
    }
}