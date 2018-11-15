using System;
using System.Diagnostics;
using NewLife.Reflection;

namespace NewLife.Log
{
    /// <summary>性能计数器接口</summary>
    public interface ICounter
    {
        /// <summary>数值</summary>
        Int64 Value { get; }

        /// <summary>次数</summary>
        Int64 Times { get; }

        /// <summary>速度</summary>
        Int64 Speed { get; }

        /// <summary>平均耗时，单位us</summary>
        Int64 Cost { get; }

        /// <summary>增加</summary>
        /// <param name="value">增加的数量</param>
        /// <param name="usCost">耗时，单位us</param>
        void Increment(Int64 value, Int64 usCost);
    }

    /// <summary>计数器助手</summary>
    public static class CounterHelper
    {
        private static readonly Double tickFrequency;
        static CounterHelper()
        {
            var type = typeof(Stopwatch);
            var fi = type.GetFieldEx("tickFrequency") ?? type.GetFieldEx("s_tickFrequency");
            if (fi != null) tickFrequency = (Double)fi.GetValue(null);
        }

        /// <summary>开始计时</summary>
        /// <param name="counter"></param>
        /// <returns></returns>
        public static Int64 StartCount(this ICounter counter) => counter == null ? 0 : Stopwatch.GetTimestamp();

        /// <summary>结束计时</summary>
        /// <param name="counter"></param>
        /// <param name="startTicks"></param>
        public static void StopCount(this ICounter counter, Int64? startTicks)
        {
            if (counter == null || startTicks == null) return;

            var ticks = Stopwatch.GetTimestamp() - startTicks.Value;
            counter.Increment(1, (Int64)(ticks * tickFrequency / 10));
        }
    }
}