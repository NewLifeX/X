using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NewLife.Reflection;

namespace NewLife.Data
{
    /// <summary>具有扩展数据的接口</summary>
    public interface IExtend
    {
        ///// <summary>数据项</summary>
        //IDictionary<String, Object> Items { get; }

        /// <summary>设置 或 获取 数据项</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Object this[String key] { get; set; }
    }

    /// <summary>扩展数据助手</summary>
    public static class ExtendHelper
    {
        /// <summary>名值字典转扩展接口</summary>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        public static IExtend ToExtend(this IDictionary<String, Object> dictionary)
        {
            if (dictionary is IExtend ext) return ext;

            return new ExtendDictionary { Items = dictionary };
        }

        /// <summary>扩展接口转名值字典</summary>
        /// <param name="extend"></param>
        /// <param name="throwOnError">出错时是否抛出异常</param>
        /// <returns></returns>
        public static IDictionary<String, Object> ToDictionary(this IExtend extend, Boolean throwOnError = true)
        {
            // 泛型字典
            if (extend is IDictionary<String, Object> dictionary) return dictionary;
            if (extend is ExtendDictionary edic) return edic.Items;

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
            var pi = extend.GetType().GetProperty("Items", BindingFlags.Instance | BindingFlags.Public /*| BindingFlags.NonPublic*/);
            if (pi != null && pi.PropertyType.As<IDictionary<String, Object>>()) return pi.GetValue(extend, null) as IDictionary<String, Object>;

            //var fi = extend.GetType().GetField("Items", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            //if (fi != null && fi.FieldType.As<IDictionary<String, Object>>()) return fi.GetValue(extend) as IDictionary<String, Object>;

            if (throwOnError) throw new NotSupportedException($"不支持从类型[{extend.GetType().FullName}]中获取字典！");

            return null;
        }
    }

    /// <summary>扩展字典。引用型</summary>
    public class ExtendDictionary : IExtend
    {
        /// <summary>数据项</summary>
        public IDictionary<String, Object> Items { get; set; }

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
}