namespace NewLife.Collections;

/// <summary>可空字典。获取数据时如果指定键不存在可返回空而不是抛出异常</summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
public class NullableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IDictionary<TKey, TValue> where TKey : notnull
{
    /// <summary>实例化一个可空字典</summary>
    public NullableDictionary() { }

    /// <summary>指定比较器实例化一个可空字典</summary>
    /// <param name="comparer"></param>
    public NullableDictionary(IEqualityComparer<TKey> comparer) : base(comparer) { }

    /// <summary>实例化一个可空字典</summary>
    /// <param name="dic"></param>
    public NullableDictionary(IDictionary<TKey, TValue> dic) : base(dic) { }

    /// <summary>实例化一个可空字典</summary>
    /// <param name="dic"></param>
    /// <param name="comparer"></param>
    public NullableDictionary(IDictionary<TKey, TValue> dic, IEqualityComparer<TKey> comparer) : base(dic, comparer) { }

    /// <summary>
    /// 获取 或 设置 数据。
    /// 当键不存在时返回 <c>default(TValue)</c>，而不是抛出 <see cref="KeyNotFoundException"/>。
    /// 注意：当 <typeparamref name="TValue"/> 为值类型时，这意味着返回其默认值。
    /// </summary>
    /// <param name="item">键</param>
    /// <returns>键对应的值或默认值</returns>
    public new TValue this[TKey item]
    {
        get
        {
            if (TryGetValue(item, out var v)) return v;

            return default!;
        }
        set
        {
            base[item] = value;
        }
    }
}