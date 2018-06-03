using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NewLife.Threading;

namespace NewLife.Log
{
    /// <summary>性能计数器。次数、TPS、平均耗时</summary>
    public class PerfCounter : DisposeBase, ICounter
    {
        #region 属性
        /// <summary>是否启用。默认true</summary>
        public Boolean Enable { get; set; } = true;

        private Int64 _Value;
        /// <summary>数值</summary>
        public Int64 Value => _Value;

        private Int64 _Times;
        /// <summary>次数</summary>
        public Int64 Times => _Times;

        /// <summary>耗时，单位us</summary>
        private Int64 _TotalCost;
        #endregion

        #region 构造
        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            _Timer.TryDispose();
        }
        #endregion

        #region 核心方法
        /// <summary>增加</summary>
        /// <param name="value">增加的数量</param>
        /// <param name="usCost">耗时，单位us</param>
        public void Increment(Int64 value, Int64 usCost)
        {
            if (!Enable) return;

            // 累加总次数和总数值
            Interlocked.Add(ref _Value, value);
            Interlocked.Increment(ref _Times);
            if (usCost > 0) Interlocked.Add(ref _TotalCost, usCost);

            if (_Timer == null)
            {
                lock (this)
                {
                    if (_Timer == null) _Timer = new TimerX(DoWork, null, Interval, Interval);
                }
            }
        }
        #endregion

        #region 采样
        /// <summary>采样间隔，默认1000毫秒</summary>
        public Int32 Interval { get; set; } = 1000;

        /// <summary>持续采样时间，默认60秒</summary>
        public Int32 Duration { get; set; } = 60;

        /// <summary>当前速度</summary>
        public Int64 Speed { get; private set; }

        /// <summary>最大速度</summary>
        public Int64 MaxSpeed => _quSpeed.Max();

        /// <summary>最后一个采样周期的平均耗时，单位us</summary>
        public Int64 Cost { get; private set; }

        /// <summary>持续采样时间内的最大平均耗时，单位us</summary>
        public Int64 MaxCost => _quCost.Max();

        private Int64[] _quSpeed = new Int64[60];
        private Int64[] _quCost = new Int64[60];
        private Int32 _queueIndex = -1;

        private TimerX _Timer;
        private Stopwatch _sw;
        private Int64 _LastValue;
        private Int64 _LastTimes;
        private Int64 _LastCost;

        /// <summary>定期采样，保存最近60组到数组队列里面</summary>
        /// <param name="state"></param>
        private void DoWork(Object state)
        {
            // 计算采样次数
            var len = Duration * 1000 / Interval;

            if (_quSpeed.Length != len) _quSpeed = new Int64[len];
            if (_quCost.Length != len) _quCost = new Int64[len];

            // 计算速度
            var sp = 0L;
            var cc = 0L;
            if (_sw == null)
                _sw = Stopwatch.StartNew();
            else
            {
                var ms = _sw.Elapsed.TotalMilliseconds;
                _sw.Restart();

                sp = (Int64)((Value - _LastValue) * 1000 / ms);
            }

            _LastValue = Value;

            // 计算本周期平均耗时
            // 本周期内的总耗时除以总次数，得到本周期平均耗时
            var ts = Times - _LastTimes;
            cc = ts == 0 ? Cost : ((_TotalCost - _LastCost) / ts);

            _LastTimes = Times;
            _LastCost = _TotalCost;

            Speed = sp;
            Cost = cc;

            // 进入队列
            _queueIndex++;
            if (_queueIndex < 0 || _queueIndex >= len) _queueIndex = 0;
            _quSpeed[_queueIndex] = sp;
            _quCost[_queueIndex] = cc;
        }
        #endregion

        #region 辅助
        /// <summary>已重载。输出统计信息</summary>
        /// <returns></returns>
        public override String ToString()
        {
            if (Cost >= 1000)
                return "{0:n0}/{1:n0}/{2:n0}tps/{3:n0}/{4:n0}ms".F(Times, MaxSpeed, Speed, MaxCost / 1000, Cost / 1000);
            if (Cost > 0)
                return "{0:n0}/{1:n0}/{2:n0}tps/{3:n0}/{4:n0}us".F(Times, MaxSpeed, Speed, MaxCost, Cost);
            else
                return "{0:n0}/{1:n0}/{2:n0}tps".F(Times, MaxSpeed, Speed);
        }
        #endregion
    }
}