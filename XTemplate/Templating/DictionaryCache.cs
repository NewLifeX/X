using System.Collections.Generic;

namespace XTemplate.Templating
{
    /// <summary>
    /// 字典缓存
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    class DictionaryCache<TKey, TValue> : Dictionary<TKey, TValue>
    {
        public TValue GetItem(TKey key, Func<TKey, TValue> func)
        {
            if (ContainsKey(key)) return this[key];
            lock (this)
            {
                if (ContainsKey(key)) return this[key];

                TValue value = func(key);
                Add(key, value);

                return value;
            }
        }
    }

    /// <summary>
    /// 具有指定参数和返回的委托
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <typeparam name="TResult">返回类型</typeparam>
    /// <param name="arg">参数</param>
    /// <returns></returns>
    public delegate TResult Func<T, TResult>(T arg);
}