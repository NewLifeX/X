using System;
using NewLife.Data;

namespace NewLife.Algorithms
{
    /// <summary>
    /// 插值算法
    /// </summary>
    public interface IInterpolation
    {
        /// <summary>
        /// 插值处理
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="prev">上一个点索引</param>
        /// <param name="next">下一个点索引</param>
        /// <param name="current">当前点时间值</param>
        /// <returns></returns>
        Double Process(TimePoint[] data, Int32 prev, Int32 next, Int64 current);
    }
}