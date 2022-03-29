using System;

namespace NewLife.Data
{
    /// <summary>
    /// 范围
    /// </summary>
    public struct IndexRange
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