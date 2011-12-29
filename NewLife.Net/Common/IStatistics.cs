using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Net.Common
{
    /// <summary>统计接口。<see cref="Increment"/>后并不会马上更新统计数据，而是通过定时器定时更新。考虑到对性能的影响，不要开太多的统计计数器。</summary>
    public interface IStatistics
    {
        /// <summary>是否启用统计计数器</summary>
        Boolean Enable { get; set; }

        /// <summary>定时器统计周期。单位毫秒，默认20000ms（Debug时10000ms），小于0时关闭定时器，采用实时计算。越小越准确，但是性能损耗也很大。</summary>
        Int32 Period { get; set; }

        /// <summary>每分钟总操作</summary>
        Int32 TotalPerMinute { get; }

        /// <summary>每小时总操作</summary>
        Int32 TotalPerHour { get; }

        /// <summary>每分钟最大值</summary>
        Int32 MaxPerMinute { get; set; }

        /// <summary>增加计数</summary>
        void Increment();
    }
}