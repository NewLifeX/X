using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NewLife.Threading;

namespace NewLife.Log
{
    /// <summary>性能计数器</summary>
    public class PerfCounter : DisposeBase, ICounter
    {
        #region 属性
        /// <summary>是否启用。默认true</summary>
        public Boolean Enable { get; set; } = true;

        private Int32 _Value;
        /// <summary>数值</summary>
        public Int32 Value => _Value;

        private Int32 _Times;
        /// <summary>次数</summary>
        public Int32 Times => _Times;
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
        /// <param name="amount"></param>
        public void Increment(Int32 amount = 1)
        {
            if (!Enable) return;

            // 累加总次数和总数值
            Interlocked.Add(ref _Value, amount);
            Interlocked.Increment(ref _Times);

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
        public Int32 Max => _queue.Max();

        /// <summary>平均速度</summary>
        public Int32 Average => (Int32)_queue.Average();

        /// <summary>当前速度</summary>
        public Int32 Speed { get; private set; }

        private Int32[] _queue = new Int32[60];
        private Int32 _queueIndex;

        private TimerX _Timer;
        private Stopwatch _sw;
        private Int32 _Last;
        private void DoWork(Object state)
        {
            // 计算采样次数
            var times = Duration * 1000 / Interval;

            var arr = _queue;
            if (arr == null || arr.Length != times) _queue = arr = new Int32[times];

            var val = Value;

            // 计算速度
            var sp = 0;
            if (_sw == null)
                _sw = Stopwatch.StartNew();
            else
            {
                var ms = _sw.Elapsed.TotalMilliseconds;
                _sw.Restart();

                sp = (Int32)((val - _Last) * 1000 / ms);
            }
            _Last = val;
            Speed = sp;

            // 进入队列
            var len = arr.Length;
            if (_queueIndex >= len) _queueIndex = 0;
            arr[_queueIndex++] = sp;
        }
        #endregion

        #region 辅助
        /// <summary>已重载。输出统计信息</summary>
        /// <returns></returns>
        public override String ToString() => "{0:n0}/{1:n0}/{2:n0}".F(Times, Max, Speed);
        #endregion
    }
}