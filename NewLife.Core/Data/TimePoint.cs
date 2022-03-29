using System;

namespace NewLife.Data
{
    /// <summary>
    /// 时序点，用于时序数据计算
    /// </summary>
    public struct TimePoint
    {
        /// <summary>
        /// 时间
        /// </summary>
        public Int64 Time;

        /// <summary>
        /// 数值
        /// </summary>
        public Double Value;

        /// <summary>
        /// 已重载
        /// </summary>
        /// <returns></returns>
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
}