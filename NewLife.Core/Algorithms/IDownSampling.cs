using System;

namespace NewLife.Algorithms
{
    /// <summary>
    /// 降采样算法
    /// </summary>
    public interface IDownSampling
    {
        /// <summary>
        /// 降采样处理
        /// </summary>
        /// <param name="times"></param>
        /// <param name="values"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        SamplingData Process(Int32[] times, Double[] values, Int32 threshold);
    }
}