using System;

namespace NewLife.Algorithms
{
    /// <summary>
    /// 范围
    /// </summary>
    public struct Range
    {
        /// <summary>
        /// 开始，包含
        /// </summary>
        public Int32 Start;

        /// <summary>
        /// 结束，不包含
        /// </summary>
        public Int32 End;

        /// <summary>
        /// 已重载
        /// </summary>
        /// <returns></returns>
        public override String ToString() => $"({Start}, {End})";
    }
}