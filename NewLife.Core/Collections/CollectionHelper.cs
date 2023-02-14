using System.Collections.Concurrent;
using NewLife.Collections;
using NewLife.Reflection;
using NewLife.Serialization;
#if NETCOREAPP
using System.Text.Json;
#endif

namespace System.Collections.Generic
{
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
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static T[] ToArray<T>(this ICollection<T> collection)
        {
            if (collection == null) return null;

            lock (collection)
            {
                var count = collection.Count;
                if (count == 0) return new T[0];

                var arr = new T[count];
                collection.CopyTo(arr, 0);

                return arr;
            }
        }

        /// <summary>集合转为数组</summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="collection"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static IList<TKey> ToKeyArray<TKey, TValue>(this IDictionary<TKey, TValue> collection, Int32 index = 0)
        {
            if (collection == null) return null;

            if (collection is ConcurrentDictionary<TKey, TValue> cdiv) return cdiv.Keys as IList<TKey>;

            if (collection.Count == 0) return new TKey[0];
            lock (collection)
            {
                var arr = new TKey[collection.Count - index];
                collection.Keys.CopyTo(arr, index);
                return arr;
            }
        }

        /// <summary>集合转为数组</summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="collection"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static IList<TValue> ToValueArray<TKey, TValue>(this IDictionary<TKey, TValue> collection, Int32 index = 0)
        {
            if (collection == null) return null;

            if (collection is ConcurrentDictionary<TKey, TValue> cdiv) return cdiv.Values as IList<TValue>;

            if (collection.Count == 0) return new TValue[0];
            lock (collection)
            {
                var arr = new TValue[collection.Count - index];
                collection.Values.CopyTo(arr, index);
                return arr;
            }
        }

        /// <summary>目标匿名参数对象转为字典</summary>
        /// <remarks>
        /// 需要注意，还有一个 IExtend.ToDictionary() 扩展
        /// </remarks>
        /// <param name="target"></param>
        /// <returns></returns>
        public static IDictionary<String, Object> ToDictionary(this Object target)
        {
            //!! 即使传入为空，也返回字典，而不是null，避免业务层需要大量判空
            //if (target == null) return null;
            if (target is IDictionary<String, Object> dic) return dic;

            dic = new NullableDictionary<String, Object>(StringComparer.OrdinalIgnoreCase);
            if (target != null)
            {
                // 修正字符串字典的支持问题
                if (target is IDictionary dic2)
                {
                    foreach (DictionaryEntry item in dic2)
                    {
                        dic[item.Key + ""] = item.Value;
                    }
                }
#if NETCOREAPP
                else if (target is JsonElement element && element.ValueKind == JsonValueKind.Object)
                {
                    foreach (var item in element.EnumerateObject())
                    {
                        Object v = item.Value.ValueKind switch
                        {
                            JsonValueKind.Object => item.Value.ToDictionary(),
                            JsonValueKind.Array => ToArray(item.Value),
                            JsonValueKind.String => item.Value.GetString(),
                            JsonValueKind.Number when item.Value.GetRawText().Contains('.') => item.Value.GetDouble(),
                            JsonValueKind.Number => item.Value.GetInt64(),
                            JsonValueKind.True or JsonValueKind.False => item.Value.GetBoolean(),
                            _ => item.Value.GetString(),
                        };
                        if (v is Int64 n && n < Int32.MaxValue) v = (Int32)n;
                        dic[item.Name] = v;
                    }
                }
#endif
                else
                {
                    foreach (var pi in target.GetType().GetProperties(true))
                    {
                        var name = SerialHelper.GetName(pi);
                        dic[name] = target.GetValue(pi);
                    }
                }
            }

            return dic;
        }

#if NETCOREAPP
        static IList<Object> ToArray(JsonElement element)
        {
            var list = new List<Object>();
            foreach (var item in element.EnumerateArray())
            {
                Object v = item.ValueKind switch
                {
                    JsonValueKind.Object => item.ToDictionary(),
                    JsonValueKind.Array => ToArray(item),
                    JsonValueKind.String => item.GetString(),
                    JsonValueKind.Number when item.GetRawText().Contains('.') => item.GetDouble(),
                    JsonValueKind.Number => item.GetInt64(),
                    JsonValueKind.True or JsonValueKind.False => item.GetBoolean(),
                    _ => item.GetString(),
                };
                if (v is Int64 n && n < Int32.MaxValue) v = (Int32)n;
                list.Add(v);
            }

            return list;
        }
#endif

        /// <summary>合并字典参数</summary>
        /// <param name="dic">字典</param>
        /// <param name="target">目标对象</param>
        /// <param name="overwrite">是否覆盖同名参数</param>
        /// <param name="excludes">排除项</param>
        /// <returns></returns>
        public static IDictionary<String, Object> Merge(this IDictionary<String, Object> dic, Object target, Boolean overwrite = true, String[] excludes = null)
        {
            var exs = excludes != null ? new HashSet<String>(StringComparer.OrdinalIgnoreCase) : null;
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
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="collection"></param>
        /// <param name="comparer"></param>
        /// <returns></returns>
        public static IDictionary<TKey, TValue> ToNullable<TKey, TValue>(this IDictionary<TKey, TValue> collection, IEqualityComparer<TKey> comparer = null)
        {
            if (collection == null) return null;

            if (collection is NullableDictionary<TKey, TValue> dic && (comparer == null || dic.Comparer == comparer)) return dic;

            return new NullableDictionary<TKey, TValue>(collection, comparer);
        }

        /// <summary>从队列里面获取指定个数元素</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">消费集合</param>
        /// <param name="count">元素个数</param>
        /// <returns></returns>
        public static IEnumerable<T> Take<T>(this Queue<T> collection, Int32 count)
        {
            if (collection == null) yield break;

            while (count-- > 0 && collection.Count > 0)
            {
                yield return collection.Dequeue();
            }
        }

        /// <summary>从消费集合里面获取指定个数元素</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">消费集合</param>
        /// <param name="count">元素个数</param>
        /// <returns></returns>
        public static IEnumerable<T> Take<T>(this IProducerConsumerCollection<T> collection, Int32 count)
        {
            if (collection == null) yield break;

            while (count-- > 0 && collection.TryTake(out var item))
            {
                yield return item;
            }
        }
    }
}