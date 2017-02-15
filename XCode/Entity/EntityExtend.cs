using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife.Collections;

namespace XCode
{
    /// <summary>实体扩展</summary>
    public class EntityExtend : DictionaryCache<String, Object>
    {
        /// <summary>实例化一个不区分键大小写的实体扩展</summary>
        public EntityExtend() : base(StringComparer.OrdinalIgnoreCase)
        {
            Asynchronous = true;
            // 扩展属性默认10秒过期，然后异步更新
            Expire = Setting.Current.ExtendExpire;
        }

        /// <summary>扩展获取数据项，当数据项不存在时，通过调用委托获取数据项。线程安全。</summary>
        /// <param name="key">键</param>
        /// <param name="func">获取值的委托，该委托以键作为参数</param>
        /// <returns></returns>
        public virtual T Get<T>(String key, Func<String, T> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));

            return (T)GetItem(key, k => func(k));
        }

        /// <summary>设置扩展属性项</summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual Boolean Set(String key, Object value)
        {
            if (value == null) return Remove(key);

            this[key] = value;

            return true;
        }
    }
}