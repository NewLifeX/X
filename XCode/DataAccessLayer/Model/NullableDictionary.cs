using System;
using System.Collections.Generic;
using System.Text;

namespace XCode.DataAccessLayer
{
    /// <summary>可空字典。获取数据时如果指定键不存在可返回空而不是抛出异常</summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    class NullableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IDictionary<TKey, TValue>
    {
        public NullableDictionary() { }
        public NullableDictionary(IEqualityComparer<TKey> comparer) : base(comparer) { }

        /// <summary>获取或设置与指定的属性是否有脏数据。</summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public new TValue this[TKey item]
        {
            get
            {
                TValue v;
                if (TryGetValue(item, out v)) return v;

                return default(TValue);
            }
            set
            {
                base[item] = value;
            }
        }
    }
}