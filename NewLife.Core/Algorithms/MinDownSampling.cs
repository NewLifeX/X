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

            // 桶大小，预留开始结束位置
            var source = AlignMode == AlignModes.None ?
                new BucketSource { Data = data, Threshod = threshold - 2, Offset = 1, Length = data.Length - 2 } :
                new BucketSource { Data = data, Threshod = threshold, Length = data.Length };
            source.Init();

            // 每个桶选择一个点作为代表
            var i = source.Offset;
            var sampled = new TimePoint[threshold];
            foreach (var item in source)
            {
                TimePoint point = default;
                var min_value = Double.MaxValue;
                for (var j = item.Start; j < item.End; j++)
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
    }
}