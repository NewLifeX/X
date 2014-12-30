using System;
using System.Collections.Generic;
using System.Text;

namespace XCode
{
    /// <summary>查询模式</summary>
    [Flags]
    public enum SelectModes
    {
        /// <summary>空，不返回任何数据</summary>
        None = 0,

        /// <summary>返回列表数据</summary>
        List = 1,

        /// <summary>返回总记录数</summary>
        TotalCount = 2,

        /// <summary>同时返回列表数据和总记录数</summary>
        Both = 3
    }
}