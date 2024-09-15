using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using NewLife.Data;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Serialization;
using NewLife.Threading;

//#nullable enable
namespace NewLife.Caching;

/// <summary>缓存键事件参数</summary>
public class KeyEventArgs : CancelEventArgs
{
    /// <summary>缓存键</summary>
    public String Key { get; set; } = null!;
}

/// <summary>默认字典缓存</summary>
public class MemoryCache : Cache
{
    #region 属性
    /// <summary>缓存核心</summary>
    protected ConcurrentDictionary<String, CacheItem> _cache = new();

    /// <summary>容量。容量超标时，采用LRU机制删除，默认100_000</summary>
    public Int32 Capacity { get; set; } = 100_000;

    /// <summary>定时清理时间，默认60秒</summary>
    public Int32 Period { get; set; } = 60;

    /// <summary>缓存键过期</summary>
    public event EventHandler<KeyEventArgs>? KeyExpired;
    #endregion

    #region 静态默认实现
    /// <summary>默认缓存</summary>
    public static ICache Instance { get; set; } = new MemoryCache();
    #endregion

    #region 构造
    /// <summary>实例化一个内存字典缓存</summary>
    public MemoryCache()
    {
        Name = GetType().Name.TrimEnd("Cache");

        Init(null);
    }

    /// <summary>销毁</summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        _clearTimer.TryDispose();
        _clearTimer = null;
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
    public override void Init(String? config)
    {
        if (_clearTimer == null)
        {
            var period = Period;
            _clearTimer = new TimerX(RemoveNotAlive, null, 10 * 1000, period * 1000) { Async = true };
        }
    }

    /// <summary>获取或添加缓存项</summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    /// <param name="expire">过期时间，秒</param>
    /// <returns></returns>
    public virtual T? GetOrAdd<T>(String key, T value, Int32 expire = -1)
    {
        if (expire < 0) expire = Expire;

        CacheItem? item = null;
        do
        {
            if (_cache.TryGetValue(key, out item) && item != null)
            {
                if (!item.Expired) return item.Visit<T>();

                item.Set(value, expire);

                return value;
            }

            item ??= new CacheItem(value, expire);
        } while (!_cache.TryAdd(key, item));

        Interlocked.Increment(ref _count);

        return item.Visit<T>();
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
        CacheItem? item = null;
        do
        {
            if (_cache.TryGetValue(key, out item) && item != null)
            {
                item.Set(value, expire);
                return true;
            }

            item ??= new CacheItem(value, expire);
        } while (!_cache.TryAdd(key, item));

        Interlocked.Increment(ref _count);

        return true;
    }

    /// <summary>获取缓存项，不存在时返回默认值</summary>
    /// <param name="key">键</param>
    /// <returns></returns>
    [return: MaybeNull]
    public override T Get<T>(String key)
    {
        if (!_cache.TryGetValue(key, out var item) || item == null || item.Expired) return default;

        return item.Visit<T>();
    }

