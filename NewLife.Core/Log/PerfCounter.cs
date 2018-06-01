using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NewLife.Threading;

namespace NewLife.Log
{
    /// <summary>性能计数类型</summary>
    public enum PerfType
    {
        /// <summary>值的速度</summary>
        ValueSpeed = 1,

        /// <summary>次数的速度</summary>
        TimesSpeed = 2,

        /// <summary>平均计时器，用于计算平均耗时</summary>
        AverageTimer = 3,
    }

    /// <summary>性能计数器。次数、TPS、平均耗时</summary>
    public class PerfCounter : DisposeBase, ICounter
    {
        #region 属性
        /// <summary>是否启用。默认true</summary>
        public Boolean Enable { get; set; } = true;

        /// <summary>性能类型</summary>
        public PerfType Type { get; set; }

        private Int64 _Value;
        /// <summary>数值</summary>
        public Int64 Value => _Value;

        private Int64 _Times;
        /// <summary>次数</summary>
        public Int64 Times => _Times;

        private Int64 _Cost;
        /// <summary>耗时</summary>
        public Int64 Cost => _Cost;
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
        /// <param name="amount">增加的数量</param>
        /// <param name="msCost">耗时</param>
        public void Increment(Int64 amount = 1, Int64 msCost = 0)
        {
            if (!Enable) return;

            // 累加总次数和总数值
            Interlocked.Add(ref _Value, amount);
            Interlocked.Increment(ref _Times);
            if (msCost > 0) Interlocked.Add(ref _Cost, msCost);

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

        /// <summary>最大速度</summary>
        public Int64 Max => _queue.Max();

        /// <summary>平均速度</summary>
        public Int64 Average => (Int64)_queue.Average();

        /// <summary>当前数值</summary>
        public Int64 Current { get; private set; }

        private Int64[] _queue = new Int64[60];
        private Int32 _queueIndex;

        private TimerX _Timer;
        /// <summary>定期采样，保存最近60组到数组队列里面</summary>
        /// <param name="state"></param>
        private void DoWork(Object state)
        {
            // 计算采样次数
            var times = Duration * 1000 / Interval;

            var arr = _queue;
            if (arr == null || arr.Length != times) _queue = arr = new Int64[times];

            var val = 0L;
            switch (Type)
            {
                case PerfType.ValueSpeed:
                    val = GetSpeed(Value);
                    break;
                case PerfType.TimesSpeed:
                    val = GetSpeed(Times);
                    break;
                case PerfType.AverageTimer:
                    val = Value / Times;
                    break;
                default:
                    break;
            }

            Current = val;

            // 进入队列
            var len = arr.Length;
            if (_queueIndex >= len) _queueIndex = 0;
            arr[_queueIndex++] = val;
        }
        #endregion

        #region 辅助
        private Stopwatch _sw;
        private Int64 _Last;
        private Int64 GetSpeed(Int64 val)
        {
            // 计算速度
            var sp = 0L;
            if (_sw == null)
                _sw = Stopwatch.StartNew();
            else
            {
                var ms = _sw.Elapsed.TotalMilliseconds;
                _sw.Restart();

                sp = (Int64)((val - _Last) * 1000 / ms);
            }
            _Last = val;

            return sp;
        }

        /// <summary>已重载。输出统计信息</summary>
        /// <returns></returns>
        public override String ToString()
        {

            return "{0:n0}/{1:n0}/{2:n0}".F(Times, Max, Current);
        }
        #endregion
    }
}