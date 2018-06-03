using System;

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

        /// <summary>平均耗时，单位ms</summary>
        Int64 Cost { get; }

        /// <summary>增加</summary>
        /// <param name="value">增加的数量</param>
        /// <param name="msCost">耗时，单位ms</param>
        void Increment(Int64 value, Int64 msCost);
    }
}