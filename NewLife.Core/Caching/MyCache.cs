using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NewLife.Threading;

namespace NewLife.Caching
{
    /// <summary>默认字典缓存</summary>
    class MyCache : Cache
    {
        #region 属性
        private ConcurrentDictionary<String, CacheItem> _cache;
        #endregion

        #region 构造
        public MyCache()
        {
            _cache = new ConcurrentDictionary<String, CacheItem>(StringComparer.OrdinalIgnoreCase);
            Name = "Memory";

            var period = 60;
            clearTimer = new TimerX(RemoveNotAlive, null, period * 1000, period * 1000);
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            clearTimer.TryDispose();
            clearTimer = null;
        }
        #endregion

        #region 属性
        /// <summary>缓存个数</summary>
        public override Int32 Count => _cache.Count;

        /// <summary>所有键</summary>
        public override ICollection<String> Keys => _cache.Keys;
        #endregion

        #region 方法
        /// <summary>获取和设置缓存，永不过期</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override Object this[String key]
        {
            get
            {
                if (!_cache.TryGetValue(key, out var item) || item == null) return null;

                return item.Value;
            }
            set
            {
                Set(key, value);
            }
        }

        /// <summary>是否包含缓存项</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override Boolean ContainsKey(String key) { return _cache.ContainsKey(key); }

        /// <summary>添加缓存项</summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expire">过期时间，秒</param>
        /// <returns></returns>
        public override Boolean Set<T>(String key, T value, TimeSpan expire)
        {
            _cache.AddOrUpdate(key,
                k => new CacheItem(value, expire),
                (k, item) =>
                {
                    item.Value = value;
                    item.ExpiredTime = DateTime.Now.Add(expire);

                    return item;
                });

            return true;
        }

        /// <summary>获取缓存项</summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public override T Get<T>(String key) { return (T)this[key]; }

        /// <summary>移除缓存项</summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public override Boolean Remove(String key) { return _cache.Remove(key); }
        #endregion

        #region 高级操作
        /// <summary>批量获取缓存项</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="keys"></param>
        /// <returns></returns>
        public override IDictionary<String, T> GetAll<T>(params String[] keys)
        {
            var dic = new Dictionary<String, T>();
            foreach (var key in keys)
            {
                if (!_cache.TryGetValue(key, out var item) || item == null) continue;

                dic[key] = (T)item.Value;
            }

            return dic;
        }

        /// <summary>累加，原子操作</summary>
        /// <param name="key"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public override Int32 Increment(String key, Int32 amount)
        {
            if (!_cache.TryGetValue(key, out var item) || item == null) return -1;

            var v = (Int32)item.Value + amount;
            item.Value = v;

            return v;
        }

        /// <summary>递减，原子操作</summary>
        /// <param name="key"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public override Int32 Decrement(String key, Int32 amount)
        {
            if (!_cache.TryGetValue(key, out var item) || item == null) return -1;

            var v = (Int32)item.Value - amount;
            item.Value = v;

            return v;
        }
        #endregion

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

            public CacheItem(Object value, TimeSpan expire)
            {
                Value = value;
                ExpiredTime = DateTime.Now.Add(expire);
            }
        }
        #endregion

        #region 清理过期缓存
        /// <summary>清理会话计时器</summary>
        private TimerX clearTimer;

        /// <summary>移除过期的缓存项</summary>
        void RemoveNotAlive(Object state)
        {
            // 这里先计算，性能很重要
            var now = DateTime.Now;
            foreach (var item in _cache)
            {
                var t = item.Value.ExpiredTime;
                if (t < now) _cache.Remove(item.Key);
            }
        }
        #endregion
    }
}