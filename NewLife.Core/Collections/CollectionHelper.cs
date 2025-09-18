using System.Collections.Concurrent;
using NewLife.Collections;
using NewLife.Reflection;
using NewLife.Serialization;
using NewLife.Data;
#if NETCOREAPP
using System.Text.Json;
#endif

namespace System.Collections.Generic;

/// <summary>集合扩展</summary>
public static class CollectionHelper
{
    ///// <summary>集合转为数组，加锁确保安全</summary>
    ///// <typeparam name="T"></typeparam>
    ///// <param name="collection"></param>
    ///// <param name="index">数组偏移量。大于0时，新数组将空出来前面一截，把数据拷贝到后面</param>
    ///// <returns></returns>
    //[Obsolete("index参数晦涩难懂")]
    //public static T[] ToArray<T>(this ICollection<T> collection, Int32 index)
    //{
    //    if (collection == null) return null;

    //    lock (collection)
    //    {
    //        var count = collection.Count;
    //        if (count == 0) return Array.Empty<T>();

    //        var arr = new T[count + index];
    //        collection.CopyTo(arr, index);

    //        return arr;
    //    }
    //}

    /// <summary>集合转为数组，加锁确保安全</summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="collection">集合</param>
    /// <returns>数组副本，永不返回 null</returns>
    public static T[] ToArray<T>(this ICollection<T> collection)
    {
        //if (collection == null) return null;

        lock (collection)
        {
            var count = collection.Count;
            if (count == 0) return [];

            var arr = new T[count];
            collection.CopyTo(arr, 0);

            return arr;
        }
    }

    /// <summary>
    /// 获取字典键数组（快照）。如果是 <see cref="ConcurrentDictionary{TKey, TValue}"/> ，直接返回其内部 Keys 列表以避免复制。
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="collection">字典实例</param>
    /// <returns>键数组（IList 视图）。</returns>
    public static IList<TKey> ToKeyArray<TKey, TValue>(this IDictionary<TKey, TValue> collection) where TKey : notnull
    {
        //if (collection == null) return null;

        // 当是并发字典时，直接复用其 Keys 集合（避免复制开销）
        if (collection is ConcurrentDictionary<TKey, TValue> cdiv && cdiv.Keys is IList<TKey> list) return list;

        if (collection.Count == 0) return [];
        lock (collection)
        {
            var arr = new TKey[collection.Count];
            collection.Keys.CopyTo(arr, 0);
            return arr;
        }
    }

    /// <summary>
    /// 获取字典值数组（快照）。如果是 <see cref="ConcurrentDictionary{TKey, TValue}"/> ，直接返回其内部 Values 列表以避免复制。
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="collection">字典实例</param>
    /// <returns>值数组（IList 视图）。</returns>
    public static IList<TValue> ToValueArray<TKey, TValue>(this IDictionary<TKey, TValue> collection) where TKey : notnull
    {
        //if (collection == null) return null;

        //if (collection is ConcurrentDictionary<TKey, TValue> cdiv) return cdiv.Values as IList<TValue>;
        if (collection is ConcurrentDictionary<TKey, TValue> cdiv && cdiv.Values is IList<TValue> list) return list;

        if (collection.Count == 0) return [];
        lock (collection)
        {
            var arr = new TValue[collection.Count];
            collection.Values.CopyTo(arr, 0);
            return arr;
        }
    }

