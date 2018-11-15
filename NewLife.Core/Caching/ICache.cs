using System;
using System.Collections.Generic;

namespace NewLife.Caching
{
    /// <summary>缓存接口</summary>
    public interface ICache
    {
        #region 属性
        /// <summary>名称</summary>
        String Name { get; }

        /// <summary>默认缓存时间。默认0秒表示不过期</summary>
        Int32 Expire { get; set; }

        /// <summary>获取和设置缓存，永不过期</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Object this[String key] { get; set; }

        /// <summary>缓存个数</summary>
        Int32 Count { get; }

        /// <summary>所有键</summary>
        ICollection<String> Keys { get; }
        #endregion

        #region 基础操作
        /// <summary>是否包含缓存项</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Boolean ContainsKey(String key);

        /// <summary>设置缓存项</summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expire">过期时间，秒。小于0时采用默认缓存时间<seealso cref="Expire"/></param>
        /// <returns></returns>
        Boolean Set<T>(String key, T value, Int32 expire = -1);

        /// <summary>设置缓存项</summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expire">过期时间</param>
        /// <returns></returns>
        Boolean Set<T>(String key, T value, TimeSpan expire);

        /// <summary>获取缓存项</summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        T Get<T>(String key);

        /// <summary>批量移除缓存项</summary>
        /// <param name="keys">键集合</param>
        /// <returns></returns>
        Int32 Remove(params String[] keys);

        /// <summary>清空所有缓存项</summary>
        void Clear();

        /// <summary>设置缓存项有效期</summary>
        /// <param name="key">键</param>
        /// <param name="expire">过期时间</param>
        Boolean SetExpire(String key, TimeSpan expire);

        /// <summary>获取缓存项有效期</summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        TimeSpan GetExpire(String key);
        #endregion

        #region 集合操作
        /// <summary>批量获取缓存项</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="keys"></param>
        /// <returns></returns>
        IDictionary<String, T> GetAll<T>(IEnumerable<String> keys);

        /// <summary>批量设置缓存项</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <param name="expire">过期时间，秒。小于0时采用默认缓存时间<seealso cref="Expire"/></param>
        void SetAll<T>(IDictionary<String, T> values, Int32 expire = -1);

        /// <summary>获取列表</summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="key">键</param>
        /// <returns></returns>
        IList<T> GetList<T>(String key);

        /// <summary>获取哈希</summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="key">键</param>
        /// <returns></returns>
        IDictionary<String, T> GetDictionary<T>(String key);

        /// <summary>获取队列</summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="key">键</param>
        /// <returns></returns>
        IProducerConsumer<T> GetQueue<T>(String key);

        /// <summary>获取Set</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        ICollection<T> GetSet<T>(String key);
        #endregion

        #region 高级操作
        /// <summary>添加，已存在时不更新</summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expire">过期时间，秒。小于0时采用默认缓存时间<seealso cref="Cache.Expire"/></param>
        /// <returns></returns>
        Boolean Add<T>(String key, T value, Int32 expire = -1);

        /// <summary>设置新值并获取旧值，原子操作</summary>
        /// <remarks>
        /// 常常配合Increment使用，用于累加到一定数后重置归零，又避免多线程冲突。
        /// </remarks>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        T Replace<T>(String key, T value);

        /// <summary>累加，原子操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        Int64 Increment(String key, Int64 value);

        /// <summary>累加，原子操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        Double Increment(String key, Double value);

        /// <summary>递减，原子操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        Int64 Decrement(String key, Int64 value);

        /// <summary>递减，原子操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        Double Decrement(String key, Double value);
        #endregion

        #region 事务
        /// <summary>提交变更。部分提供者需要刷盘</summary>
        /// <returns></returns>
        Int32 Commit();

        /// <summary>申请分布式锁</summary>
        /// <param name="key">要锁定的key</param>
        /// <param name="msTimeout"></param>
        /// <returns></returns>
        IDisposable AcquireLock(String key, Int32 msTimeout);
        #endregion

        #region 性能测试
        /// <summary>多线程性能测试</summary>
        /// <param name="rand">随机读写</param>
        /// <param name="batch">批量操作。默认0不分批</param>
        void Bench(Boolean rand = false, Int32 batch = 0);
        #endregion
    }
}