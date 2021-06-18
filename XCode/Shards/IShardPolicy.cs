using System;

namespace XCode.Shards
{
    /// <summary>分表策略</summary>
    public interface IShardPolicy
    {
        /// <summary>为实体对象计算分表分库</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        ShardModel Get(IEntity entity);

        /// <summary>为时间计算分表分库</summary>
        /// <param name="time"></param>
        /// <returns></returns>
        ShardModel Get(DateTime time);

        /// <summary>从查询表达式中计算多个分表分库</summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        ShardModel[] Gets(Expression expression);
    }
}