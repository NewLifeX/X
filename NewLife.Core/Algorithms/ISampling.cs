using System;
using System.Collections.Generic;
using NewLife.Data;

namespace NewLife.Algorithms
{
    /// <summary>
    /// 采样接口
    /// </summary>
    public interface ISampling
    {
        /// <summary>
        /// 插值处理
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <param name="size">桶大小。如60/3600/86400</param>
        /// <param name="offset">偏移量。时间不是对齐零点时使用</param>
        /// <returns></returns>
        TimePoint[] Process(TimePoint[] data, Int32 size, Int32 offset = 0);
    }

    /// <summary>
    /// 采样助手
    /// </summary>
    public static class SamplingHelper
    {
        /// <summary>
        /// 按照固定时间间隔，拆分数据轴为多个桶
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <param name="size">桶大小。如60/3600/86400</param>
        /// <param name="offset">偏移量。时间不是对齐零点时使用</param>
        /// <returns></returns>
        public static Int64[] Split(Int64[] data, Int32 size, Int32 offset = 0)
        {
            if (data == null || data.Length == 0) return data;
            if (size <= 0) throw new ArgumentNullException(nameof(size));
            if (offset >= size) throw new ArgumentOutOfRangeException(nameof(offset));

            // 计算首尾的两个桶的值
            var start = data[0] / size * size + offset;
            if (start > data[0]) start -= size;
            var last = data[data.Length - 1];
            var end = last / size * size + offset;
            if (end > last) end -= size;

            var list = new List<Int64>();

            // 计算每个桶的头尾
            var idx = 0;
            for (var time = start; time <= end;)
            {
                var v = -1;
                var next = time + size;

                // 顺序遍历原始数据，这里假设原始数据为升序
                for (; idx < data.Length; idx++)
                {
                    // 如果超过了当前桶的结尾，则换下一个桶
                    if (data[idx] >= next) break;

                    if (v < 0 && time <= data[idx]) v = idx;
                }

                list.Add(v);
                time = next;
            }

            return list.ToArray();
        }
    }
}