using System.Collections.Concurrent;

namespace System;

/// <summary>并发字典扩展</summary>
public static class ConcurrentDictionaryExtensions
{
    /// <summary>从并发字典中删除</summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="dict"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static Boolean Remove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key) where TKey : notnull => dict.TryRemove(key, out _);
}