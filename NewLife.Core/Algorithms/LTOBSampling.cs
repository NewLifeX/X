using System;
using NewLife.Data;
using NewLife.Log;

namespace NewLife.Algorithms
{
    /// <summary>
    /// LTOB最大三角形单桶降采样算法
    /// </summary>
    /// <remarks>
    /// Largest-Triangle-One-Bucket
    /// LTOB最大三角形单桶算法，使用了Whytt算法有效区域的思路，再结合直觉算法中的分桶。
    /// 算法步骤：
    /// 1） 首先确定桶的大小，并将数据点平分到桶中，注意首尾点各占一个桶确保选中
    /// 2） 其次计算每个点和邻接点形成的有效区域，去除无有效区域的点
    /// 3） 在每个桶中选取有效区域最大的点代表当前桶
    /// LTOB算法相比原始的Whytt算法，确保了点分布的相对均匀。每个桶都有一个代表点来表示，从而连接成为一个全局的路由。
    /// </remarks>
    public class LTOBSampling : ISampling
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
        public TimePoint[] Down(TimePoint[] data, Int32 threshold)
        {
            if (data == null || data.Length < 2) return data;
            if (threshold < 2 || threshold >= data.Length) return data;

            // 桶大小，预留开始结束位置
            var source = AlignMode == AlignModes.None ?
                new BucketSource { Data = data, Threshod = threshold - 2, Offset = 1, Length = data.Length - 2 } :
                new BucketSource { Data = data, Threshod = threshold, Length = data.Length };
            source.Init();

            // 三角形选择当前同相邻三个ABC点，选择B，使得三角形有效面积最大
            var i = source.Offset;
            var sampled = new TimePoint[threshold];
            foreach (var item in source)
            {
                //XTrace.WriteLine("i={0} r=({1},{2})", i, item.Start, item.End);
                // 计算每个点的有效区域，并选取有效区域最大的点作为桶的代表点
                TimePoint point = default;
                var max_area = -1.0;
                for (var j = item.Start + 1; j < item.End - 1; j++)
                {
                    // 选择一个点B，计算ABC三角形面积
                    var pointA = data[j - 1];
                    var pointB = data[j];
                    var pointC = data[j + 1];
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