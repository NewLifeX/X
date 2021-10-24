using System;
using NewLife.Data;

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
        /// <param name="data">原始数据</param>
        /// <param name="threshold">阈值，采样数</param>
        /// <returns></returns>
        TimePoint[] Process(TimePoint[] data, Int32 threshold);
    }
}