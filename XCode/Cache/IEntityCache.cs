using System;
using System.Collections.Generic;

namespace XCode.Cache
{
    /// <summary>缓存基接口</summary>
    public interface IEntityCacheBase
    {
        /// <summary>连接名</summary>
        String ConnName { get; set; }

        /// <summary>表名</summary>
        String TableName { get; set; }
    }

    /// <summary>实体缓存接口</summary>
    public interface IEntityCache : IEntityCacheBase
    {
        /// <summary>实体集合。因为涉及一个转换，数据量大时很耗性能，建议不要使用。</summary>
        IList<IEntity> Entities { get; }

        /// <summary>清除缓存</summary>
        void Clear(String reason);
    }

    /// <summary>单对象缓存接口</summary>
    public interface ISingleEntityCache : IEntityCacheBase
    {
        /// <summary>过期时间。单位是秒，默认60秒</summary>
        Int32 Expire { get; set; }

        /// <summary>最大实体数。默认10000</summary>
        Int32 MaxEntity { get; set; }

        /// <summary>是否在使用缓存</summary>
        Boolean Using { get; set; }

        /// <summary>获取数据</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        IEntity this[Object key] { get; }

        /// <summary>根据从键获取实体数据</summary>
        /// <param name="slaveKey"></param>
        /// <returns></returns>
        IEntity GetItemWithSlaveKey(String slaveKey);

        /// <summary>是否包含指定主键</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Boolean ContainsKey(Object key);

        /// <summary>是否包含指定从键</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Boolean ContainsSlaveKey(String key);

        /// <summary>向单对象缓存添加项</summary>
        /// <param name="value">实体对象</param>
        /// <returns></returns>
        Boolean Add(IEntity value);

        /// <summary>移除指定项</summary>
        /// <param name="entity"></param>
        void Remove(IEntity entity);

        /// <summary>清除所有数据</summary>
        /// <param name="reason">清除缓存原因</param>
        void Clear(String reason);
    }

    /// <summary></summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TEntity"></typeparam>
    public interface ISingleEntityCache<TKey, TEntity> : ISingleEntityCache where TEntity : Entity<TEntity>, new()
    {
        /// <summary>获取数据</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        TEntity this[TKey key] { get; }

        /// <summary>获取缓存主键的方法，默认方法为获取实体主键值</summary>
        Func<TEntity, TKey> GetKeyMethod { get; set; }

        /// <summary>查找数据的方法</summary>
        Func<TKey, TEntity> FindKeyMethod { get; set; }

        /// <summary>从键是否区分大小写</summary>
        Boolean SlaveKeyIgnoreCase { get; set; }

        /// <summary>根据从键查找数据的方法</summary>
        Func<String, TEntity> FindSlaveKeyMethod { get; set; }

        /// <summary>获取缓存从键的方法，默认为空</summary>
        Func<TEntity, String> GetSlaveKeyMethod { get; set; }
    }
}