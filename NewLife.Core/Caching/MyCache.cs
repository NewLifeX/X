using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace NewLife.Caching
{
    class MyCache : Cache
    {
        #region 属性
        private MemoryCache _cache;
        #endregion

        #region 构造
        public MyCache()
        {
            _cache = MemoryCache.Default;
            Name = _cache.Name == "Default" ? "Memory" : _cache.Name;

        }
        #endregion

        #region 方法
        /// <summary>是否包含缓存项</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override Boolean Contains(String key) { return _cache.Contains(key); }

        /// <summary>添加缓存项</summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expire">过期时间，秒</param>
        /// <returns></returns>
        public override Boolean Add(String key, Object value, Int32 expire = 0) { return _cache.Add(key, value, expire == 0 ? DateTimeOffset.MaxValue : DateTimeOffset.UtcNow.AddSeconds(expire)); }

        /// <summary>添加获取缓存项</summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expire">过期时间，秒</param>
        /// <returns></returns>
        public override Object AddOrGet(String key, Object value, Int32 expire = 0) { return _cache.AddOrGetExisting(key, value, expire == 0 ? DateTimeOffset.MaxValue : DateTimeOffset.UtcNow.AddSeconds(expire)); }

        /// <summary>获取缓存项</summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public override Object Get(String key) { return _cache.Get(key); }

        /// <summary>移除缓存项</summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public override Object Remove(String key) { return _cache.Remove(key); }
        #endregion
    }
}