    /// <summary>目标匿名参数对象转为名值字典</summary>
    /// <param name="source">匿名对象 / POCO / 字典 / JsonElement</param>
    /// <returns>大小写不敏感的名值字典（永不为 null）</returns>
    public static IDictionary<String, Object?> ToDictionary(this Object source)
    {
        //!! 即使传入为空，也返回字典，而不是null，避免业务层需要大量判空
        //if (target == null) return null;
        if (source is IDictionary<String, Object?> dic) return dic;
        var type = source?.GetType();
        if (type != null && type.IsBaseType())
            throw new InvalidDataException("source is not Object");

        dic = new NullableDictionary<String, Object?>(StringComparer.OrdinalIgnoreCase);
        if (source != null)
        {
            // 修正字符串字典的支持问题
            if (source is IDictionary dic2)
            {
                foreach (var item in dic2)
                {
                    if (item is DictionaryEntry de)
                        dic[de.Key + ""] = de.Value;
                }
            }
#if NETCOREAPP
            else if (source is JsonElement element && element.ValueKind == JsonValueKind.Object)
            {
                foreach (var item in element.EnumerateObject())
                {
                    Object? v = item.Value.ValueKind switch
                    {
                        JsonValueKind.Object => item.Value.ToDictionary(),
                        JsonValueKind.Array => ToArray(item.Value),
                        JsonValueKind.String => item.Value.GetString(),
                        JsonValueKind.Number when item.Value.GetRawText().Contains('.') => item.Value.GetDouble(),
                        JsonValueKind.Number => item.Value.GetInt64(),
                        JsonValueKind.True or JsonValueKind.False => item.Value.GetBoolean(),
                        _ => item.Value.GetString(),
                    };
                    // 将 Int64 收缩到 Int32 范围（若可能），避免不必要的 64 位数值
                    if (v is Int64 n && n >= Int32.MinValue && n <= Int32.MaxValue) v = (Int32)n;
                    dic[item.Name] = v;
                }
            }
#endif
            else
            {
                foreach (var pi in source.GetType().GetProperties(true))
                {
                    var name = SerialHelper.GetName(pi);
                    if (source is IModel src)
                        dic[name] = src[name];
                    else
                        dic[name] = source.GetValue(pi);
                }

                // 增加扩展属性
                if (source is IExtend ext && ext.Items != null)
                {
                    foreach (var item in ext.Items)
                    {
                        dic[item.Key] = item.Value;
                    }
                }
            }
        }

        return dic;
    }

#if NETCOREAPP
    /// <summary>Json对象转为数组</summary>
    /// <param name="element">Json元素（数组）</param>
    /// <returns>列表</returns>
    public static IList<Object?> ToArray(this JsonElement element)
    {
        var list = new List<Object?>();
        foreach (var item in element.EnumerateArray())
        {
            Object? v = item.ValueKind switch
            {
                JsonValueKind.Object => item.ToDictionary(),
                JsonValueKind.Array => ToArray(item),
                JsonValueKind.String => item.GetString(),
                JsonValueKind.Number when item.GetRawText().Contains('.') => item.GetDouble(),
                JsonValueKind.Number => item.GetInt64(),
                JsonValueKind.True or JsonValueKind.False => item.GetBoolean(),
                _ => item.GetString(),
            };
            // 将 Int64 收缩到 Int32 范围（若可能）
            if (v is Int64 n && n >= Int32.MinValue && n <= Int32.MaxValue) v = (Int32)n;
            list.Add(v);
        }

        return list;
    }
#endif

    /// <summary>合并字典参数</summary>
    /// <param name="dic">基础字典（被写入）</param>
    /// <param name="target">待合并对象（匿名 / 字典 / JsonElement / POCO）</param>
    /// <param name="overwrite">同名键是否覆盖</param>
    /// <param name="excludes">排除键集合（大小写不敏感）</param>
    /// <returns>合并后的同一 <paramref name="dic"/> 引用</returns>
    public static IDictionary<String, Object?> Merge(this IDictionary<String, Object?> dic, Object target, Boolean overwrite = true, String[]? excludes = null)
    {
        if (target == null || target.GetType().IsBaseType()) return dic;

        var exs = excludes != null ? new HashSet<String>(excludes, StringComparer.OrdinalIgnoreCase) : null;
        foreach (var item in target.ToDictionary())
        {
            if (exs == null || !exs.Contains(item.Key))
            {
                if (overwrite || !dic.ContainsKey(item.Key)) dic[item.Key] = item.Value;
            }
        }

        return dic;
    }

    /// <summary>转为可空字典</summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="collection">源字典</param>
    /// <param name="comparer">可选比较器</param>
    /// <returns>可空字典（如果本身已是则直接返回）</returns>
    public static IDictionary<TKey, TValue> ToNullable<TKey, TValue>(this IDictionary<TKey, TValue> collection, IEqualityComparer<TKey>? comparer = null) where TKey : notnull
    {
        //if (collection == null) return null;

        if (collection is NullableDictionary<TKey, TValue> dic && (comparer == null || dic.Comparer == comparer)) return dic;

        if (comparer == null)
            return new NullableDictionary<TKey, TValue>(collection);
        else
            return new NullableDictionary<TKey, TValue>(collection, comparer);
    }

    /// <summary>从队列里面获取指定个数元素并消费（Dequeue）</summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="collection">队列</param>
    /// <param name="count">最大获取数量</param>
    /// <returns>枚举序列</returns>
    public static IEnumerable<T> Take<T>(this Queue<T> collection, Int32 count)
    {
        if (collection == null) yield break;

        while (count-- > 0 && collection.Count > 0)
        {
            yield return collection.Dequeue();
        }
    }

    /// <summary>从并发生产消费集合里获取指定个数元素并消费（TryTake）</summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="collection">生产消费集合</param>
    /// <param name="count">最大获取数量</param>
    /// <returns>枚举序列</returns>
    public static IEnumerable<T> Take<T>(this IProducerConsumerCollection<T> collection, Int32 count)
    {
        if (collection == null) yield break;

        while (count-- > 0 && collection.TryTake(out var item))
        {
            yield return item;
        }
    }
}