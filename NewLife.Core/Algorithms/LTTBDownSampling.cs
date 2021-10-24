using System;
using NewLife.Data;

namespace NewLife.Algorithms
{
    /// <summary>
    /// LTTB最大三角形三桶降采样算法
    /// </summary>
    /// <remarks>
    /// Largest triangle three buckets
    /// LTTB算法由冰岛大学的Sveinn在2013年提出，其对时序数据的降维拟合效果显著，且适用于海量数据处理，已被欧洲多家商业公司采纳
    /// 算法步骤：
    /// 1） 首先确定桶的大小，并将数据点平分到桶中，注意首尾点各占一个桶确保选中
    /// 2） 选中第一个点
    /// 3） 从第二个桶开始，遍历桶中的点，计算每个点的有效区域，并选取有效区域最大的点作为桶的代表点。三角形的选取为[前一个桶的选中点，当前点，后一个桶的平均点]。
    /// 4） 选中最后一个点
    /// </remarks>
    public class LTTBDownSampling : IDownSampling
    {
        /// <summary>
        /// 降采样处理
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <param name="threshold">阈值，采样数</param>
        /// <returns></returns>
        public TimePoint[] Process(TimePoint[] data, Int32 threshold)
        {
            if (data == null || data.Length < 2) return data;
            if (threshold < 2 || threshold >= data.Length) return data;

            var data_length = data.Length;
            var sampled = new TimePoint[threshold];

            // 桶大小，预留开始结束位置
            var step = (Double)(data_length - 2) / (threshold - 2);

            // 第一个点
            sampled[0] = data[0];

            // 三角形选择相邻三个桶的ABC点，A是前一个桶选择点，C是后一个桶平均点，当前桶选择B，使得三角形有效面积最大
            TimePoint pointA = default;
            for (var i = 0; i < threshold - 2; i++)
            {
                // 计算下一个桶的平均点作为C
                TimePoint pointC = default;
                {
                    var start = (Int32)Math.Floor((i + 1) * step) + 1;
                    var end = (Int32)Math.Floor((i + 2) * step) + 1;
                    end = end < data_length ? end : data_length;

                    var length = end - start;
                    for (; start < end; start++)
                    {
                        pointC.Time += data[start].Time;
                        pointC.Value += data[start].Value;
                    }
                    pointC.Time /= length;
                    pointC.Value /= length;
                }

                // 计算每个点的有效区域，并选取有效区域最大的点作为桶的代表点
                TimePoint point = default;
                {
                    // 获取当前桶的范围
                    var start = (Int32)Math.Floor((i + 0) * step) + 1;
                    var end = (Int32)Math.Floor((i + 1) * step) + 1;

                    var max_area = -1.0;
                    for (; start < end; start++)
                    {
                        // 选择一个点B，计算ABC三角形面积
                        var pointB = data[start];
                        var area = Math.Abs(
                            (pointA.Time - pointC.Time) * (pointB.Value - pointA.Value) -
                            (pointA.Time - pointB.Time) * (pointC.Value - pointA.Value)
                            ) / 2;
                        if (area > max_area)
                        {
                            max_area = area;
                            point = pointB;
                        }
                    }
                }

                sampled[i + 1] = point;
                pointA = point;
            }

            // 最后一个点
            sampled[threshold - 1] = data[data_length - 1];

            return sampled;
        }
    }
}