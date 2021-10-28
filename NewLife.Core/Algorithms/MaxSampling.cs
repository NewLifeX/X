using System;
using NewLife.Data;

namespace NewLife.Algorithms
{
    /// <summary>
    /// 最大值采样算法
    /// </summary>
    public class MaxSampling : ISampling
    {
        /// <summary>
        /// 对齐模式。每个桶X轴对齐方式
        /// </summary>
        public AlignModes AlignMode { get; set; }

        /// <summary>
        /// 插值填充算法
        /// </summary>
        public IInterpolation Interpolation { get; set; } = new LinearInterpolation();

        /// <summary>
        /// 降采样处理
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <param name="threshold">阈值，采样数</param>
        /// <returns></returns>
        public TimePoint[] Down(TimePoint[] data, Int32 threshold)
        {
            if (data == null || data.Length < 2) return data;
            if (threshold < 2 || threshold >= data.Length) return data;

            var buckets = SamplingHelper.SplitByAverage(data.Length, threshold, true);

            // 每个桶选择一个点作为代表
            var sampled = new TimePoint[buckets.Length];
            for (var i = 0; i < buckets.Length; i++)
            {
                var item = buckets[i];
                TimePoint point = default;
                var max_value = Double.MinValue;
                for (var j = item.Start; j < item.End; j++)
                {
                    if (data[j].Value > max_value)
                    {
                        max_value = data[j].Value;
                        point = data[j];
                    }
                }

                // 对齐
                switch (AlignMode)
                {
                    case AlignModes.Left:
                        point.Time = data[item.Start].Time;
                        break;
                    case AlignModes.Right:
                        point.Time = data[item.End - 1].Time;
                        break;
                    case AlignModes.Center:
                        point.Time = data[(Int32)Math.Round((item.Start + item.End) / 2.0)].Time;
                        break;
                }

                sampled[i++] = point;
            }

            // 第一个点和最后一个点
            if (AlignMode == AlignModes.None)
            {
                sampled[0] = data[0];
                sampled[threshold - 1] = data[data.Length - 1];
            }

            return sampled;
        }

        /// <summary>
        /// 混合处理，降采样和插值
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <param name="size">桶大小。如60/3600/86400</param>
        /// <param name="offset">偏移量。时间不是对齐零点时使用</param>
        /// <returns></returns>
        public TimePoint[] Process(TimePoint[] data, Int32 size, Int32 offset = 0)
        {
            if (data == null || data.Length < 2) return data;
            if (size <= 1) return data;

            var xs = new Int64[data.Length];
            for (var i = 0; i < data.Length; i++) xs[i] = data[i].Time;

            var buckets = SamplingHelper.SplitByFixedSize(xs, size, offset);

            // 每个桶选择一个点作为代表
            var sampled = new TimePoint[buckets.Length];
            var last = 0;
            for (var i = 0; i < buckets.Length; i++)
            {
                // 断层，插值
                var item = buckets[i];
                if (item.Start < 0)
                {
                    sampled[i].Time = i * size;
                    sampled[i].Value = Interpolation.Process(data, last, item.End, i);
                    continue;
                }

                TimePoint point = default;
                var vs = 0.0;
                for (var j = item.Start; j < item.End; j++)
                {
                    vs += data[j].Value;
                }
                last = item.End - 1;
                point.Value = vs / (item.End - item.Start);

                // 对齐
                switch (AlignMode)
                {
                    case AlignModes.Left:
                    default:
                        point.Time = i * size;
                        break;
                    case AlignModes.Right:
                        point.Time = (i + 1) * size - 1;
                        break;
                    case AlignModes.Center:
                        point.Time = data[(Int32)Math.Round((i + 0.5) * size)].Time;
                        break;
                }

                sampled[i] = point;
            }

            return sampled;
        }
    }
}