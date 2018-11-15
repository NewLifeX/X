using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NewLife.Log;
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

        /// <summary>容量。容量超标时，采用LRU机制删除，默认100_000</summary>
        public Int32 Capacity { get; set; } = 100_000;

        /// <summary>定时清理时间，默认60秒</summary>
        public Int32 Period { get; set; } = 60;
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

        #region 缓存属性
        private Int32 _count;
        /// <summary>缓存项。原子计数</summary>
        public override Int32 Count => _count;

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
                var period = Period;
                clearTimer = new TimerX(RemoveNotAlive, null, 10 * 1000, period * 1000)
                {
                    Async = true,
                    CanExecute = () => _cache.Any(),
                };
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
                if (_cache.TryGetValue(key, out var item)) return (T)item.Visit();

                if (ci == null) ci = new CacheItem(value, expire);
            } while (!_cache.TryAdd(key, ci));

            Interlocked.Increment(ref _count);

            return (T)ci.Visit();
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
                    item.Set(value, expire);
                    return true;
                }

                if (ci == null) ci = new CacheItem(value, expire);
            } while (!_cache.TryAdd(key, ci));

            Interlocked.Increment(ref _count);

            return true;
        }

        /// <summary>获取缓存项，不存在时返回默认值</summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public override T Get<T>(String key)
        {
            if (!_cache.TryGetValue(key, out var item) || item == null) return default(T);

            return (T)item.Visit();
        }

        /// <summary>批量移除缓存项</summary>
        /// <param name="keys">键集合</param>
        /// <returns>实际移除个数</returns>
        public override Int32 Remove(params String[] keys)
        {
            var count = 0;
            foreach (var k in keys)
            {
                if (_cache.TryRemove(k, out var item))
                {
                    count++;

                    Interlocked.Decrement(ref _count);
                }
            }
            return count;
        }

        /// <summary>清空所有缓存项</summary>
        public override void Clear() => _cache.Clear();

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

            Interlocked.Increment(ref _count);

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
                    item.Set(value, expire);
                    return (T)rs;
                }

                if (ci == null) ci = new CacheItem(value, expire);
            } while (!_cache.TryAdd(key, ci));

            Interlocked.Increment(ref _count);

            return default(T);
        }

        /// <summary>累加，原子操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        public override Int64 Increment(String key, Int64 value)
        {
            var item = GetOrAddItem(key, k => 0L);
            return (Int64)item.Inc(value);
        }

        /// <summary>累加，原子操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        public override Double Increment(String key, Double value)
        {
            var item = GetOrAddItem(key, k => 0d);
            return (Double)item.Inc(value);
        }

        /// <summary>递减，原子操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        public override Int64 Decrement(String key, Int64 value)
        {
            var item = GetOrAddItem(key, k => 0L);
            return (Int64)item.Dec(value);
        }

        /// <summary>递减，原子操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        public override Double Decrement(String key, Double value)
        {
            var item = GetOrAddItem(key, k => 0d);
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
            var item = GetOrAddItem(key, k => new List<T>());
            return item.Visit() as IList<T>;
        }

        /// <summary>获取哈希</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public override IDictionary<String, T> GetDictionary<T>(String key)
        {
            var item = GetOrAddItem(key, k => new ConcurrentDictionary<String, T>());
            return item.Visit() as IDictionary<String, T>;
        }

        /// <summary>获取队列</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public override IProducerConsumer<T> GetQueue<T>(String key)
        {
            var item = GetOrAddItem(key, k => new MemoryQueue<T>());
            return item.Visit() as IProducerConsumer<T>;
        }

        /// <summary>获取Set</summary>
        /// <remarks>基于HashSet，非线程安全</remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public override ICollection<T> GetSet<T>(String key)
        {
            var item = GetOrAddItem(key, k => new HashSet<T>());
            return item.Visit() as ICollection<T>;
        }

        /// <summary>获取 或 添加 缓存项</summary>
        /// <param name="key"></param>
        /// <param name="valueFactory"></param>
        /// <returns></returns>
        protected CacheItem GetOrAddItem(String key, Func<String, Object> valueFactory)
        {
            var expire = Expire;

            CacheItem ci = null;
            do
            {
                if (_cache.TryGetValue(key, out var item)) return item;

                if (ci == null) ci = new CacheItem(valueFactory(key), expire);
            } while (!_cache.TryAdd(key, ci));

            Interlocked.Increment(ref _count);

            return ci;
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

            /// <summary>访问时间</summary>
            public DateTime VisitTime { get; private set; }

            /// <summary>构造缓存项</summary>
            /// <param name="value"></param>
            /// <param name="expire"></param>
            public CacheItem(Object value, Int32 expire) => Set(value, expire);

            /// <summary>设置数值和过期时间</summary>
            /// <param name="value"></param>
            /// <param name="expire"></param>
            public void Set(Object value, Int32 expire)
            {
                Value = value;

                var now = VisitTime = TimerX.Now;
                if (expire <= 0)
                    ExpiredTime = DateTime.MaxValue;
                else
                    ExpiredTime = now.AddSeconds(expire);
            }

            /// <summary>更新访问时间并返回数值</summary>
            /// <returns></returns>
            public Object Visit()
            {
                VisitTime = TimerX.Now;
                return Value;
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

                Visit();

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

                Visit();

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
            var tx = TimerX.Current;
            if (tx != null && tx.Period == 60_000) tx.Period = Period * 1000;

            var dic = _cache;
            if (_count == 0 && !dic.Any()) return;

            // 过期时间升序，用于缓存满以后删除
            var slist = new SortedList<DateTime, IList<String>>();
            // 超出个数
            if (Capacity <= 0 || _count <= Capacity) slist = null;

            // 60分钟之内过期的数据，进入LRU淘汰
            var now = TimerX.Now;
            var exp = now.AddSeconds(3600);
            var k = 0;

            // 这里先计算，性能很重要
            var list = new List<String>();
            foreach (var item in dic)
            {
                var ci = item.Value;
                if (ci.ExpiredTime <= now)
                    list.Add(item.Key);
                else
                {
                    k++;
                    if (slist != null && ci.ExpiredTime < exp)
                    {
                        if (!slist.TryGetValue(ci.VisitTime, out var ss))
                            slist.Add(ci.VisitTime, ss = new List<String>());

                        ss.Add(item.Key);
                    }
                }
            }

            // 如果满了，删除前面
            if (slist != null && slist.Count > 0 && _count - list.Count > Capacity)
            {
                var over = _count - list.Count - Capacity;
                for (var i = 0; i < slist.Count && over > 0; i++)
                {
                    var ss = slist.Values[i];
                    if (ss != null && ss.Count > 0)
                    {
                        foreach (var item in ss)
                        {
                            if (over <= 0) break;

                            list.Add(item);
                            over--;
                            k--;
                        }
                    }
                }

                XTrace.WriteLine("[{0}]满，{1:n0}>{2:n0}，删除[{3:n0}]个", Name, _count, Capacity, list.Count);
            }

            foreach (var item in list)
            {
                _cache.Remove(item);
            }

            // 修正
            _count = k;
        }
        #endregion

        #region 性能测试
        /// <summary>使用指定线程测试指定次数</summary>
        /// <param name="times">次数</param>
        /// <param name="threads">线程</param>
        /// <param name="rand">随机读写</param>
        /// <param name="batch">批量操作</param>
        public override void BenchOne(Int64 times, Int32 threads, Boolean rand, Int32 batch)
        {
            if (rand)
                times *= 100;
            else
                times *= 1000;

            base.BenchOne(times, threads, rand, batch);
        }
        #endregion
    }

    /// <summary>生产者消费者</summary>
    /// <typeparam name="T"></typeparam>
    public class MemoryQueue<T> : IProducerConsumer<T>
    {
        private IProducerConsumerCollection<T> _Collection;

        /// <summary>实例化内存队列</summary>
        public MemoryQueue() => _Collection = new ConcurrentQueue<T>();

        /// <summary>实例化内存队列</summary>
        /// <param name="collection"></param>
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