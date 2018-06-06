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
        /// <summary>缓存核心</summary>
        protected ConcurrentDictionary<String, CacheItem> _cache;
        #endregion

        #region 构造
        /// <summary>实例化一个内存字典缓存</summary>
        public MemoryCache()
        {
            _cache = new ConcurrentDictionary<String, CacheItem>();
            Name = "Memory";

            Init(null);
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
        /// <summary>缓存个数。高频使用时注意性能</summary>
        public override Int32 Count => _cache.Count;

        /// <summary>所有键。实际返回只读列表新实例，数据量较大时注意性能</summary>
        public override ICollection<String> Keys => _cache.Keys;
        #endregion

        #region 方法
        /// <summary>初始化配置</summary>
        /// <param name="config"></param>
        public override void Init(String config)
        {
            if (clearTimer == null)
            {
                var period = 60;
                clearTimer = new TimerX(RemoveNotAlive, null, period * 1000, period * 1000) { Async = true };
            }
        }

        /// <summary>获取或添加缓存项</summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expire">过期时间，秒。小于0时采用默认缓存时间<seealso cref="Cache.Expire"/></param>
        /// <returns></returns>
        public virtual T GetOrAdd<T>(String key, T value, Int32 expire = -1)
        {
            if (expire < 0) expire = Expire;

            CacheItem ci = null;
            do
            {
                if (_cache.TryGetValue(key, out var item)) return (T)item.Value;

                if (ci == null) ci = new CacheItem(value, expire);
            } while (!_cache.TryAdd(key, ci));

            return (T)ci.Value;
        }
        #endregion

        #region 基本操作
        /// <summary>是否包含缓存项</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override Boolean ContainsKey(String key) => _cache.ContainsKey(key);

        /// <summary>添加缓存项，已存在时更新</summary>
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
                    item.SetExpire(expire);
                    return true;
                }

                if (ci == null) ci = new CacheItem(value, expire);
            } while (!_cache.TryAdd(key, ci));

            return true;
        }

        /// <summary>获取缓存项，不存在时返回默认值</summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public override T Get<T>(String key)
        {
            if (!_cache.TryGetValue(key, out var item) || item == null) return default(T);

            return (T)item.Value;
        }

        /// <summary>批量移除缓存项</summary>
        /// <param name="keys">键集合</param>
        /// <returns>实际移除个数</returns>
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
        /// <returns>设置是否成功</returns>
        public override Boolean SetExpire(String key, TimeSpan expire)
        {
            if (!_cache.TryGetValue(key, out var item) || item == null) return false;

            item.ExpiredTime = TimerX.Now.Add(expire);

            return true;
        }

        /// <summary>获取缓存项有效期，不存在时返回Zero</summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public override TimeSpan GetExpire(String key)
        {
            if (!_cache.TryGetValue(key, out var item) || item == null) return TimeSpan.Zero;

            return item.ExpiredTime - TimerX.Now;
        }
        #endregion

        #region 高级操作
        /// <summary>添加，已存在时不更新，常用于锁争夺</summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expire">过期时间，秒。小于0时采用默认缓存时间<seealso cref="Cache.Expire"/></param>
        /// <returns></returns>
        public override Boolean Add<T>(String key, T value, Int32 expire = -1)
        {
            if (expire < 0) expire = Expire;

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
            var item = _cache.GetOrAdd(key, k => new CacheItem(0L, Expire));
            return (Int64)item.Inc(value);
        }

        /// <summary>累加，原子操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        public override Double Increment(String key, Double value)
        {
            var item = _cache.GetOrAdd(key, k => new CacheItem(0d, Expire));
            return (Double)item.Inc(value);
        }

        /// <summary>递减，原子操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        public override Int64 Decrement(String key, Int64 value)
        {
            var item = _cache.GetOrAdd(key, k => new CacheItem(0L, Expire));
            return (Int64)item.Dec(value);
        }

        /// <summary>递减，原子操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        public override Double Decrement(String key, Double value)
        {
            var item = _cache.GetOrAdd(key, k => new CacheItem(0d, Expire));
            return (Double)item.Dec(value);
        }
        #endregion

        #region 集合操作
        /// <summary>获取列表</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public override IList<T> GetList<T>(String key)
        {
            var item = _cache.GetOrAdd(key, k => new CacheItem(new List<T>(), Expire));
            return item.Value as IList<T>;
        }

        /// <summary>获取哈希</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public override IDictionary<String, T> GetDictionary<T>(String key)
        {
            var item = _cache.GetOrAdd(key, k => new CacheItem(new ConcurrentDictionary<String, T>(), Expire));
            return item.Value as IDictionary<String, T>;
        }

        /// <summary>获取队列</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public override IProducerConsumer<T> GetQueue<T>(String key)
        {
            var item = _cache.GetOrAdd(key, k => new CacheItem(new ConcurrentQueue<T>(), Expire));
            return item.Value as IProducerConsumer<T>;
        }
        #endregion

        #region 缓存项
        /// <summary>缓存项</summary>
        protected class CacheItem
        {
            private Object _Value;
            /// <summary>数值</summary>
            public Object Value { get { return _Value; } set { _Value = value; } }

            /// <summary>过期时间</summary>
            public DateTime ExpiredTime { get; set; }

            /// <summary>是否过期</summary>
            public Boolean Expired => ExpiredTime <= TimerX.Now;

            /// <summary>构造缓存项</summary>
            /// <param name="value"></param>
            /// <param name="expire"></param>
            public CacheItem(Object value, Int32 expire)
            {
                Value = value;
                SetExpire(expire);
            }

            /// <summary>设置过期时间</summary>
            /// <param name="expire"></param>
            public void SetExpire(Int32 expire)
            {
                if (expire <= 0)
                    ExpiredTime = DateTime.MaxValue;
                else
                    ExpiredTime = TimerX.Now.AddSeconds(expire);
            }

            /// <summary>递增</summary>
            /// <param name="value"></param>
            /// <returns></returns>
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
                        case TypeCode.Int64:
                            newValue = (Int64)oldValue + (Int64)value;
                            break;
                        case TypeCode.Single:
                        case TypeCode.Double:
                            newValue = (Double)oldValue + (Double)value;
                            break;
                        default:
                            throw new NotSupportedException("不支持类型[{0}]的递增".F(value.GetType().FullName));
                    }
                } while (Interlocked.CompareExchange(ref _Value, newValue, oldValue) != oldValue);

                return newValue;
            }

            /// <summary>递减</summary>
            /// <param name="value"></param>
            /// <returns></returns>
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
                        case TypeCode.Int64:
                            newValue = (Int64)oldValue - (Int64)value;
                            break;
                        case TypeCode.Single:
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
            var now = TimerX.Now;
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

    /// <summary>生产者消费者</summary>
    /// <typeparam name="T"></typeparam>
    class MemoryQueue<T> : IProducerConsumer<T>
    {
        private IProducerConsumerCollection<T> _Collection;

        public MemoryQueue(IProducerConsumerCollection<T> collection) => _Collection = collection;

        /// <summary>生产添加</summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public Int32 Add(IEnumerable<T> values)
        {
            var count = 0;
            foreach (var item in values)
            {
                if (_Collection.TryAdd(item)) count++;
            }

            return count;
        }

        /// <summary>消费获取</summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public IEnumerable<T> Take(Int32 count = 1)
        {
            if (count <= 0) yield break;

            for (var i = 0; i < count; i++)
            {
                if (!_Collection.TryTake(out var item)) break;

                yield return item;
            }
        }
    }
}