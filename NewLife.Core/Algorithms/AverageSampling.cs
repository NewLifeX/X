using System;
using System.Linq;
using NewLife.Data;

namespace NewLife.Algorithms
{
    /// <summary>
    /// 平均值采样算法
    /// </summary>
    public class AverageSampling : ISampling
    {
        /// <summary>
        /// 对齐模式。每个桶X轴对齐方式
        /// </summary>
        public AlignModes AlignMode { get; set; } = AlignModes.Left;

        /// <summary>
        /// 桶大小。若指定，则采用固定桶大小，例如每分钟一个桶
        /// </summary>
        public Int32 BucketSize { get; set; }

        /// <summary>
        /// 桶偏移。X轴对桶大小取模后的偏移量
        /// </summary>
        public Int32 BucketOffset { get; set; }

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

            var xs = new Int64[data.Length];
            for (var i = 0; i < data.Length; i++) xs[i] = data[i].Time;

            var buckets = BucketSize > 0 ?
                SamplingHelper.SplitByFixedSize(xs, BucketSize, BucketOffset) :
                SamplingHelper.SplitByAverage(data.Length, threshold, true);

            // 每个桶选择一个点作为代表
            var sampled = new TimePoint[threshold];
            for (var i = 0; i < buckets.Length; i++)
            {
                var b = buckets[i];

                TimePoint point = default;
                var vs = 0.0;
                for (var j = b.Start; j < b.End; j++)
                {
                    vs += data[j].Value;
                }
                point.Value = vs / (b.End - b.Start);

                // 对齐
                switch (AlignMode)
                {
                    case AlignModes.Left:
                    default:
                        point.Time = data[b.Start].Time;
                        break;
                    case AlignModes.Right:
                        point.Time = data[b.End - 1].Time;
                        break;
                    case AlignModes.Center:
                        point.Time = data[(Int32)Math.Round((b.Start + b.End - 1) / 2.0)].Time;
                        break;
                }

                sampled[i] = point;
            }

            return sampled;
        }
    }
}