using System;

namespace NewLife.Algorithms
{
    /// <summary>
    /// LTTB降采样算法
    /// </summary>
    /// <remarks>
    /// Largest triangle three buckets
    /// </remarks>
    public class LttbDownSampling : IDownSampling
    {
        /// <summary>
        /// 降采样处理
        /// </summary>
        /// <param name="xAxis"></param>
        /// <param name="data"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        public SamplingData Process(Int32[] xAxis, Double[] data, Int32 threshold)
        {
            if (xAxis == null || xAxis.Length < 2) throw new ArgumentNullException(nameof(xAxis));
            if (data == null || data.Length < 2) throw new ArgumentNullException(nameof(data));
            if (xAxis.Length != data.Length) throw new ArgumentOutOfRangeException(nameof(data));
            if (threshold >= data.Length) return new SamplingData { XAxis = xAxis, Data = data };

            var data_length = data.Length;
            var xx = new Int32[threshold];
            var yy = new Double[threshold];

            // 桶大小，预留开始结束位置
            var step = (Double)(data_length - 2) / (threshold - 2);

            // 三角形第一个点
            var p = 0;
            var next_point = 0;
            xx[0] = xAxis[p];
            yy[0] = data[p];
            for (var i = 0; i < threshold - 2; i++)
            {
                // 计算下一个桶的平均点，向前对齐
                var start = (Int32)Math.Floor((i + 1) * step) + 1;
                var end = (Int32)Math.Floor((i + 2) * step) + 1;
                end = end < data_length ? end : data_length;

                var length = end - start;
                var avg_x = 0;
                var avg_y = 0.0;
                for (; start < end; start++)
                {
                    avg_x += xAxis[start];
                    avg_y += data[start];
                }
                avg_x /= length;
                avg_y /= length;

                // 获取这个桶的范围
                var offset = (Int32)Math.Floor((i + 0) * step) + 1;
                var to = (Int32)Math.Floor((i + 1) * step) + 1;

                // 计算点
                var x = xAxis[p];
                var y = data[p];
                var max_area = -1.0;
                var px = 0;
                var py = 0.0;
                for (; offset < to; offset++)
                {
                    // 计算三角形面积
                    var area = Math.Abs((x - avg_x) * (data[offset] - y) - (x - xAxis[offset]) * (avg_y - y)) * 0.5;
                    if (area > max_area)
                    {
                        max_area = area;
                        px = xAxis[offset];
                        py = data[offset];
                        next_point = offset;
                    }
                }

                xx[i + 1] = px;
                yy[i + 1] = py;
                p = next_point;
            }

            // 最后一个点
            xx[threshold - 1] = xAxis[data_length - 1];
            yy[threshold - 1] = data[data_length - 1];

            return new SamplingData { XAxis = xx, Data = yy };
        }
    }
}