using System;

namespace NewLife.Net
{
    /// <summary>统计接口。
    /// <see cref="Increment"/>后更新<see cref="First"/>、<see cref="Last"/>、<see cref="Total"/>，
    /// 但并不会马上更新统计数据，除非<see cref="Enable"/>为true。</summary>
    /// <example>
    /// <code>
    /// private IStatistics _Statistics;
    /// /// &lt;summary&gt;统计信息，默认关闭，通过&lt;see cref="IStatistics.Enable"/&gt;打开。&lt;/summary&gt;
    /// public IStatistics Statistics { get { return _Statistics ?? (_Statistics = NetService.Resolve&lt;IStatistics&gt;()); } }
    /// </code>
    /// </example>
    public interface IStatistics
    {
        /// <summary>是否启用统计。</summary>
        Boolean Enable { get; set; }

        ///// <summary>定时器统计周期。单位毫秒，默认20000ms（Debug时10000ms），小于0时关闭定时器，采用实时计算。越小越准确，但是性能损耗也很大。</summary>
        //Int32 Period { get; set; }

        /// <summary>首次统计时间</summary>
        DateTime First { get; set; }

        /// <summary>最后统计时间</summary>
        DateTime Last { get; }

        /// <summary>每分钟最大值</summary>
        Int32 Total { get; set; }

        /// <summary>每分钟总操作</summary>
        Int32 TotalPerMinute { get; }

        /// <summary>每小时总操作</summary>
        Int32 TotalPerHour { get; }

        /// <summary>每分钟最大值</summary>
        Int32 MaxPerMinute { get; }

        /// <summary>每秒平均</summary>
        Int32 AveragePerSecond { get; }

        /// <summary>每分钟平均</summary>
        Int32 AveragePerMinute { get; }

        /// <summary>增加计数</summary>
        /// <param name="n"></param>
        void Increment(Int32 n = 1);
    }
}