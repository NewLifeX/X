using System;

namespace XCode.Shards
{
    /// <summary>分表策略</summary>
    public interface IShardPolicy
    {
        /// <summary>为实体对象、时间、雪花Id等计算分表分库</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        ShardModel Shard(Object value);

        /// <summary>从时间区间中计算多个分表分库</summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        ShardModel[] Shards(DateTime start, DateTime end);

        /// <summary>从查询表达式中计算多个分表分库</summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        ShardModel[] Shards(Expression expression);
    }
}