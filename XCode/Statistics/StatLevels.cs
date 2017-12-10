using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XCode.Statistics
{
    /// <summary>统计层级</summary>
    [Description("统计层级")]
    public enum StatLevels
    {
        /// <summary>年</summary>
        [Description("年")]
        Year = 1,

        /// <summary>月</summary>
        [Description("月")]
        Month = 2,

        /// <summary>天</summary>
        [Description("天")]
        Day = 3,

        /// <summary>小时</summary>
        [Description("小时")]
        Hour = 4,

        /// <summary>分钟</summary>
        [Description("分钟")]
        Minute = 5,
    }
}
