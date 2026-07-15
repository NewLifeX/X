using System;

namespace NewLife.Data;

/// <summary>时序点，用于时序数据计算</summary>
public struct TimePoint
{
    /// <summary>时间</summary>
    public Int64 Time;

    /// <summary>数值</summary>
    public Double Value;

    /// <summary>返回时序点的字符串表示</summary>
    /// <returns>格式为 (Time, Value) 的字符串</returns>
    public override String ToString() => $"({Time}, {Value})";
}

///// <summary>
///// 时序点，用于时序数据计算
///// </summary>
//public struct LongTimePoint
//{
//    /// <summary>
//    /// 时间
//    /// </summary>
//    public Int64 Time;

//    /// <summary>
//    /// 数值
//    /// </summary>
//    public Double Value;
//}