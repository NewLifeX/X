using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NewLife.Reflection;

namespace NewLife.Data
{
    /// <summary>具有可读性的扩展数据</summary>
    public interface IExtend
    {
        /// <summary>设置 或 获取 数据项</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Object this[String key] { get; set; }
    }

    /// <summary>具有扩展数据字典</summary>
    public interface IExtend2 : IExtend
    {
        /// <summary>扩展数据键集合</summary>
        IEnumerable<String> Keys { get; }
    }

    /// <summary>具有扩展数据字典</summary>
    public interface IExtend3 : IExtend
    {
        /// <summary>数据项</summary>
        IDictionary<String, Object> Items { get; }
    }

    /// <summary>扩展数据助手</summary>
    public static class ExtendHelper
    {
        /// <summary>名值字典转扩展接口</summary>
        /// <param name="dictionary">字典</param>
        /// <returns></returns>
        public static IExtend ToExtend(this IDictionary<String, Object> dictionary)
        {
            if (dictionary is IExtend ext) return ext;

            return new ExtendDictionary { Items = dictionary };
        }

        /// <summary>扩展接口转名值字典</summary>
        /// <param name="extend">扩展对象</param>
        /// <returns></returns>
        public static IDictionary<String, Object> ToDictionary(this IExtend extend)
        {
            if (extend == null) return null;

            // 泛型字典
            if (extend is IDictionary<String, Object> dictionary) return dictionary;
            if (extend is ExtendDictionary edic) return edic.Items;
            if (extend is IExtend3 ext3) return ext3.Items;

            // IExtend2
            if (extend is IExtend2 ext2)
            {
                var dic = new Dictionary<String, Object>();
                foreach (var item in ext2.Keys)
                {
                    dic[item] = extend[item];
                }
                return dic;
            }

            // 普通字典
            if (extend is IDictionary dictionary2)
            {
                var dic = new Dictionary<String, Object>();
                foreach (DictionaryEntry item in dictionary2)
                {
                    dic[item.Key + ""] = item.Value;
                }
                return dic;
            }

            // 反射 Items
            var pis = extend.GetType().GetProperties(true);
            var pi = pis.FirstOrDefault(e => e.Name == "Items");
            if (pi != null && pi.PropertyType.As<IDictionary<String, Object>>()) return pi.GetValue(extend, null) as IDictionary<String, Object>;

            // 反射属性
            return new ExtendDictionary2 { Data = extend, Keys = pis.Select(e => e.Name).ToList() };

            //var dic2 = new Dictionary<String, Object>();
            //foreach (var item in pis)
            //{
            //    dic2[item.Name] = extend[item.Name];
            //}

            //return dic2;
        }

        /// <summary>从源对象拷贝数据到目标对象</summary>
        /// <param name="target">目标对象</param>
        /// <param name="source">源对象</param>
        public static void Copy(this IExtend target, IExtend source)
        {
            var dst = target.ToDictionary();
            var src = source.ToDictionary();
            foreach (var item in src)
            {
                if (dst.ContainsKey(item.Key)) dst[item.Key] = item.Value;
            }
        }
    }

    /// <summary>扩展字典。引用型</summary>
    public class ExtendDictionary : IExtend, IExtend2, IExtend3
    {
        /// <summary>数据项</summary>
        public IDictionary<String, Object> Items { get; set; }

        IEnumerable<String> IExtend2.Keys => Items?.Keys;

        /// <summary>获取 或 设置 数据</summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Object this[String item]
        {
            get
            {
                if (Items == null) return null;

                if (Items.TryGetValue(item, out var v)) return v;

                return default;
            }
            set
            {
                if (Items == null) Items = new Dictionary<String, Object>();

                Items[item] = value;
            }
        }
    }

    internal class ExtendDictionary2 : IDictionary<String, Object>
    {
        public IExtend Data { get; set; }

        public ICollection<String> Keys { get; set; }

        public Object this[String key] { get => Data[key]; set => Data[key] = value; }

        public ICollection<Object> Values => Keys.Select(e => Data[e]).ToList();

        public Int32 Count => Keys.Count;

        public Boolean IsReadOnly => false;

        public void Add(String key, Object value) => throw new NotImplementedException();

        public void Add(KeyValuePair<String, Object> item) => throw new NotImplementedException();

        public void Clear() => throw new NotImplementedException();

        public Boolean Contains(KeyValuePair<String, Object> item) => Keys.Contains(item.Key);

        public Boolean ContainsKey(String key) => Keys.Contains(key);

        public void CopyTo(KeyValuePair<String, Object>[] array, Int32 arrayIndex) => throw new NotImplementedException();

        public IEnumerator<KeyValuePair<String, Object>> GetEnumerator()
        {
            foreach (var item in Keys)
            {
                yield return new KeyValuePair<String, Object>(item, Data[item]);
            }
        }

        public Boolean Remove(String key) => throw new NotImplementedException();

        public Boolean Remove(KeyValuePair<String, Object> item) => throw new NotImplementedException();

        public Boolean TryGetValue(String key, out Object value)
        {
            if (Keys.Contains(key))
            {
                value = Data[key];
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}