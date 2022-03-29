using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewLife.Algorithms
{
    /// <summary>
    /// 对齐模型。数据采样时X轴对齐
    /// </summary>
    public enum AlignModes
    {
        /// <summary>
        /// 不对齐，原始值
        /// </summary>
        None,

        /// <summary>
        /// 左对齐
        /// </summary>
        Left,

        /// <summary>
        /// 中间对齐
        /// </summary>
        Center,

        /// <summary>
        /// 右对齐
        /// </summary>
        Right,
    }
}