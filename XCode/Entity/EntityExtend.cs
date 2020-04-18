using System;
using System.Collections.Generic;
using NewLife.Reflection;
using NewLife.Threading;

namespace XCode
{
    /// <summary>实体扩展</summary>
    public class EntityExtend
    {
        /// <summary>过期时间。单位是秒</summary>
        public Int32 Expire { get; set; }

        private Dictionary<String, CacheItem> _cache;

        /// <summary>实例化一个不区分键大小写的实体扩展</summary>
        public EntityExtend()
        {
            // 扩展属性默认10秒过期，然后异步更新
            Expire = Setting.Current.ExtendExpire;
        }

        /// <summary>扩展获取数据项，当数据项不存在时，通过调用委托获取数据项。线程安全。</summary>
        /// <param name="key">键</param>
        /// <param name="func">获取值的委托，该委托以键作为参数</param>
        /// <returns></returns>
        public virtual T Get<T>(String key, Func<String, T> func = null)
        {
            //if (func == null) throw new ArgumentNullException(nameof(func));
            if (key == null) return default;

            // 不能使用并行字段，那会造成内存暴涨，因为大多数实体对象没有或者只有很少扩展数据
            var dic = _cache;
            if (dic == null)
            {
                if (func == null) return default;

                dic = _cache = new Dictionary<String, CacheItem>(StringComparer.OrdinalIgnoreCase);
            }

            CacheItem ci;
            try
            {
                // 比较小几率出现多线程问题
                if (dic.TryGetValue(key, out ci) && (func == null || !ci.Expired)) return ci.Value.ChangeType<T>();
            }
            catch { }

            lock (dic)
            {
                // 只有指定func时才使用过期
                if (dic.TryGetValue(key, out ci) && (func == null || !ci.Expired)) return ci.Value.ChangeType<T>();

                if (func == null) return default;

                var value = func(key);

                if (!Equals(value, default(T))) dic[key] = new CacheItem(value, Expire);

                return value;
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
            public Boolean Expired => ExpiredTime <= TimerX.Now;

            public CacheItem(Object value, Int32 seconds)
            {
                Value = value;
                if (seconds > 0) ExpiredTime = TimerX.Now.AddSeconds(seconds);
            }
        }
        #endregion
    }
}