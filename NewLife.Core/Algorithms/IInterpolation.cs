using NewLife.Data;

namespace NewLife.Algorithms;

/// <summary>
/// 插值算法。
/// 典型线性插值公式：v = v0 + (t - t0) * (v1 - v0) / (t1 - t0)。
/// 实现应保证在 t1 == t0 或索引非法时的防护策略（抛异常 / 返回端点值）。
/// </summary>
public interface IInterpolation
{
    /// <summary>
    /// 插值处理
    /// </summary>
    /// <param name="data">数据序列，建议按时间升序</param>
    /// <param name="prev">上一个点索引（起点）</param>
    /// <param name="next">下一个点索引（终点）</param>
    /// <param name="current">当前点时间值（区间内或外推）</param>
    /// <returns>插值后的值</returns>
    Double Process(TimePoint[] data, Int32 prev, Int32 next, Int64 current);
}