    /// <summary>移除缓存项</summary>
    /// <param name="key">键</param>
    /// <returns>实际移除个数</returns>
    public override Int32 Remove(String key)
    {
        var count = 0;

        if (_cache.TryRemove(key, out _))
        {
            count++;

            Interlocked.Decrement(ref _count);
        }

        return count;
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

    /// <summary>设置缓存项有效期。已过期但未移除的键会重新激活</summary>
    /// <param name="key">键</param>
    /// <param name="expire">过期时间</param>
    /// <returns>设置是否成功</returns>
    public override Boolean SetExpire(String key, TimeSpan expire)
    {
        if (!_cache.TryGetValue(key, out var item) || item == null) return false;

        item.SetExpire(expire);

        return true;
    }

    /// <summary>获取缓存项有效期，不存在时返回Zero</summary>
    /// <param name="key">键</param>
    /// <returns></returns>
    public override TimeSpan GetExpire(String key)
    {
        if (!_cache.TryGetValue(key, out var item) || item == null) return TimeSpan.Zero;

        return TimeSpan.FromMilliseconds(item.ExpiredTime - Runtime.TickCount64);
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

        CacheItem? item = null;
        do
        {
            if (_cache.TryGetValue(key, out item) && item != null)
            {
                if (!item.Expired) return false;

                item.Set(value, expire);

                return true;
            }

            item ??= new CacheItem(value, expire);
        } while (!_cache.TryAdd(key, item));

        Interlocked.Increment(ref _count);

        return true;
    }

    /// <summary>设置新值并获取旧值，原子操作</summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    /// <returns></returns>
    [return: MaybeNull]
    public override T Replace<T>(String key, T value)
    {
        var expire = Expire;

        CacheItem? item = null;
        do
        {
            if (_cache.TryGetValue(key, out item) && item != null)
            {
                var rs = item.Visit<T>();
                // 如果已经过期，不要返回旧值
                if (item.Expired) rs = default(T);

                item.Set(value, expire);

                return (T?)rs;
            }

            item ??= new CacheItem(value, expire);
        } while (!_cache.TryAdd(key, item));

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
    public override Boolean TryGetValue<T>(String key, [MaybeNullWhen(false)] out T value)
    {
        value = default;

        // 没有值，直接结束
        if (!_cache.TryGetValue(key, out var item) || item == null) return false;

        // 得到已有值
        value = item.Visit<T>();

        // 是否未过期的有效值
        return !item.Expired;
    }

    /// <summary>获取 或 添加 缓存数据，在数据不存在时执行委托请求数据</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="callback"></param>
    /// <param name="expire">过期时间，秒。小于0时采用默认缓存时间<seealso cref="Cache.Expire"/></param>
    /// <returns></returns>
    [return: MaybeNull]
    public override T GetOrAdd<T>(String key, Func<String, T> callback, Int32 expire = -1)
    {
        if (expire < 0) expire = Expire;

        CacheItem? item = null;
        do
        {
            if (_cache.TryGetValue(key, out item) && item != null) return item.Visit<T>();

            item ??= new CacheItem(callback(key), expire);
        } while (!_cache.TryAdd(key, item));

        Interlocked.Increment(ref _count);

        return item.Visit<T>();
    }

    /// <summary>累加，原子操作</summary>
    /// <param name="key">键</param>
    /// <param name="value">变化量</param>
    /// <returns></returns>
    public override Int64 Increment(String key, Int64 value)
    {
        var item = GetOrAddItem(key, k => 0L);
        return item.Inc(value);
    }

    /// <summary>累加，原子操作</summary>
    /// <param name="key">键</param>
    /// <param name="value">变化量</param>
    /// <returns></returns>
    public override Double Increment(String key, Double value)
    {
        var item = GetOrAddItem(key, k => 0d);
        return item.Inc(value);
    }

    /// <summary>递减，原子操作</summary>
    /// <param name="key">键</param>
    /// <param name="value">变化量</param>
    /// <returns></returns>
    public override Int64 Decrement(String key, Int64 value)
    {
        var item = GetOrAddItem(key, k => 0L);
        return item.Dec(value);
    }

    /// <summary>递减，原子操作</summary>
    /// <param name="key">键</param>
    /// <param name="value">变化量</param>
    /// <returns></returns>
    public override Double Decrement(String key, Double value)
    {
        var item = GetOrAddItem(key, k => 0d);
        return item.Dec(value);
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
        return item.Visit<IList<T>>() ??
         throw new InvalidCastException($"Unable to convert the value of [{key}] from {item.TypeCode} to {typeof(IList<T>)}");
    }

    /// <summary>获取哈希</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    public override IDictionary<String, T> GetDictionary<T>(String key)
    {
        var item = GetOrAddItem(key, k => new ConcurrentDictionary<String, T>());
        return item.Visit<IDictionary<String, T>>() ??
         throw new InvalidCastException($"Unable to convert the value of [{key}] from {item.TypeCode} to {typeof(IDictionary<String, T>)}");
    }

    /// <summary>获取队列</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    public override IProducerConsumer<T> GetQueue<T>(String key)
    {
        var item = GetOrAddItem(key, k => new MemoryQueue<T>());
        return item.Visit<IProducerConsumer<T>>() ??
            throw new InvalidCastException($"Unable to convert the value of [{key}] from {item.TypeCode} to {typeof(IProducerConsumer<T>)}");
    }

    /// <summary>获取栈</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    public override IProducerConsumer<T> GetStack<T>(String key)
    {
        var item = GetOrAddItem(key, k => new MemoryQueue<T>(new ConcurrentStack<T>()));
        return item.Visit<IProducerConsumer<T>>() ??
            throw new InvalidCastException($"Unable to convert the value of [{key}] from {item.TypeCode} to {typeof(IProducerConsumer<T>)}");
    }

    /// <summary>获取Set</summary>
    /// <remarks>基于HashSet，非线程安全</remarks>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    public override ICollection<T> GetSet<T>(String key)
    {
        var item = GetOrAddItem(key, k => new HashSet<T>());
        return item.Visit<ICollection<T>>() ??
            throw new InvalidCastException($"Unable to convert the value of [{key}] from {item.TypeCode} to {typeof(ICollection<T>)}");
    }

    /// <summary>获取 或 添加 缓存项</summary>
    /// <param name="key"></param>
    /// <param name="valueFactory"></param>
    /// <returns></returns>
    protected CacheItem GetOrAddItem(String key, Func<String, Object> valueFactory)
    {
        var expire = Expire;

        CacheItem? item = null;
        do
        {
            if (_cache.TryGetValue(key, out item) && item != null)
            {
                if (!item.Expired) return item;

                item.Set(valueFactory(key), expire);

                return item;
            }

            item ??= new CacheItem(valueFactory(key), expire);
        } while (!_cache.TryAdd(key, item));

        Interlocked.Increment(ref _count);

        return item;
    }
    #endregion

    #region 缓存项
    /// <summary>缓存项</summary>
    protected class CacheItem
    {
        /// <summary>数值类型</summary>
        public TypeCode TypeCode { get; set; }

        private Int64 _valueLong;
        private Object? _value;
        /// <summary>数值</summary>
        public Object? Value { get => IsInt() ? _valueLong : _value; }

        /// <summary>过期时间。系统启动以来的毫秒数</summary>
        public Int64 ExpiredTime { get; set; }

        /// <summary>是否过期</summary>
        public Boolean Expired => ExpiredTime <= Runtime.TickCount64;

        /// <summary>访问时间</summary>
        public Int64 VisitTime { get; private set; }

        /// <summary>构造缓存项</summary>
        /// <param name="value"></param>
        /// <param name="expire"></param>
        public CacheItem(Object? value, Int32 expire) => Set(value, expire);

        /// <summary>设置数值和过期时间</summary>
        /// <param name="value"></param>
        /// <param name="expire">过期时间，秒</param>
        public void Set<T>(T value, Int32 expire) => Set(value, TimeSpan.FromSeconds(expire));

        /// <summary>设置数值和过期时间</summary>
        /// <param name="value"></param>
        /// <param name="expire">过期时间，秒</param>
        public void Set<T>(T value, TimeSpan expire)
        {
            var type = typeof(T);
            TypeCode = type.GetTypeCode();

            if (IsInt())
                _valueLong = value.ToLong();
            else
                _value = value;

            SetExpire(expire);
        }

        /// <summary>设置过期时间</summary>
        /// <param name="expire"></param>
        public void SetExpire(TimeSpan expire)
        {
            var now = VisitTime = Runtime.TickCount64;
            if (expire == TimeSpan.Zero)
                ExpiredTime = Int64.MaxValue;
            else
                ExpiredTime = now + (Int64)expire.TotalMilliseconds;
        }

        private Boolean IsInt() => TypeCode >= TypeCode.SByte && TypeCode <= TypeCode.UInt64;
        //private Boolean IsDouble() => TypeCode is TypeCode.Single or TypeCode.Double or TypeCode.Decimal;

        /// <summary>更新访问时间并返回数值</summary>
        /// <returns></returns>
        public T? Visit<T>()
        {
            VisitTime = Runtime.TickCount64;

            if (IsInt())
            {
                // 存入取出相同，大多数时候走这里
                if (_valueLong is T n) return n;

                return _valueLong.ChangeType<T>();
            }
            else
            {
                var rs = _value;
                if (rs == null) return default;

                // 存入取出相同，大多数时候走这里
                if (rs is T t) return t;

                // 复杂类型返回空值，避免ChangeType失败抛出异常
                if (typeof(T).GetTypeCode() == TypeCode.Object) return default;

                return rs.ChangeType<T>();
            }
        }

        /// <summary>递增</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public Int64 Inc(Int64 value)
        {
            // 如果不是整数，先转为整数
            if (!IsInt())
            {
                _valueLong = _value.ToLong();
                TypeCode = TypeCode.Int64;
            }

            // 原子操作
            var newValue = Interlocked.Add(ref _valueLong, value);

            VisitTime = Runtime.TickCount64;

            return newValue;
        }

        /// <summary>递增</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public Double Inc(Double value)
        {
            // 原子操作
            Double newValue;
            Object? oldValue;
            do
            {
                oldValue = _value;
                newValue = (oldValue is Double n ? n : oldValue.ToDouble()) + value;
            } while (Interlocked.CompareExchange(ref _value, newValue, oldValue) != oldValue);

            VisitTime = Runtime.TickCount64;

            return newValue;
        }

        /// <summary>递减</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public Int64 Dec(Int64 value)
        {
            // 如果不是整数，先转为整数
            if (!IsInt())
            {
                _valueLong = _value.ToLong();
                TypeCode = TypeCode.Int64;
            }

            // 原子操作
            var newValue = Interlocked.Add(ref _valueLong, -value);

            VisitTime = Runtime.TickCount64;

            return newValue;
        }

        /// <summary>递减</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public Double Dec(Double value)
        {
            // 原子操作
            Double newValue;
            Object? oldValue;
            do
            {
                oldValue = _value;
                newValue = (oldValue is Double n ? n : oldValue.ToDouble()) - value;
            } while (Interlocked.CompareExchange(ref _value, newValue, oldValue) != oldValue);

            VisitTime = Runtime.TickCount64;

            return newValue;
        }
    }
    #endregion

    #region 清理过期缓存
    /// <summary>清理会话计时器</summary>
    private TimerX? _clearTimer;

    /// <summary>移除过期的缓存项</summary>
    private void RemoveNotAlive(Object? state)
    {
        var tx = _clearTimer;
        if (tx != null /*&& tx.Period == 60_000*/) tx.Period = Period * 1000;

        var dic = _cache;
        if (_count == 0 && !dic.Any()) return;

        // 过期时间升序，用于缓存满以后删除
        var slist = new SortedList<Int64, IList<String>>();
        // 超出个数
        var exceed = true;
        if (Capacity <= 0 || _count <= Capacity) exceed = false;

        // 60分钟之内过期的数据，进入LRU淘汰
        var now = Runtime.TickCount64;
        var exp = now + 3600_000;
        var k = 0;

        // 这里先计算，性能很重要
        var toDels = new List<String>();
        foreach (var item in dic)
        {
            // 已过期，准备删除
            var ci = item.Value;
            if (ci.ExpiredTime <= now)
                toDels.Add(item.Key);
            else
            {
                k++;

                // 超出个数，且1小时内过期的数据，进入LRU淘汰
                if (exceed && ci.ExpiredTime < exp)
                {
                    if (!slist.TryGetValue(ci.VisitTime, out var ss))
                        slist.Add(ci.VisitTime, ss = []);

                    ss.Add(item.Key);
                }
            }
        }

        // 如果满了，删除前面
        if (exceed && slist.Count > 0 && _count - toDels.Count > Capacity)
        {
            // 从lru列表中删除最先将要过期的数据
            var over = _count - toDels.Count - Capacity;
            for (var i = 0; i < slist.Count && over > 0; i++)
            {
                var ss = slist.Values[i];
                if (ss != null && ss.Count > 0)
                {
                    foreach (var item in ss)
                    {
                        if (over <= 0) break;

                        toDels.Add(item);
                        over--;
                        k--;
                    }
                }
            }

            XTrace.WriteLine("[{0}]满，{1:n0}>{2:n0}，删除[{3:n0}]个", Name, _count, Capacity, toDels.Count);
        }

        // 确认删除
        foreach (var item in toDels)
        {
            if (OnExpire(item))
                _cache.Remove(item);
        }

        // 修正
        _count = k;
    }

    /// <summary>缓存过期</summary>
    /// <param name="key"></param>
    protected virtual Boolean OnExpire(String key)
    {
        var e = new KeyEventArgs { Key = key, Cancel = false };
        KeyExpired?.Invoke(this, e);

        return !e.Cancel;
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
            bn.Write((Int32)(ci.ExpiredTime / 1000));

            var value = ci.Value;
            var type = value?.GetType();
            if (type == null)
            {
                bn.Write((Byte)TypeCode.Empty);
            }
            else
            {
                var code = type.GetTypeCode();
                bn.Write((Byte)code);

                if (code != TypeCode.Object)
                    bn.Write(value);
                else
                {
                    bn.Write(type.FullName);
                    if (value != null) bn.Write(Binary.FastWrite(value));
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
        if (ver > _Ver) throw new InvalidDataException($"MemoryCache[ver={_Ver}] Unable to support newer versions [{ver}]");

        var count = bn.ReadSize();
        while (count-- > 0)
        {
            // Key+Expire+Empty
            // Key+Expire+TypeCode+Value
            // Key+Expire+TypeCode+Type+Length+Value
            var key = bn.Read<String>();
            var exp = bn.Read<Int32>();
            var code = (TypeCode)bn.ReadByte();

            Object? value = null;
            if (code == TypeCode.Empty)
            {
            }
            else if (code != TypeCode.Object)
            {
                var type = Type.GetType("System." + code);
                if (type != null) value = bn.Read(type);
            }
            else
            {
                var typeName = bn.Read<String>();
                //var type = Type.GetType(typeName);
                var type = typeName?.GetTypeEx();

                var pk = bn.Read<IPacket>();
                value = pk;
                if (type != null && pk != null)
                {
                    var bn2 = new Binary() { Stream = pk.GetStream(), EncodeInt = true };
                    value = bn2.Read(type);
                }
            }

            if (key != null) Set(key, value, exp - (Int32)(Runtime.TickCount64 / 1000));
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
    /// <summary>获取每个线程测试次数</summary>
    /// <param name="rand"></param>
    /// <param name="batch"></param>
    /// <returns></returns>
    protected override Int32 GetTimesPerThread(Boolean rand, Int32 batch)
    {
        var times = base.GetTimesPerThread(rand, batch);

        if (rand)
            times *= 100;
        else
            times *= 10000;

        return times;
    }
    #endregion
}

/// <summary>生产者消费者</summary>
/// <typeparam name="T"></typeparam>
public class MemoryQueue<T> : DisposeBase, IProducerConsumer<T>
{
    private readonly IProducerConsumerCollection<T> _collection;
    private readonly SemaphoreSlim _occupiedNodes;

    /// <summary>实例化内存队列</summary>
    public MemoryQueue()
    {
        _collection = new ConcurrentQueue<T>();
        _occupiedNodes = new SemaphoreSlim(0);
    }

    /// <summary>实例化内存队列</summary>
    /// <param name="collection"></param>
    public MemoryQueue(IProducerConsumerCollection<T> collection)
    {
        _collection = collection;
        _occupiedNodes = new SemaphoreSlim(collection.Count);
    }

    /// <summary>元素个数</summary>
    public Int32 Count => _collection.Count;

    /// <summary>集合是否为空</summary>
    public Boolean IsEmpty
    {
        get
        {
            if (_collection is ConcurrentQueue<T> queue) return queue.IsEmpty;
            if (_collection is ConcurrentStack<T> stack) return stack.IsEmpty;

            //throw new NotSupportedException();
            return _collection.Count == 0;
        }
    }

    /// <summary>销毁</summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        _occupiedNodes.TryDispose();
    }

    /// <summary>生产添加</summary>
    /// <param name="values"></param>
    /// <returns></returns>
    public Int32 Add(params T[] values)
    {
        var count = 0;
        foreach (var item in values)
        {
            if (_collection.TryAdd(item))
            {
                count++;
                _occupiedNodes.Release();
            }
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
            if (!_occupiedNodes.Wait(0)) break;
            if (!_collection.TryTake(out var item)) break;

            yield return item;
        }
    }

    /// <summary>消费一个</summary>
    /// <param name="timeout">超时。默认0秒，永久等待</param>
    /// <returns></returns>
    public T? TakeOne(Int32 timeout = 0)
    {
        if (!_occupiedNodes.Wait(0))
        {
            if (timeout <= 0 || !_occupiedNodes.Wait(timeout * 1000)) return default;
        }

        return _collection.TryTake(out var item) ? item : default;
    }

    /// <summary>消费获取，异步阻塞</summary>
    /// <param name="timeout">超时。单位秒，0秒表示永久等待</param>
    /// <returns></returns>
    public async Task<T?> TakeOneAsync(Int32 timeout = 0)
    {
        if (!_occupiedNodes.Wait(0))
        {
            if (timeout <= 0) return default;

            if (!await _occupiedNodes.WaitAsync(timeout * 1000)) return default;
        }

        return _collection.TryTake(out var item) ? item : default;
    }

    /// <summary>消费获取，异步阻塞</summary>
    /// <param name="timeout">超时。单位秒，0秒表示永久等待</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task<T?> TakeOneAsync(Int32 timeout, CancellationToken cancellationToken)
    {
        if (!_occupiedNodes.Wait(0, cancellationToken))
        {
            if (timeout <= 0) return default;

            if (!await _occupiedNodes.WaitAsync(timeout * 1000, cancellationToken)) return default;
        }

        return _collection.TryTake(out var item) ? item : default;
    }

    /// <summary>确认消费</summary>
    /// <param name="keys"></param>
    /// <returns></returns>
    public Int32 Acknowledge(params String[] keys) => 0;
}
//#nullable restore