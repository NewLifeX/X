using System.Collections.Concurrent;

namespace System
{
    /// <summary>并行字典扩展</summary>
    public static class ConcurrentDictionaryExtensions
    {
        /// <summary>从并行字典中删除</summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static Boolean Remove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key) => dict.TryRemove(key, out var value);
    }
}