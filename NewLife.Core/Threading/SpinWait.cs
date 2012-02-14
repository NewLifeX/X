using System;
using System.Runtime.InteropServices;
using System.Threading;
using NewLife.Reflection;

namespace NewLife.Threading
{
    /// <summary>提供对基于自旋的等待的支持。</summary>
    class SpinWait
    {
        const int YIELD_THRESHOLD = 10;
        const int SLEEP_0_EVERY_HOW_MANY_TIMES = 5;
        const int SLEEP_1_EVERY_HOW_MANY_TIMES = 20;

        private Int32 _Count;
        /// <summary>获取已对此实例调用 <see cref="SpinOnce" /> 的次数。</summary>
        public Int32 Count { get { return _Count; } }

        private static readonly Int32 ProcessorCount = Environment.ProcessorCount;

        /// <summary>获取对 <see cref="SpinOnce" /> 的下一次调用是否将产生处理器，同时触发强制上下文切换。</summary>
        /// <returns>对 <see cref="SpinOnce" /> 的下一次调用是否将产生处理器，同时触发强制上下文切换。</returns>
        public bool NextSpinWillYield
        {
            get
            {
                if (this._Count <= YIELD_THRESHOLD) return ProcessorCount == 1;
                return true;
            }
        }
        /// <summary>执行单一自旋。</summary>
        public void SpinOnce()
        {
            if (this.NextSpinWillYield)
            {
                int num = (this._Count >= YIELD_THRESHOLD) ? (this._Count - YIELD_THRESHOLD) : this._Count;
                if (num % SLEEP_1_EVERY_HOW_MANY_TIMES == SLEEP_1_EVERY_HOW_MANY_TIMES - 1)
                    Thread.Sleep(1);
                else if (num % SLEEP_0_EVERY_HOW_MANY_TIMES == SLEEP_0_EVERY_HOW_MANY_TIMES - 1)
                    Thread.Sleep(0);
                else
                    SwitchToThread();
            }
            else
                Thread.SpinWait(((int)(SLEEP_0_EVERY_HOW_MANY_TIMES - 1)) << this._Count);
            this._Count = (this._Count == Int32.MaxValue) ? YIELD_THRESHOLD : (this._Count + 1);
        }

        [DllImport("Kernel32.dll")]
        extern static Int32 SwitchToThread();

        /// <summary>重置自旋计数器。</summary>
        public void Reset() { this._Count = 0; }

        /// <summary>在指定条件得到满足之前自旋。</summary>
        /// <param name="condition">在返回 true 之前重复执行的委托。</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="condition" /> 参数为 null。</exception>
        public static void SpinUntil(Func<bool> condition) { SpinUntil(condition, -1); }

        /// <summary>在指定条件得到满足或指定超时过期之前自旋。</summary>
        /// <returns>如果条件在超时时间内得到满足，则为 true；否则为 false</returns>
        /// <param name="condition">在返回 true 之前重复执行的委托。</param>
        /// <param name="timeout">一个 <see cref="T:System.TimeSpan" />，表示等待的毫秒数；或者一个 TimeSpan，表示 -1 毫秒（无限期等待）。</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="condition" /> 参数为 null。</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="timeout" /> 是 -1 毫秒之外的负数，表示无限超时或者超时大于 <see cref="F:System.Int32.MaxValue" />。</exception>
        public static bool SpinUntil(Func<bool> condition, TimeSpan timeout)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > Int32.MaxValue) throw new ArgumentOutOfRangeException("timeout");
            return SpinUntil(condition, (int)timeout.TotalMilliseconds);
        }

        /// <summary>在指定条件得到满足或指定超时过期之前自旋。</summary>
        /// <returns>如果条件在超时时间内得到满足，则为 true；否则为 false</returns>
        /// <param name="condition">在返回 true 之前重复执行的委托。</param>
        /// <param name="millisecondsTimeout">等待的毫秒数，或为 <see cref="F:System.Threading.Timeout.Infinite" /> (-1)，表示无限期等待。</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="condition" /> 参数为 null。</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="millisecondsTimeout" /> 是一个非 -1 的负数，而 -1 表示无限期超时。</exception>
        public static bool SpinUntil(Func<bool> condition, int millisecondsTimeout)
        {
            if (millisecondsTimeout < -1) throw new ArgumentOutOfRangeException("millisecondsTimeout");
            if (condition == null) throw new ArgumentNullException("condition");
            long ticks = 0;
            if (millisecondsTimeout != 0 && millisecondsTimeout != -1) ticks = DateTime.UtcNow.Ticks;
            SpinWait wait = new SpinWait();
            while (!condition())
            {
                if (millisecondsTimeout == 0) return false;
                wait.SpinOnce();
                if (millisecondsTimeout != -1 && wait.NextSpinWillYield && millisecondsTimeout <= (DateTime.UtcNow.Ticks - ticks) / 10000) return false;
            }
            return true;
        }
    }
}