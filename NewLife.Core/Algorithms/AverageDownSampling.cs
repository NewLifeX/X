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
        public TimePoint[] Process(TimePoint[] data, Int32 threshold)
        {
            if (data == null || data.Length < 2) return data;
            if (threshold < 2 || threshold >= data.Length) return data;

            var source = new BucketSource
            {
                Data = data,
                Threshod = threshold,
                Length = data.Length,
                BucketSize = BucketSize,
                BucketOffset = BucketOffset
            };
            source.Init();

            // 每个桶选择一个点作为代表
            var i = source.Offset;
            var sampled = new TimePoint[threshold];
            foreach (var item in source)
            {
                TimePoint point = default;
                var vs = 0.0;
                for (var j = item.Start; j < item.End; j++)
                {
                    vs += data[j].Value;
                }
                point.Value = vs / (item.End - item.Start);

                // 对齐
                switch (AlignMode)
                {
                    case AlignModes.Left:
                    default:
                        point.Time = data[item.Start].Time;
                        break;
                    case AlignModes.Right:
                        point.Time = data[item.End - 1].Time;
                        break;
                    case AlignModes.Center:
                        point.Time = data[(Int32)Math.Round((item.Start + item.End) / 2.0)].Time;
                        break;
                }

                sampled[i] = point;
            }

            return sampled;
        }
    }
}