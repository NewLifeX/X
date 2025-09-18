using System.Collections;
using System.Collections.Concurrent;

namespace NewLife.Collections;

/// <summary>并行哈希集合</summary>
/// <remarks>
/// 主要用于频繁添加删除而又要遍历的场合。
/// 基于 <see cref="ConcurrentDictionary{TKey, TValue}"/> 实现，所有操作线程安全。
/// </remarks>
public class ConcurrentHashSet<T> : IEnumerable<T> where T : notnull
{
    private readonly ConcurrentDictionary<T, Byte> _dic = new();

    /// <summary>是否空集合</summary>
    public Boolean IsEmpty => _dic.IsEmpty;

    /// <summary>元素个数</summary>
    public Int32 Count => _dic.Count;

    /// <summary>是否包含元素（旧命名，建议使用 <see cref="Contains"/>）。</summary>
    /// <param name="item">元素</param>
    /// <returns>是否存在</returns>
    [Obsolete("Use Contains instead")]
    public Boolean Contain(T item) => _dic.ContainsKey(item);

    /// <summary>是否包含元素</summary>
    /// <param name="item">元素</param>
    /// <returns>是否存在</returns>
    public Boolean Contains(T item) => _dic.ContainsKey(item);

    /// <summary>尝试添加</summary>
    /// <param name="item">元素</param>
    /// <returns>是否成功加入（已存在则返回 false）</returns>
    public Boolean TryAdd(T item) => _dic.TryAdd(item, 0);

    /// <summary>尝试删除</summary>
    /// <param name="item">元素</param>
    /// <returns>是否成功删除</returns>
    public Boolean TryRemove(T item) => _dic.TryRemove(item, out _);

    #region IEnumerable<T> 成员
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _dic.Keys.GetEnumerator();
    #endregion

    #region IEnumerable 成员
    IEnumerator IEnumerable.GetEnumerator() => _dic.Keys.GetEnumerator();
    #endregion
}