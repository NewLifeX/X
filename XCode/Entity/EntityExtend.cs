using System;
using System.Collections.Generic;
using NewLife.Collections;
using NewLife.Log;
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

        /// <summary>重写索引器。取值时如果没有该项则返回默认值；赋值时如果已存在该项则覆盖，否则添加。</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Object this[String key] { get => Get<Object>(key); set => Set(key, value); }

        /// <summary>扩展获取数据项，当数据项不存在时，通过调用委托获取数据项。线程安全。</summary>
        /// <param name="key">键</param>
        /// <param name="func">获取值的委托，该委托以键作为参数</param>
        /// <returns></returns>
        public virtual T Get<T>(String key, Func<String, T> func = null)
        {
            //if (func == null) throw new ArgumentNullException(nameof(func));
            if (key == null) return default(T);

            // 不能使用并行字段，那会造成内存暴涨，因为大多数实体对象没有或者只有很少扩展数据
            var dic = _cache;
            if (dic == null)
            {
                if (func == null) return default(T);

                dic = _cache = new Dictionary<String, CacheItem>(StringComparer.OrdinalIgnoreCase);
            }

            CacheItem ci = null;
            try
            {
                // 比较小几率出现多线程问题
                if (dic.TryGetValue(key, out ci) && (func == null || !ci.Expired)) return (T)ci.Value;
            }
            catch (Exception ex) { XTrace.WriteException(ex); }

            lock (dic)
            {
                // 只有指定func时才使用过期
                if (dic.TryGetValue(key, out ci) && (func == null || !ci.Expired)) return (T)ci.Value;

                if (func == null) return default(T);

                var value = func(key);

                if (!Equals(value, default(T))) dic[key] = new CacheItem(value, Expire);

                return value;
            }
        }

        /// <summary>设置扩展属性项</summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual Boolean Set(String key, Object value)
        {
            var dic = _cache;

            // 删除
            if (value == null)
            {
                if (dic == null) return false;

                lock (dic) { _cache.Remove(key); }

                return true;
            }

            if (dic == null) dic = _cache = new Dictionary<String, CacheItem>(StringComparer.OrdinalIgnoreCase);

            // 只有指定func时才使用过期
            //var exp = Expire;
            var exp = 24 * 3600;

            lock (dic)
            {
                // 不存在则添加
                if (!dic.TryGetValue(key, out var ci))
                {
                    dic[key] = new CacheItem(value, exp);
                }
                // 更新
                else
                {
                    ci.Value = value;
                    ci.ExpiredTime = TimerX.Now.AddSeconds(exp);
                }
            }

            return true;
        }

        /// <summary>是否已存在</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Boolean ContainsKey(String key) => _cache != null && _cache.ContainsKey(key);

        /// <summary>赋值到目标缓存</summary>
        /// <param name="target"></param>
        public void CopyTo(EntityExtend target)
        {
            var dic = _cache;
            if (dic == null || dic.Count == 0) return;

            var arr = dic.ToArray();
            foreach (var item in arr)
            {
                //target[item.Key] = item.Value.Value;
                target.Set(item.Key, item.Value.Value);
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
            public Boolean Expired { get { return ExpiredTime <= TimerX.Now; } }

            public CacheItem(Object value, Int32 seconds)
            {
                Value = value;
                if (seconds > 0) ExpiredTime = TimerX.Now.AddSeconds(seconds);
            }
        }
        #endregion
    }
}