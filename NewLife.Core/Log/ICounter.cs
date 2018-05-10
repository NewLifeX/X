using System;

namespace NewLife.Log
{
    /// <summary>性能计数器接口</summary>
    public interface ICounter
    {
        /// <summary>数值</summary>
        Int64 Value { get; }

        /// <summary>增加</summary>
        /// <param name="amount"></param>
        void Increment(Int64 amount = 1);
    }
}