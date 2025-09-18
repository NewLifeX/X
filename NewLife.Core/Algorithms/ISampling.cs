using NewLife.Data;

namespace NewLife.Algorithms;

/// <summary>
/// 采样接口。负责降采样和插值处理，用于处理时序数据
/// </summary>
public interface ISampling
{
    /// <summary>
    /// 对齐模式。每个桶X轴对齐方式
    /// </summary>
    AlignModes AlignMode { get; set; }

    /// <summary>
    /// 插值填充算法
    /// </summary>
    IInterpolation? Interpolation { get; set; }

    /// <summary>
    /// 降采样处理
    /// </summary>
    /// <param name="data">原始数据</param>
    /// <param name="threshold">阈值，采样数</param>
    /// <returns></returns>
    TimePoint[] Down(TimePoint[] data, Int32 threshold);

    /// <summary>
    /// 混合处理，降采样和插值
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
    /// 按照指定桶数平均分，可指定保留头尾。
    /// 当 retainEdge = true 时，首尾两个桶分别只包含第一个和最后一个数据点，其余桶做平均分配。
    /// </summary>
    /// <param name="dataLength">数据长度，必须 &gt; 0</param>
    /// <param name="threshold">目标桶数，必须 &gt; 2</param>
    /// <param name="retainEdge">是否保留首尾两个边界点</param>
    /// <returns>桶索引区间数组（左闭右开）</returns>
    public static IndexRange[] SplitByAverage(Int32 dataLength, Int32 threshold, Boolean retainEdge = true)
    {
        if (dataLength == 0) throw new ArgumentNullException(nameof(dataLength));
        if (threshold <= 2) throw new ArgumentNullException(nameof(threshold));

        var buckets = new IndexRange[threshold];
        if (retainEdge)
        {
            var step = (Double)(dataLength - 2) / (threshold - 2);
            var v = 0d;
            for (var i = 1; i < threshold - 1; i++)
            {
                buckets[i].Start = (Int32)Math.Round(v) + 1;
                buckets[i].End = (Int32)Math.Round(v += step) + 1;
                if (buckets[i].End > dataLength - 1) buckets[i].End = dataLength - 1;
            }
            buckets[0].Start = 0;
            buckets[0].End = 1;
            buckets[threshold - 1].Start = dataLength - 1;
            buckets[threshold - 1].End = dataLength; // 最后一个桶到 length
        }
        else
        {
            var step = (Double)dataLength / threshold;
            var v = 0d;
            for (var i = 0; i < threshold; i++)
            {
                buckets[i].Start = (Int32)Math.Round(v);
                buckets[i].End = (Int32)Math.Round(v += step);
                if (buckets[i].End > dataLength) buckets[i].End = dataLength;
            }
        }

        return buckets;
    }

    /// <summary>
    /// 按照固定时间间隔，拆分数据轴为多个桶。
    /// 返回的 IndexRange 的 Start/End 为原数组索引区间 [Start,End)；Start = -1 表示该桶无数据（可用于插值）。
    /// </summary>
    /// <param name="data">原始时间数组（升序）</param>
    /// <param name="size">桶大小。如60/3600/86400</param>
    /// <param name="offset">偏移量。时间不是对齐零点时使用，必须 0 &lt;= offset &lt; size</param>
    /// <returns>索引区间数组</returns>
    public static IndexRange[] SplitByFixedSize(Int64[] data, Int32 size, Int32 offset = 0)
    {
        if (data == null || data.Length == 0) throw new ArgumentNullException(nameof(data));
        if (size <= 0) throw new ArgumentNullException(nameof(size));
        if (offset >= size) throw new ArgumentOutOfRangeException(nameof(offset));

        // 计算首尾对齐后的时间边界
        var start = data[0] / size * size + offset;
        if (start > data[0]) start -= size;
        var last = data[^1];
        var end = last / size * size + offset;
        if (end > last) end -= size;

        var buckets = new IndexRange[(end - start) / size + 1];

        // 计算每个桶的头尾
        var p = 0;
        var idx = 0;
        for (var time = start; time <= end; p++)
        {
            IndexRange r = default;
            r.Start = -1; // -1 表示该桶无数据
            r.End = -1;
            var next = time + size;

            // 顺序遍历原始数据，假设为升序
            for (; idx < data.Length; idx++)
            {
                // 如果超过了当前桶的结尾，则换下一个桶
                if (data[idx] >= next)
                {
                    r.End = idx;
                    break;
                }

                if (r.Start < 0 && time <= data[idx]) r.Start = idx;
            }
            if (r.End < 0) r.End = idx;

            buckets[p] = r;
            time = next;
        }

        return buckets; // 避免 ToArray() 冗余复制
    }
}