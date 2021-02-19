using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Serialization;
using NewLife.Threading;
#if !NET4
using TaskEx = System.Threading.Tasks.Task;
#endif

//#nullable enable
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

        #region 静态默认实现
        /// <summary>默认缓存</summary>
        public static ICache Instance { get; set; } = new MemoryCache();
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
        protected override void Dispose(Boolean disposing)
        {
            base.Dispose(disposing);

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
        /// <param name="expire">过期时间，秒</param>
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
        public override Boolean ContainsKey(String key) => _cache.TryGetValue(key, out var item) && item != null && !item.Expired;

        /// <summary>添加缓存项，已存在时更新</summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expire">过期时间，秒</param>
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
            if (!_cache.TryGetValue(key, out var item) || item == null || item.Expired) return default;

            return item.Visit().ChangeType<T>();
        }

        /// <summary>批量移除缓存项</summary>
        /// <param name="keys">键集合</param>
        /// <returns>实际移除个数</returns>
        public override Int32 Remove(params String[] keys)
        {
            var count = 0;
            foreach (var k in keys)
            {
                if (_cache.TryRemove(k, out _))
                {
                    count++;

                    Interlocked.Decrement(ref _count);
                }
            }
            return count;
        }

        /// <summary>清空所有缓存项</summary>
        public override void Clear()
        {
            _cache.Clear();
            _count = 0;
        }

        /// <summary>设置缓存项有效期</summary>
        /// <param name="key">键</param>
        /// <param name="expire">过期时间</param>
        /// <returns>设置是否成功</returns>
        public override Boolean SetExpire(String key, TimeSpan expire)
        {
            if (!_cache.TryGetValue(key, out var item) || item == null) return false;

            item.ExpiredTime = DateTime.Now.Add(expire);

            return true;
        }

        /// <summary>获取缓存项有效期，不存在时返回Zero</summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public override TimeSpan GetExpire(String key)
        {
            if (!_cache.TryGetValue(key, out var item) || item == null) return TimeSpan.Zero;

            return item.ExpiredTime - DateTime.Now;
        }
        #endregion

        #region 高级操作
        /// <summary>添加，已存在时不更新，常用于锁争夺</summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expire">过期时间，秒</param>
        /// <returns></returns>
        public override Boolean Add<T>(String key, T value, Int32 expire = -1)
        {
            if (expire < 0) expire = Expire;

            CacheItem ci = null;
            do
            {
                if (_cache.TryGetValue(key, out _)) return false;

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

            return default;
        }

        /// <summary>尝试获取指定键，返回是否包含值。有可能缓存项刚好是默认值，或者只是反序列化失败</summary>
        /// <remarks>
        /// 在 MemoryCache 中，如果某个key过期，在清理之前仍然可以通过TryGet访问，并且更新访问时间，避免被清理。
        /// </remarks>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">值。即使有值也不一定能够返回，可能缓存项刚好是默认值，或者只是反序列化失败</param>
        /// <returns>返回是否包含值，即使反序列化失败</returns>
        public override Boolean TryGetValue<T>(String key, out T value)
        {
            value = default;

            // 没有值，直接结束
            if (!_cache.TryGetValue(key, out var item) || item == null) return false;

            // 得到已有值
            value = item.Visit().ChangeType<T>();

            // 是否未过期的有效值
            return !item.Expired;
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

        /// <summary>获取栈</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public override IProducerConsumer<T> GetStack<T>(String key)
        {
            var item = GetOrAddItem(key, k => new MemoryQueue<T>(new ConcurrentStack<T>()));
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
            public Object Value { get => _Value; set => _Value = value; }

            /// <summary>过期时间</summary>
            public DateTime ExpiredTime { get; set; }

            /// <summary>是否过期</summary>
            public Boolean Expired => ExpiredTime <= DateTime.Now;

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

                var now = VisitTime = DateTime.Now;
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

            /// <summary>递增</summary>
            /// <param name="value"></param>
            /// <returns></returns>
            public Object Inc(Object value)
            {
                var code = value.GetType().GetTypeCode();
                // 原子操作
                Object newValue;
                Object oldValue;
                do
                {
                    oldValue = _Value ?? 0;
                    switch (code)
                    {
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                            newValue = oldValue.ToLong() + value.ToLong();
                            break;
                        case TypeCode.Single:
                        case TypeCode.Double:
                            newValue = oldValue.ToDouble() + value.ToDouble();
                            break;
                        default:
                            throw new NotSupportedException($"不支持类型[{value.GetType().FullName}]的递增");
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
                Object newValue;
                Object oldValue;
                do
                {
                    oldValue = _Value ?? 0;
                    switch (code)
                    {
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                            newValue = oldValue.ToLong() - value.ToLong();
                            break;
                        case TypeCode.Single:
                        case TypeCode.Double:
                            newValue = oldValue.ToDouble() - value.ToDouble();
                            break;
                        default:
                            throw new NotSupportedException($"不支持类型[{value.GetType().FullName}]的递减");
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
            var tx = clearTimer;
            if (tx != null /*&& tx.Period == 60_000*/) tx.Period = Period * 1000;

            var dic = _cache;
            if (_count == 0 && !dic.Any()) return;

            // 过期时间升序，用于缓存满以后删除
            var slist = new SortedList<DateTime, IList<String>>();
            // 超出个数
            var flag = true;
            if (Capacity <= 0 || _count <= Capacity) flag = false;

            // 60分钟之内过期的数据，进入LRU淘汰
            var now = DateTime.Now;
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
                    if (flag && ci.ExpiredTime < exp)
                    {
                        if (!slist.TryGetValue(ci.VisitTime, out var ss))
                            slist.Add(ci.VisitTime, ss = new List<String>());

                        ss.Add(item.Key);
                    }
                }
            }

            // 如果满了，删除前面
            if (flag && slist.Count > 0 && _count - list.Count > Capacity)
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

        #region 持久化
        private const String MAGIC = "NewLifeCache";
        private const Byte _Ver = 1;
        /// <summary>保存到数据流</summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public void Save(Stream stream)
        {
            var bn = new Binary
            {
                Stream = stream,
                EncodeInt = true,
            };

            // 头部，幻数、版本和标记
            bn.Write(MAGIC.GetBytes(), 0, MAGIC.Length);
            bn.Write(_Ver);
            bn.Write(0);

            bn.WriteSize(_cache.Count);
            foreach (var item in _cache)
            {
                var ci = item.Value;

                // Key+Expire+Empty
                // Key+Expire+TypeCode+Value
                // Key+Expire+TypeCode+Type+Length+Value
                bn.Write(item.Key);
                bn.Write(ci.ExpiredTime.ToInt());

                var type = ci.Value?.GetType();
                if (type == null)
                {
                    bn.Write((Byte)TypeCode.Empty);
                }
                else
                {
                    var code = type.GetTypeCode();
                    bn.Write((Byte)code);

                    if (code != TypeCode.Object)
                        bn.Write(ci.Value);
                    else
                    {
                        bn.Write(type.FullName);
                        bn.Write(Binary.FastWrite(ci.Value));
                    }
                }
            }
        }

        /// <summary>从数据流加载</summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public void Load(Stream stream)
        {
            var bn = new Binary
            {
                Stream = stream,
                EncodeInt = true,
            };

            // 头部，幻数、版本和标记
            var magic = bn.ReadBytes(MAGIC.Length).ToStr();
            if (magic != MAGIC) throw new InvalidDataException();

            var ver = bn.Read<Byte>();
            _ = bn.Read<Byte>();

            // 版本兼容
            if (ver > _Ver) throw new InvalidDataException($"MemoryCache[ver={_Ver}]无法支持较新的版本[{ver}]");

            var count = bn.ReadSize();
            while (count-- > 0)
            {
                // Key+Expire+Empty
                // Key+Expire+TypeCode+Value
                // Key+Expire+TypeCode+Type+Length+Value
                var key = bn.Read<String>();
                var exp = bn.Read<Int32>().ToDateTime();
                var code = (TypeCode)bn.ReadByte();

                Object value = null;
                if (code == TypeCode.Empty)
                {
                }
                else if (code != TypeCode.Object)
                {
                    value = bn.Read(Type.GetType("System." + code));
                }
                else
                {
                    var typeName = bn.Read<String>();
                    var type = typeName.GetTypeEx(false);

                    var pk = bn.Read<Packet>();
                    value = pk;
                    if (type != null)
                    {
                        var bn2 = new Binary() { Stream = pk.GetStream(), EncodeInt = true };
                        value = bn2.Read(type);
                    }
                }

                Set(key, value, exp - DateTime.Now);
            }
        }

        /// <summary>保存到文件</summary>
        /// <param name="file"></param>
        /// <param name="compressed"></param>
        /// <returns></returns>
        public Int64 Save(String file, Boolean compressed) => file.AsFile().OpenWrite(compressed, s => Save(s));

        /// <summary>从文件加载</summary>
        /// <param name="file"></param>
        /// <param name="compressed"></param>
        /// <returns></returns>
        public Int64 Load(String file, Boolean compressed) => file.AsFile().OpenRead(compressed, s => Load(s));
        #endregion

        #region 性能测试
        /// <summary>使用指定线程测试指定次数</summary>
        /// <param name="times">次数</param>
        /// <param name="threads">线程</param>
        /// <param name="rand">随机读写</param>
        /// <param name="batch">批量操作</param>
        public override Int64 BenchOne(Int64 times, Int32 threads, Boolean rand, Int32 batch)
        {
            if (rand)
                times *= 100;
            else
                times *= 1000;

            return base.BenchOne(times, threads, rand, batch);
        }
        #endregion
    }

    /// <summary>生产者消费者</summary>
    /// <typeparam name="T"></typeparam>
    public class MemoryQueue<T> : IProducerConsumer<T>
    {
        private readonly IProducerConsumerCollection<T> _Collection;

        /// <summary>实例化内存队列</summary>
        public MemoryQueue() => _Collection = new ConcurrentQueue<T>();

        /// <summary>实例化内存队列</summary>
        /// <param name="collection"></param>
        public MemoryQueue(IProducerConsumerCollection<T> collection) => _Collection = collection;

        /// <summary>元素个数</summary>
        public Int32 Count => _Collection.Count;

        /// <summary>集合是否为空</summary>
        public Boolean IsEmpty
        {
            get
            {
                if (_Collection is ConcurrentQueue<T> queue) return queue.IsEmpty;
                if (_Collection is ConcurrentStack<T> stack) return stack.IsEmpty;

                throw new NotSupportedException();
            }
        }

        /// <summary>生产添加</summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public Int32 Add(params T[] values)
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

        /// <summary>消费一个</summary>
        /// <param name="timeout">超时。默认0秒，永久等待</param>
        /// <returns></returns>
        public T TakeOne(Int32 timeout = 0) => _Collection.TryTake(out var item) ? item : default;

        /// <summary>消费获取</summary>
        /// <param name="timeout">超时。默认0秒，永久等待</param>
        /// <returns></returns>
        public Task<T> TakeOneAsync(Int32 timeout = 0) => TaskEx.FromResult(TakeOne(timeout));

        /// <summary>确认消费</summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public Int32 Acknowledge(params String[] keys) => 0;
    }
}
//#nullable restore