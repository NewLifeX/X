using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Threading;

namespace NewLife.Collections
{
    /// <summary>字典缓存。当指定键的缓存项不存在时，调用委托获取值，并写入缓存。</summary>
    /// <remarks>常用匿名函数或者Lambda表达式作为委托。</remarks>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    public class DictionaryCache<TKey, TValue> : DisposeBase
    {
        #region 属性
        /// <summary>过期时间。单位是秒，默认0秒，表示永不过期</summary>
        public Int32 Expire { get; set; }

        /// <summary>定时清理时间，默认0秒，表示不清理过期项</summary>
        public Int32 Period { get; set; }

        /// <summary>是否允许缓存控制，避免缓存穿透。默认false</summary>
        public Boolean AllowNull { get; set; }

        /// <summary>查找数据的方法</summary>
        public Func<TKey, TValue> FindMethod { get; set; }

        private ConcurrentDictionary<TKey, CacheItem> _cache;
        #endregion

        #region 构造
        /// <summary>实例化一个字典缓存</summary>
        public DictionaryCache() => _cache = new ConcurrentDictionary<TKey, CacheItem>();

        /// <summary>实例化一个字典缓存</summary>
        /// <param name="comparer"></param>
        public DictionaryCache(IEqualityComparer<TKey> comparer) => _cache = new ConcurrentDictionary<TKey, CacheItem>(comparer);

        /// <summary>实例化一个字典缓存</summary>
        /// <param name="findMethod"></param>
        /// <param name="comparer"></param>
        public DictionaryCache(Func<TKey, TValue> findMethod, IEqualityComparer<TKey> comparer = null)
        {
            FindMethod = findMethod;

            if (comparer != null)
                _cache = new ConcurrentDictionary<TKey, CacheItem>(comparer);
            else
                _cache = new ConcurrentDictionary<TKey, CacheItem>();
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            _count = 0;
            //_cache.Clear();

            StopTimer();
        }
        #endregion

        #region 缓存项
        /// <summary>缓存项</summary>
        class CacheItem
        {
            /// <summary>数值</summary>
            public TValue Value { get; set; }

            /// <summary>过期时间</summary>
            public DateTime ExpiredTime { get; set; }

            /// <summary>是否过期</summary>
            public Boolean Expired => ExpiredTime <= TimerX.Now;

            public CacheItem(TValue value, Int32 seconds)
            {
                Value = value;
                if (seconds > 0) ExpiredTime = TimerX.Now.AddSeconds(seconds);
            }
        }
        #endregion

        #region 核心取值方法
        /// <summary>重写索引器。取值时如果没有该项则返回默认值；赋值时如果已存在该项则覆盖，否则添加。</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TValue this[TKey key] { get => GetOrAdd(key); set => Set(key, value); }

        /// <summary>获取 GetOrAdd</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual TValue GetOrAdd(TKey key)
        {
            var func = FindMethod;

            if (_cache.TryGetValue(key, out var item))
            {
                // 找到后判断过期
                if (Expire > 0 && func != null && item.Expired)
                {
                    // 超时异步更新
                    Task.Factory.StartNew(() =>
                    {
                        item.Value = func(key);
                        item.ExpiredTime = TimerX.Now.AddSeconds(Expire);
                    });
                }

                return item.Value;
            }

            // 找不到，则查找数据并加入缓存
            if (func != null)
            {
                // 查数据，避免缓存穿透
                var value = func(key);
                if (value != null || AllowNull)
                {
                    // 如果没有添加成功，则返回旧值
                    if (!TryAdd(key, value, false, out var rs)) return rs;
                    return value;
                }
            }

            return default(TValue);
        }

        /// <summary>获取 GetOrAdd</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual TValue Get(TKey key)
        {
            if (!_cache.TryGetValue(key, out var item)) return default(TValue);

            return item.Value;
        }

        /// <summary>设置 AddOrUpdate</summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual Boolean Set(TKey key, TValue value)
        {
            // 不用AddOrUpdate，避免匿名委托带来的GC损耗
            return TryAdd(key, value, true, out var rs);
        }

        /// <summary>尝试添加，或返回旧值</summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="updateIfExists"></param>
        /// <param name="resultingValue"></param>
        /// <returns></returns>
        public virtual Boolean TryAdd(TKey key, TValue value, Boolean updateIfExists, out TValue resultingValue)
        {
            // 不用AddOrUpdate，避免匿名委托带来的GC损耗
            CacheItem ci = null;
            do
            {
                if (_cache.TryGetValue(key, out var item))
                {
                    resultingValue = item.Value;
                    if (updateIfExists)
                    {
                        item.Value = value;
                        item.ExpiredTime = TimerX.Now.AddSeconds(Expire);
                    }
                    return false;
                }

                if (ci == null) ci = new CacheItem(value, Expire);
            } while (!_cache.TryAdd(key, ci));

            Interlocked.Increment(ref _count);

            resultingValue = default(TValue);

            StartTimer();

            return true;
        }

        /// <summary>扩展获取数据项，当数据项不存在时，通过调用委托获取数据项。线程安全。</summary>
        /// <param name="key">键</param>
        /// <param name="func">获取值的委托，该委托以键作为参数</param>
        /// <returns></returns>
        [DebuggerHidden]
        public virtual TValue GetItem(TKey key, Func<TKey, TValue> func)
        {
            var exp = Expire;
            var items = _cache;
            if (items.TryGetValue(key, out var item) && (exp <= 0 || !item.Expired)) return item.Value;

            // 提前计算，避免因为不同的Key错误锁定了主键
            var value = default(TValue);

            lock (items)
            {
                if (items.TryGetValue(key, out item) && (exp <= 0 || !item.Expired)) return item.Value;

                if (func != null)
                {
                    value = func(key);
                    if (value != null || AllowNull)
                    {
                        items[key] = new CacheItem(value, exp);

                        Interlocked.Increment(ref _count);
                    }
                }
                StartTimer();

                return value;
            }
        }

        /// <summary>移除指定缓存项</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual Boolean Remove(TKey key)
        {
            if (!_cache.Remove(key)) return false;

            Interlocked.Decrement(ref _count);

            return true;
        }
        #endregion

        #region 辅助
        private Int32 _count;
        /// <summary>缓存项</summary>
        public Int32 Count => _count;

        /// <summary>是否包含指定键</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Boolean ContainsKey(TKey key) => _cache.ContainsKey(key);

        /// <summary>赋值到目标缓存</summary>
        /// <param name="cache"></param>
        public void CopyTo(DictionaryCache<TKey, TValue> cache)
        {
            if (_cache.Count == 0) return;

            foreach (var item in _cache)
            {
                cache[item.Key] = item.Value.Value;
            }
        }
        #endregion

        #region 清理过期缓存
        /// <summary>清理会话计时器</summary>
        private TimerX _timer;

        void StartTimer()
        {
            var period = Period;
            // 缓存数大于0才启动定时器
            if (period <= 0 || _count <= 0) return;

            if (_timer == null)
            {
                lock (this)
                {
                    if (_timer == null) _timer = new TimerX(RemoveNotAlive, null, period * 1000, period * 1000);
                }
            }
        }

        void StopTimer()
        {
            _timer.TryDispose();
            _timer = null;
        }

        /// <summary>移除过期的缓存项</summary>
        void RemoveNotAlive(Object state)
        {
            var dic = _cache;
            if (_count == 0 && !dic.Any())
            {
                // 缓存数小于0时关闭定时器
                StopTimer();
                return;
            }

            // 这里先计算，性能很重要
            var now = TimerX.Now;
            var ds = new List<TKey>();
            var k = 0;
            foreach (var item in dic)
            {
                var t = item.Value.ExpiredTime;
                if (t < now)
                    ds.Add(item.Key);
                else
                    k++;
            }

            foreach (var item in ds)
            {
                dic.Remove(item);
                //if (dic.Remove(item)) Interlocked.Decrement(ref _count);
            }

            // 修正
            _count = k;
        }
        #endregion
    }
}