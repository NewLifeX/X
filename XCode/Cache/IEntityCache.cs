using System;

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
        EntityList<IEntity> Entities { get; }

        /// <summary>根据指定项查找</summary>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        IEntity Find(String name, Object value);

        /// <summary>根据指定项查找</summary>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        EntityList<IEntity> FindAll(String name, Object value);

        /// <summary>检索与指定谓词定义的条件匹配的所有元素。</summary>
        /// <param name="match">条件</param>
        /// <returns></returns>
        EntityList<IEntity> FindAll(Predicate<IEntity> match);

        ///// <summary>
        ///// 按指定字段排序
        ///// </summary>
        ///// <param name="name">字段</param>
        ///// <param name="isDesc">是否降序</param>
        //void Sort(String name, Boolean isDesc);
    }

    /// <summary>单对象缓存接口</summary>
    public interface ISingleEntityCache : IEntityCacheBase
    {
        /// <summary>获取数据</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        IEntity this[Object key] { get; }
    }
}