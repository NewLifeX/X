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
    public class EntityExtend : Dictionary<String, Object>, IDictionary<String, Object>
    {
        #region 属性
        /// <summary>过期时间。单位是秒，默认60秒，0表示永不过期</summary>
        public Int32 Expire { get; set; } = 60;

        private Dictionary<String, CacheItem> Items;
        #endregion

        /// <summary>实例化一个不区分键大小写的实体扩展</summary>
        public EntityExtend() : base(StringComparer.OrdinalIgnoreCase) { }

        /// <summary>扩展获取数据项，当数据项不存在时，通过调用委托获取数据项。线程安全。</summary>
        /// <param name="key">键</param>
        /// <param name="func">获取值的委托，该委托以键作为参数</param>
        /// <returns></returns>
        [DebuggerHidden]
        public virtual T Get<T>(String key, Func<String, Object> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));

            var exp = Expire;
            var items = Items;
            CacheItem item;
            if (items.TryGetValue(key, out item) && (exp <= 0 || !item.Expired)) return (T)item.Value;

            // 提前计算，避免因为不同的Key错误锁定了主键
            var value = default(Object);

            lock (items)
            {
                if (items.TryGetValue(key, out item) && (exp <= 0 || !item.Expired)) return (T)item.Value;

                // 首次访问同步计算，以后异步计算
                if (item == null)
                {
                    value = func(key);
                    items[key] = new CacheItem(value, exp);
                }
                else
                {
                    // 马上修改缓存时间，让后面的来访者直接采用已过期的缓存项
                    item.ExpiredTime = DateTime.Now.AddSeconds(exp);
                    value = item.Value;
                    Task.Run(() => { item.Value = func(key); });
                }

                return (T)value;
            }
        }

        #region 缓存项
        /// <summary>缓存项</summary>
        class CacheItem
        {
            /// <summary>数值</summary>
            public Object Value { get; set; }

            /// <summary>过期时间</summary>
            public DateTime ExpiredTime { get; set; }

            /// <summary>是否过期</summary>
            public Boolean Expired { get { return ExpiredTime <= DateTime.Now; } }

            public CacheItem(Object value, Int32 seconds)
            {
                Value = value;
                if (seconds > 0) ExpiredTime = DateTime.Now.AddSeconds(seconds);
            }
        }
        #endregion
    }
}