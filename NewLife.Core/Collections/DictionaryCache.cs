using System.Collections.Generic;
using NewLife.Reflection;

namespace NewLife.Collections
{
    /// <summary>
    /// 字典缓存
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    public class DictionaryCache<TKey, TValue> : Dictionary<TKey, TValue>
    {
        /// <summary>
        /// 扩展获取数据项，当数据项不存在时，通过调用委托获取数据项。线程安全。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public virtual TValue GetItem(TKey key, Func<TKey, TValue> func)
        {
            TValue value;
            if (TryGetValue(key, out value)) return value;
            lock (this)
            {
                if (TryGetValue(key, out value)) return value;

                value = func(key);
                //Add(key, value);
                this[key] = value;

                return value;
            }
        }

        /// <summary>
        /// 扩展获取数据项，当数据项不存在时，通过调用委托获取数据项。线程安全。
        /// </summary>
        /// <typeparam name="TArgs">参数类型</typeparam>
        /// <param name="key"></param>
        /// <param name="args"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public virtual TValue GetItem<TArgs>(TKey key, TArgs args, Func<TKey, TArgs, TValue> func)
        {
            TValue value;
            if (TryGetValue(key, out value)) return value;
            lock (this)
            {
                if (TryGetValue(key, out value)) return value;

                value = func(key, args);
                //Add(key, value);
                this[key] = value;

                return value;
            }
        }
    }
}