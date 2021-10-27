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
        /// 对齐模式。每个桶X轴对齐方式
        /// </summary>
        AlignModes AlignMode { get; set; }

        /// <summary>
        /// 降采样处理
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <param name="threshold">阈值，采样数</param>
        /// <returns></returns>
        TimePoint[] Process(TimePoint[] data, Int32 threshold);
    }

    public abstract class DownSampling : IDownSampling
    {
        /// <summary>
        /// 对齐模式。每个桶X轴对齐方式
        /// </summary>
        public AlignModes AlignMode { get; set; }

        /// <summary>
        /// 降采样处理
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <param name="threshold">阈值，采样数</param>
        /// <returns></returns>
        public abstract TimePoint[] Process(TimePoint[] data, Int32 threshold);
    }
}