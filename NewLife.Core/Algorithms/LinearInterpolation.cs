using NewLife.Data;

namespace NewLife.Algorithms;

/// <summary>
/// 线性插值
/// </summary>
public class LinearInterpolation : IInterpolation
{
    /// <summary>
    /// 插值处理（线性插值公式：v = v0 + (t - t0) * (v1 - v0) / (t1 - t0)）。
    /// </summary>
    /// <param name="data">数据序列，要求按时间升序排列</param>
    /// <param name="prev">上一个点索引（起点）</param>
    /// <param name="next">下一个点索引（终点）</param>
    /// <param name="current">当前插值时间值，可在区间内，也可在区间外（外推）</param>
    /// <returns>插值/外推后的数值</returns>
    /// <remarks>
    /// 保护：
    /// 1. 如果 prev/next 越界或 data 为空将抛出异常。
    /// 2. 如果 prev == next 或时间差为 0（异常数据），直接返回该点的原值，避免除零。
    /// </remarks>
    public Double Process(TimePoint[] data, Int32 prev, Int32 next, Int64 current)
    {
        if (data == null || data.Length == 0) throw new ArgumentNullException(nameof(data));
        if (prev < 0 || prev >= data.Length) throw new ArgumentOutOfRangeException(nameof(prev));
        if (next < 0 || next >= data.Length) throw new ArgumentOutOfRangeException(nameof(next));

        if (prev == next) return data[prev].Value;

        var dt = data[next].Time - data[prev].Time;
        if (dt == 0) return data[prev].Value; // 异常：时间相同，避免除零

        var rate = (data[next].Value - data[prev].Value) / dt;
        return data[prev].Value + (current - data[prev].Time) * rate;
    }
}