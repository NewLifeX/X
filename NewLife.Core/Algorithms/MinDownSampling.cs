using System;
using NewLife.Data;

namespace NewLife.Algorithms
{
    /// <summary>
    /// 最小值采样算法
    /// </summary>
    public class MinDownSampling : IDownSampling
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
        public TimePoint[] Process(TimePoint[] data, Int32 threshold)
        {
            if (data == null || data.Length < 2) return data;
            if (threshold < 2 || threshold >= data.Length) return data;

            if (AlignMode != AlignModes.None) return ProcessByAlign(data, threshold);

            var data_length = data.Length;
            var sampled = new TimePoint[threshold];

            // 桶大小，预留开始结束位置
            var step = (Double)(data_length - 2) / (threshold - 2);

            // 每个桶选择一个点作为代表
            for (var i = 0; i < threshold - 2; i++)
            {
                // 获取当前桶的范围
                var start = (Int32)Math.Round((i + 0) * step) + 1;
                var end = (Int32)Math.Round((i + 1) * step) + 1;
                end = end < data_length ? end : data_length;

                TimePoint point = default;
                var min_value = Double.MaxValue;
                for (var j = start; j < end; j++)
                {
                    if (data[j].Value < min_value)
                    {
                        min_value = data[j].Value;
                        point = data[j];
                    }
                }

                sampled[i + 1] = point;
            }

            // 第一个点和最后一个点
            sampled[0] = data[0];
            sampled[threshold - 1] = data[data_length - 1];

            return sampled;
        }

        private TimePoint[] ProcessByAlign(TimePoint[] data, Int32 threshold)
        {
            var data_length = data.Length;
            var sampled = new TimePoint[threshold];

            var step = (Double)data_length / threshold;

            // 每个桶选择一个点作为代表
            for (var i = 0; i < threshold; i++)
            {
                // 获取当前桶的范围
                var start = (Int32)Math.Round((i + 0) * step);
                var end = (Int32)Math.Round((i + 1) * step);
                end = end < data_length ? end : data_length;

                TimePoint point = default;
                var min_value = Double.MaxValue;
                for (var j = start; j < end; j++)
                {
                    if (data[j].Value < min_value)
                    {
                        min_value = data[j].Value;
                        point = data[j];
                    }
                }

                // 对齐
                switch (AlignMode)
                {
                    case AlignModes.Left:
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