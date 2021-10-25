using System;
using NewLife.Data;

namespace NewLife.Algorithms
{
    /// <summary>
    /// 平均值采样算法
    /// </summary>
    public class AverageDownSampling : IDownSampling
    {
        /// <summary>
        /// 对齐模式。每个桶X轴对齐方式
        /// </summary>
        public AlignModes AlignMode { get; set; } = AlignModes.Left;

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

            if (AlignMode == AlignModes.None) AlignMode = AlignModes.Left;

            var data_length = data.Length;
            var sampled = new TimePoint[threshold];

            // 桶大小，预留开始结束位置
            var step = (Double)data_length / threshold;

            // 每个桶选择一个点作为代表
            for (var i = 0; i < threshold; i++)
            {
                // 获取当前桶的范围
                var start = (Int32)Math.Round((i + 0) * step);
                var end = (Int32)Math.Round((i + 1) * step);
                end = end < data_length ? end : data_length;

                TimePoint point = default;
                var vs = 0.0;
                for (var j = start; j < end; j++)
                {
                    vs += data[j].Value;
                }
                point.Value = vs / (end - start);

                // 对齐
                switch (AlignMode)
                {
                    case AlignModes.Left:
                    default:
                        point.Time = data[start].Time;
                        break;
                    case AlignModes.Right:
                        point.Time = data[end - 1].Time;
                        break;
                    case AlignModes.Center:
                        point.Time = data[(Int32)Math.Round((start + end) / 2.0)].Time;
                        break;
                }

                sampled[i] = point;
            }

            return sampled;
        }
    }
}