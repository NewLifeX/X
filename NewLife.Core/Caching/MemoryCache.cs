using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using NewLife.Reflection;
using NewLife.Threading;

namespace NewLife.Caching
{
    /// <summary>默认字典缓存</summary>
    public class MemoryCache : Cache
    {
        #region 属性
        private ConcurrentDictionary<String, CacheItem> _cache;
        #endregion

        #region 构造
        /// <summary>实例化一个内存字典缓存</summary>
        public MemoryCache()
        {
            //_cache = new ConcurrentDictionary<String, CacheItem>(StringComparer.OrdinalIgnoreCase);
            _cache = new ConcurrentDictionary<String, CacheItem>();
            Name = "Memory";
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
        /// <summary>初始化配置</summary>
        /// <param name="set"></param>
        protected override void Init(CacheSetting set)
        {
            if (clearTimer == null)
            {
                var period = 60;
                clearTimer = new TimerX(RemoveNotAlive, null, period * 1000, period * 1000);
            }
        }

        /// <summary>是否包含缓存项</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override Boolean ContainsKey(String key) { return _cache.ContainsKey(key); }

        /// <summary>添加缓存项</summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expire">过期时间，秒。小于0时采用默认缓存时间<seealso cref="Cache.Expire"/></param>
        /// <returns></returns>
        public override Boolean Set<T>(String key, T value, Int32 expire = -1)
        {
            if (expire < 0) expire = Expire;

            //_cache.AddOrUpdate(key,
            //    k => new CacheItem(value, expire),
            //    (k, item) =>
            //    {
            //        item.Value = value;
            //        item.ExpiredTime = DateTime.Now.AddSeconds(expire);

            //        return item;
            //    });

            // 不用AddOrUpdate，避免匿名委托带来的GC损耗
            CacheItem ci = null;
            do
            {
                if (_cache.TryGetValue(key, out var item))
                {
                    item.Value = value;
                    if (expire <= 0)
                        item.ExpiredTime = DateTime.MaxValue;
                    else
                        item.ExpiredTime = TimerX.Now.AddSeconds(expire);
                    //if (_cache.TryUpdate(key, item, item)) return true;
                    return true;
                }

                if (ci == null) ci = new CacheItem(value, expire);
            } while (!_cache.TryAdd(key, ci));

            return true;
        }

        /// <summary>获取缓存项</summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public override T Get<T>(String key)
        {
            if (!_cache.TryGetValue(key, out var item) || item == null) return default(T);

            return (T)item.Value;
        }

        /// <summary>批量移除缓存项</summary>
        /// <param name="keys">键集合</param>
        /// <returns></returns>
        public override Int32 Remove(params String[] keys)
        {
            var count = 0;
            foreach (var k in keys)
            {
                if (_cache.TryRemove(k, out var item)) count++;
            }
            return count;
        }

        /// <summary>设置缓存项有效期</summary>
        /// <param name="key">键</param>
        /// <param name="expire">过期时间</param>
        public override Boolean SetExpire(String key, TimeSpan expire)
        {
            if (!_cache.TryGetValue(key, out var item) || item == null) return false;

            item.ExpiredTime = DateTime.Now.Add(expire);

            return true;
        }

        /// <summary>获取缓存项有效期</summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public override TimeSpan GetExpire(String key)
        {
            if (!_cache.TryGetValue(key, out var item) || item == null) return TimeSpan.Zero;

            return item.ExpiredTime - DateTime.Now;
        }
        #endregion

        #region 高级操作
        /// <summary>添加，已存在时不更新</summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expire">过期时间，秒。小于0时采用默认缓存时间<seealso cref="Cache.Expire"/></param>
        /// <returns></returns>
        public override Boolean Add<T>(String key, T value, Int32 expire = -1)
        {
            CacheItem ci = null;
            do
            {
                if (_cache.TryGetValue(key, out var item)) return false;

                if (ci == null) ci = new CacheItem(value, expire);
            } while (!_cache.TryAdd(key, ci));

            return true;
        }

        /// <summary>设置新值并获取旧值，原子操作</summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public override T Replace<T>(String key, T value)
        {
            var expire = Expire;

            CacheItem ci = null;
            do
            {
                if (_cache.TryGetValue(key, out var item))
                {
                    var rs = item.Value;
                    item.Value = value;
                    return (T)rs;
                }

                if (ci == null) ci = new CacheItem(value, expire);
            } while (!_cache.TryAdd(key, ci));

            return default(T);
        }

        /// <summary>累加，原子操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        public override Int64 Increment(String key, Int64 value)
        {
            if (!_cache.TryGetValue(key, out var item) || item == null)
            {
                item = new CacheItem(0, Expire);
                item = _cache.GetOrAdd(key, item);
            }

            // 原子操作
            return (Int64)item.Inc(value);
        }

        /// <summary>累加，原子操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        public override Double Increment(String key, Double value)
        {
            if (!_cache.TryGetValue(key, out var item) || item == null)
            {
                item = new CacheItem(0, Expire);
                item = _cache.GetOrAdd(key, item);
            }

            // 原子操作
            return (Double)item.Inc(value);
        }

        /// <summary>递减，原子操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        public override Int64 Decrement(String key, Int64 value)
        {
            if (!_cache.TryGetValue(key, out var item) || item == null)
            {
                item = new CacheItem(0, Expire);
                item = _cache.GetOrAdd(key, item);
            }

            // 原子操作
            return (Int64)item.Dec(value);
        }

        /// <summary>递减，原子操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        public override Double Decrement(String key, Double value)
        {
            if (!_cache.TryGetValue(key, out var item) || item == null)
            {
                item = new CacheItem(0, Expire);
                item = _cache.GetOrAdd(key, item);
            }

            // 原子操作
            return (Double)item.Dec(value);
        }
        #endregion

        #region 缓存项
        /// <summary>缓存项</summary>
        class CacheItem
        {
            private Object _Value;
            /// <summary>数值</summary>
            public Object Value { get { return _Value; } set { _Value = value; } }

            /// <summary>过期时间</summary>
            public DateTime ExpiredTime { get; set; }

            /// <summary>是否过期</summary>
            public Boolean Expired { get { return ExpiredTime <= TimerX.Now; } }

            public CacheItem(Object value, Int32 expire)
            {
                Value = value;
                if (expire <= 0)
                    ExpiredTime = DateTime.MaxValue;
                else
                    ExpiredTime = TimerX.Now.AddSeconds(expire);
            }

            public Object Inc(Object value)
            {
                var code = value.GetType().GetTypeCode();
                // 原子操作
                Object newValue = null;
                Object oldValue = null;
                do
                {
                    oldValue = _Value;
                    switch (code)
                    {
                        case TypeCode.Int32:
                            newValue = (Int32)oldValue + (Int32)value;
                            break;
                        case TypeCode.Int64:
                            newValue = (Int64)oldValue + (Int64)value;
                            break;
                        case TypeCode.Single:
                            newValue = (Single)oldValue + (Single)value;
                            break;
                        case TypeCode.Double:
                            newValue = (Double)oldValue + (Double)value;
                            break;
                        default:
                            throw new NotSupportedException("不支持类型[{0}]的递增".F(value.GetType().FullName));
                    }
                } while (Interlocked.CompareExchange(ref _Value, newValue, oldValue) != oldValue);

                //Interlocked.Increment(ref _Value);

                return newValue;
            }

            public Object Dec(Object value)
            {
                var code = value.GetType().GetTypeCode();
                // 原子操作
                Object newValue = null;
                Object oldValue = null;
                do
                {
                    oldValue = _Value;
                    switch (code)
                    {
                        case TypeCode.Int32:
                            newValue = (Int32)oldValue - (Int32)value;
                            break;
                        case TypeCode.Int64:
                            newValue = (Int64)oldValue - (Int64)value;
                            break;
                        case TypeCode.Single:
                            newValue = (Single)oldValue - (Single)value;
                            break;
                        case TypeCode.Double:
                            newValue = (Double)oldValue - (Double)value;
                            break;
                        default:
                            throw new NotSupportedException("不支持类型[{0}]的递减".F(value.GetType().FullName));
                    }
                } while (Interlocked.CompareExchange(ref _Value, newValue, oldValue) != oldValue);

                return newValue;
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
            var list = new List<String>();
            foreach (var item in _cache)
            {
                var t = item.Value.ExpiredTime;
                if (t < now) list.Add(item.Key);
            }

            foreach (var item in list)
            {
                _cache.Remove(item);
            }
        }
        #endregion

        #region 性能测试
        /// <summary>使用指定线程测试指定次数</summary>
        /// <param name="times">次数</param>
        /// <param name="threads">线程</param>
        /// <param name="rand">随机读写</param>
        public override void BenchOne(Int64 times, Int32 threads, Boolean rand)
        {
            if (rand)
                times *= 100;
            else
                times *= 1000;

            base.BenchOne(times, threads, rand);
        }
        #endregion
    }
}