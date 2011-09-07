using System.Collections.Generic;
using NewLife.Reflection;
using System;

namespace NewLife.Collections
{
    /// <summary>
    /// 字典缓存。当指定键的缓存项不存在时，调用委托获取值，并写入缓存
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    public class DictionaryCache<TKey, TValue> : Dictionary<TKey, TValue>
    {
        private Boolean LockKey = false;

        /// <summary>
        /// 实例化字典缓存，GetItem锁定字典
        /// </summary>
        public DictionaryCache() { }

        /// <summary>
        /// 实例化字典缓存，参数lockKey指定GetItem是锁定字典(false)还是锁定键值(true)
        /// </summary>
        /// <param name="lockKey"></param>
        public DictionaryCache(Boolean lockKey) { LockKey = lockKey; }

        /// <summary>
        /// 重写索引器。取值时如果没有该项则返回默认值；赋值时如果已存在该项则覆盖，否则添加。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public new TValue this[TKey key]
        {
            get
            {
                TValue value;
                if (TryGetValue(key, out value)) return value;

                return default(TValue);
            }
            set
            {
                if (ContainsKey(key))
                    base[key] = value;
                else
                    base.Add(key, value);
            }
        }

        /// <summary>
        /// 扩展获取数据项，当数据项不存在时，通过调用委托获取数据项。线程安全。
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="func">获取值的委托，该委托以键作为参数</param>
        /// <returns></returns>
        public virtual TValue GetItem(TKey key, Func<TKey, TValue> func)
        {
            return GetItem(key, func, true);
        }

        /// <summary>
        /// 扩展获取数据项，当数据项不存在时，通过调用委托获取数据项。线程安全。
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="func">获取值的委托，该委托以键作为参数</param>
        /// <param name="cacheDefault">是否缓存默认值，可选参数，默认缓存</param>
        /// <returns></returns>
        public virtual TValue GetItem(TKey key, Func<TKey, TValue> func, Boolean cacheDefault)
        {
            TValue value;
            if (TryGetValue(key, out value)) return value;
            lock (LockKey ? (Object)key : this)
            {
                if (TryGetValue(key, out value)) return value;

                value = func(key);
                //Add(key, value);
                if (cacheDefault || Object.Equals(value, default(TValue))) this[key] = value;

                return value;
            }
        }

        /// <summary>
        /// 扩展获取数据项，当数据项不存在时，通过调用委托获取数据项。线程安全。
        /// </summary>
        /// <typeparam name="TArg">参数类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="arg">参数</param>
        /// <param name="func">获取值的委托，该委托除了键参数外，还有一个泛型参数</param>
        /// <returns></returns>
        public virtual TValue GetItem<TArg>(TKey key, TArg arg, Func<TKey, TArg, TValue> func)
        {
            return GetItem<TArg>(key, arg, func, true);
        }

        /// <summary>
        /// 扩展获取数据项，当数据项不存在时，通过调用委托获取数据项。线程安全。
        /// </summary>
        /// <typeparam name="TArg">参数类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="arg">参数</param>
        /// <param name="func">获取值的委托，该委托除了键参数外，还有一个泛型参数</param>
        /// <param name="cacheDefault">是否缓存默认值，可选参数，默认缓存</param>
        /// <returns></returns>
        public virtual TValue GetItem<TArg>(TKey key, TArg arg, Func<TKey, TArg, TValue> func, Boolean cacheDefault)
        {
            TValue value;
            if (TryGetValue(key, out value)) return value;
            lock (LockKey ? (Object)key : this)
            {
                if (TryGetValue(key, out value)) return value;

                value = func(key, arg);
                //Add(key, value);
                if (cacheDefault || Object.Equals(value, default(TValue))) this[key] = value;

                return value;
            }
        }

        /// <summary>
        /// 扩展获取数据项，当数据项不存在时，通过调用委托获取数据项。线程安全。
        /// </summary>
        /// <typeparam name="TArg">参数类型</typeparam>
        /// <typeparam name="TArg2">参数类型2</typeparam>
        /// <param name="key">键</param>
        /// <param name="arg">参数</param>
        /// <param name="arg2">参数2</param>
        /// <param name="func">获取值的委托，该委托除了键参数外，还有两个泛型参数</param>
        /// <returns></returns>
        public virtual TValue GetItem<TArg, TArg2>(TKey key, TArg arg, TArg2 arg2, Func<TKey, TArg, TArg2, TValue> func)
        {
            return GetItem<TArg, TArg2>(key, arg, arg2, func, true);
        }

        /// <summary>
        /// 扩展获取数据项，当数据项不存在时，通过调用委托获取数据项。线程安全。
        /// </summary>
        /// <typeparam name="TArg">参数类型</typeparam>
        /// <typeparam name="TArg2">参数类型2</typeparam>
        /// <param name="key">键</param>
        /// <param name="arg">参数</param>
        /// <param name="arg2">参数2</param>
        /// <param name="func">获取值的委托，该委托除了键参数外，还有两个泛型参数</param>
        /// <param name="cacheDefault">是否缓存默认值，可选参数，默认缓存</param>
        /// <returns></returns>
        public virtual TValue GetItem<TArg, TArg2>(TKey key, TArg arg, TArg2 arg2, Func<TKey, TArg, TArg2, TValue> func, Boolean cacheDefault)
        {
            TValue value;
            if (TryGetValue(key, out value)) return value;
            lock (LockKey ? (Object)key : this)
            {
                if (TryGetValue(key, out value)) return value;

                value = func(key, arg, arg2);
                //Add(key, value);
                if (cacheDefault || Object.Equals(value, default(TValue))) this[key] = value;

                return value;
            }
        }

        /// <summary>
        /// 扩展获取数据项，当数据项不存在时，通过调用委托获取数据项。线程安全。
        /// </summary>
        /// <typeparam name="TArg">参数类型</typeparam>
        /// <typeparam name="TArg2">参数类型2</typeparam>
        /// <typeparam name="TArg3">参数类型3</typeparam>
        /// <param name="key">键</param>
        /// <param name="arg">参数</param>
        /// <param name="arg2">参数2</param>
        /// <param name="arg3">参数3</param>
        /// <param name="func">获取值的委托，该委托除了键参数外，还有三个泛型参数</param>
        /// <returns></returns>
        public virtual TValue GetItem<TArg, TArg2, TArg3>(TKey key, TArg arg, TArg2 arg2, TArg3 arg3, Func<TKey, TArg, TArg2, TArg3, TValue> func)
        {
            return GetItem<TArg, TArg2, TArg3>(key, arg, arg2, arg3, func, true);
        }

        /// <summary>
        /// 扩展获取数据项，当数据项不存在时，通过调用委托获取数据项。线程安全。
        /// </summary>
        /// <typeparam name="TArg">参数类型</typeparam>
        /// <typeparam name="TArg2">参数类型2</typeparam>
        /// <typeparam name="TArg3">参数类型3</typeparam>
        /// <param name="key">键</param>
        /// <param name="arg">参数</param>
        /// <param name="arg2">参数2</param>
        /// <param name="arg3">参数3</param>
        /// <param name="func">获取值的委托，该委托除了键参数外，还有三个泛型参数</param>
        /// <param name="cacheDefault">是否缓存默认值，可选参数，默认缓存</param>
        /// <returns></returns>
        public virtual TValue GetItem<TArg, TArg2, TArg3>(TKey key, TArg arg, TArg2 arg2, TArg3 arg3, Func<TKey, TArg, TArg2, TArg3, TValue> func, Boolean cacheDefault)
        {
            TValue value;
            if (TryGetValue(key, out value)) return value;
            lock (LockKey ? (Object)key : this)
            {
                if (TryGetValue(key, out value)) return value;

                value = func(key, arg, arg2, arg3);
                //Add(key, value);
                if (cacheDefault || Object.Equals(value, default(TValue))) this[key] = value;

                return value;
            }
        }
    }
}