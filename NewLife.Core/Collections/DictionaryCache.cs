using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NewLife.Threading;

namespace NewLife.Collections
{
    /// <summary>字典缓存。当指定键的缓存项不存在时，调用委托获取值，并写入缓存。</summary>
    /// <remarks>常用匿名函数或者Lambda表达式作为委托。</remarks>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    public class DictionaryCache<TKey, TValue> : /*DisposeBase, */IDictionary<TKey, TValue>, IDisposable
    {
        #region 属性
        /// <summary>过期时间。单位是秒，默认0秒，表示永不过期</summary>
        public Int32 Expire { get; set; }

        /// <summary>过期清理时间，缓存项过期后达到这个时间时，将被移除缓存。单位是秒，默认0秒，表示不清理过期项</summary>
        public Int32 ClearPeriod { get; set; }

        /// <summary>异步更新。默认false</summary>
        public Boolean Asynchronous { get; set; }

        /// <summary>移除过期缓存项时，自动调用其Dispose。默认false</summary>
        public Boolean AutoDispose { get; set; }

        /// <summary>是否缓存默认值，有时候委托返回默认值不希望被缓存，而是下一次尽快进行再次计算。默认true</summary>
        public Boolean CacheDefault { get; set; }

        /// <summary>延迟加锁，字典没有数据时，先计算结果再加锁加入字典，避免大量不同键的插入操作形成排队影响性能。默认false</summary>
        public Boolean DelayLock { get; set; }

        private Dictionary<TKey, CacheItem> Items;
        #endregion

        #region 构造
        /// <summary>实例化一个字典缓存</summary>
        public DictionaryCache()
        {
            Items = new Dictionary<TKey, CacheItem>();
            CacheDefault = true;
        }

        /// <summary>实例化一个字典缓存</summary>
        /// <param name="comparer"></param>
        public DictionaryCache(IEqualityComparer<TKey> comparer)
        {
            Items = new Dictionary<TKey, CacheItem>(comparer);
            CacheDefault = true;
        }

        ///// <summary>子类重载实现资源释放逻辑时必须首先调用基类方法</summary>
        ///// <param name="disposing">从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）。
        ///// 因为该方法只会被调用一次，所以该参数的意义不太大。</param>
        //protected override void OnDispose(bool disposing)
        //{
        //    base.OnDispose(disposing);

        //    StopTimer();
        //}

        /// <summary>销毁字典，关闭</summary>
        public void Dispose()
        {
            lock (Items)
            {
                Items.Clear();
            }
            StopTimer();
        }
        #endregion

        #region 缓存项
        /// <summary>缓存项</summary>
        class CacheItem
        {
            /// <summary>数值</summary>
            public TValue Value;

            private DateTime _ExpiredTime;
            /// <summary>过期时间</summary>
            public DateTime ExpiredTime { get { return _ExpiredTime; } set { _ExpiredTime = value; } }

            /// <summary>是否过期</summary>
            public Boolean Expired { get { return ExpiredTime <= DateTime.Now; } }

            public CacheItem(TValue value, Int32 seconds)
            {
                Value = value;
                if (seconds > 0) ExpiredTime = DateTime.Now.AddSeconds(seconds);
            }
        }
        #endregion

        #region 核心取值方法
        /// <summary>重写索引器。取值时如果没有该项则返回默认值；赋值时如果已存在该项则覆盖，否则添加。</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TValue this[TKey key]
        {
            get
            {
                CacheItem item;
                if (Items.TryGetValue(key, out item) && (Expire <= 0 || !item.Expired)) return item.Value;

                return default(TValue);
            }
            set
            {
                CacheItem item;
                if (Items.TryGetValue(key, out item))
                {
                    item.Value = value;
                    //更新当前缓存项的过期时间
                    item.ExpiredTime = DateTime.Now.AddSeconds(Expire);
                }
                else
                {
                    // 加锁，避免意外
                    lock (Items)
                    {
                        Items[key] = new CacheItem(value, Expire);
                    }
                    StartTimer();
                }
            }
        }

        /// <summary>扩展获取数据项，当数据项不存在时，通过调用委托获取数据项。线程安全。</summary>
        /// <param name="key">键</param>
        /// <param name="func">获取值的委托，该委托以键作为参数</param>
        /// <returns></returns>
        [DebuggerHidden]
        public virtual TValue GetItem(TKey key, Func<TKey, TValue> func)
        {
            var exp = Expire;
            var items = Items;
            CacheItem item;
            if (items.TryGetValue(key, out item) && (exp <= 0 || !item.Expired)) return item.Value;

            // 提前计算，避免因为不同的Key错误锁定了主键
            var value = default(TValue);
            // 如果延迟加锁，则提前计算
            if (DelayLock && func != null) value = func(key);

            lock (items)
            {
                if (items.TryGetValue(key, out item) && (exp <= 0 || !item.Expired)) return item.Value;

                // 对于缓存命中，仅是缓存过期的项，如果采用异步，则马上修改缓存时间，让后面的来访者直接采用已过期的缓存项
                if (exp > 0 && Asynchronous)
                {
                    if (item != null) item.ExpiredTime = DateTime.Now.AddSeconds(exp);
                }

                if (func == null)
                {
                    if (CacheDefault) items[key] = new CacheItem(value, exp);
                }
                else
                {
                    // 如果不是延迟加锁，则现在计算
                    if (!DelayLock) value = func(key);

                    if (CacheDefault || !Object.Equals(value, default(TValue))) items[key] = new CacheItem(value, exp);
                }
                StartTimer();

                return value;
            }
        }

        /// <summary>扩展获取数据项，当数据项不存在时，通过调用委托获取数据项。线程安全。</summary>
        /// <typeparam name="TArg">参数类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="arg">参数</param>
        /// <param name="func">获取值的委托，该委托除了键参数外，还有一个泛型参数</param>
        /// <returns></returns>
        [DebuggerHidden]
        public virtual TValue GetItem<TArg>(TKey key, TArg arg, Func<TKey, TArg, TValue> func)
        {
            var exp = Expire;
            var items = Items;
            CacheItem item;
            if (items.TryGetValue(key, out item) && (exp <= 0 || !item.Expired)) return item.Value;
            lock (items)
            {
                if (items.TryGetValue(key, out item) && (exp <= 0 || !item.Expired)) return item.Value;

                // 对于缓存命中，仅是缓存过期的项，如果采用异步，则马上修改缓存时间，让后面的来访者直接采用已过期的缓存项
                if (exp > 0 && Asynchronous)
                {
                    if (item != null) item.ExpiredTime = DateTime.Now.AddSeconds(exp);
                }

                var value = func(key, arg);
                if (CacheDefault || !Object.Equals(value, default(TValue))) items[key] = new CacheItem(value, exp);
                StartTimer();

                return value;
            }
        }

        /// <summary>扩展获取数据项，当数据项不存在时，通过调用委托获取数据项。线程安全。</summary>
        /// <typeparam name="TArg">参数类型</typeparam>
        /// <typeparam name="TArg2">参数类型2</typeparam>
        /// <param name="key">键</param>
        /// <param name="arg">参数</param>
        /// <param name="arg2">参数2</param>
        /// <param name="func">获取值的委托，该委托除了键参数外，还有两个泛型参数</param>
        /// <returns></returns>
        [DebuggerHidden]
        public virtual TValue GetItem<TArg, TArg2>(TKey key, TArg arg, TArg2 arg2, Func<TKey, TArg, TArg2, TValue> func)
        {
            var exp = Expire;
            var items = Items;
            CacheItem item;
            if (items.TryGetValue(key, out item) && (exp <= 0 || !item.Expired)) return item.Value;
            lock (items)
            {
                if (items.TryGetValue(key, out item) && (exp <= 0 || !item.Expired)) return item.Value;

                // 对于缓存命中，仅是缓存过期的项，如果采用异步，则马上修改缓存时间，让后面的来访者直接采用已过期的缓存项
                if (exp > 0 && Asynchronous)
                {
                    if (item != null) item.ExpiredTime = DateTime.Now.AddSeconds(exp);
                }

                var value = func(key, arg, arg2);
                if (CacheDefault || !Object.Equals(value, default(TValue))) items[key] = new CacheItem(value, exp);
                StartTimer();

                return value;
            }
        }

        /// <summary>扩展获取数据项，当数据项不存在时，通过调用委托获取数据项。线程安全。</summary>
        /// <typeparam name="TArg">参数类型</typeparam>
        /// <typeparam name="TArg2">参数类型2</typeparam>
        /// <typeparam name="TArg3">参数类型3</typeparam>
        /// <param name="key">键</param>
        /// <param name="arg">参数</param>
        /// <param name="arg2">参数2</param>
        /// <param name="arg3">参数3</param>
        /// <param name="func">获取值的委托，该委托除了键参数外，还有三个泛型参数</param>
        /// <returns></returns>
        [DebuggerHidden]
        public virtual TValue GetItem<TArg, TArg2, TArg3>(TKey key, TArg arg, TArg2 arg2, TArg3 arg3, Func<TKey, TArg, TArg2, TArg3, TValue> func)
        {
            var exp = Expire;
            var items = Items;
            CacheItem item;
            if (items.TryGetValue(key, out item) && (exp <= 0 || !item.Expired)) return item.Value;
            lock (items)
            {
                if (items.TryGetValue(key, out item) && (exp <= 0 || !item.Expired)) return item.Value;

                // 对于缓存命中，仅是缓存过期的项，如果采用异步，则马上修改缓存时间，让后面的来访者直接采用已过期的缓存项
                if (exp > 0 && Asynchronous)
                {
                    if (item != null) item.ExpiredTime = DateTime.Now.AddSeconds(exp);
                }

                var value = func(key, arg, arg2, arg3);
                if (CacheDefault || !Object.Equals(value, default(TValue))) items[key] = new CacheItem(value, exp);
                StartTimer();

                return value;
            }
        }

        /// <summary>扩展获取数据项，当数据项不存在时，通过调用委托获取数据项。线程安全。</summary>
        /// <typeparam name="TArg">参数类型</typeparam>
        /// <typeparam name="TArg2">参数类型2</typeparam>
        /// <typeparam name="TArg3">参数类型3</typeparam>
        /// <typeparam name="TArg4">参数类型4</typeparam>
        /// <param name="key">键</param>
        /// <param name="arg">参数</param>
        /// <param name="arg2">参数2</param>
        /// <param name="arg3">参数3</param>
        /// <param name="arg4">参数4</param>
        /// <param name="func">获取值的委托，该委托除了键参数外，还有三个泛型参数</param>
        /// <returns></returns>
        [DebuggerHidden]
        public virtual TValue GetItem<TArg, TArg2, TArg3, TArg4>(TKey key, TArg arg, TArg2 arg2, TArg3 arg3, TArg4 arg4, Func<TKey, TArg, TArg2, TArg3, TArg4, TValue> func)
        {
            var exp = Expire;
            var items = Items;
            CacheItem item;
            if (items.TryGetValue(key, out item) && (exp <= 0 || !item.Expired)) return item.Value;
            lock (items)
            {
                if (items.TryGetValue(key, out item) && (exp <= 0 || !item.Expired)) return item.Value;

                // 对于缓存命中，仅是缓存过期的项，如果采用异步，则马上修改缓存时间，让后面的来访者直接采用已过期的缓存项
                if (exp > 0 && Asynchronous)
                {
                    if (item != null) item.ExpiredTime = DateTime.Now.AddSeconds(exp);
                }

                var value = func(key, arg, arg2, arg3, arg4);
                if (CacheDefault || !Object.Equals(value, default(TValue))) items[key] = new CacheItem(value, exp);
                StartTimer();

                return value;
            }
        }
        #endregion

        #region 清理过期缓存
        /// <summary>清理会话计时器</summary>
        private TimerX clearTimer;

        void StartTimer()
        {
            var period = ClearPeriod;
            // 缓存数大于0才启动定时器
            if (period <= 0 || Items.Count < 1) return;

            if (clearTimer == null) clearTimer = new TimerX(RemoveNotAlive, null, period * 1000, period * 1000);
        }

        void StopTimer()
        {
            clearTimer.TryDispose();
            clearTimer = null;
        }

        /// <summary>移除过期的缓存项</summary>
        void RemoveNotAlive(Object state)
        {
            var expriod = ClearPeriod;
            if (expriod <= 0) return;

            var dic = Items;
            if (dic.Count < 1)
            {
                // 缓存数小于0时关闭定时器
                StopTimer();
                return;
            }
            lock (dic)
            {
                if (dic.Count < 1)
                {
                    StopTimer();
                    return;
                }

                // 这里先计算，性能很重要
                var now = DateTime.Now;
                var exp = now.AddSeconds(-1 * expriod);
                foreach (var item in dic.ToArray())
                {
                    var t = item.Value.ExpiredTime;
                    if (t < exp)
                    {
                        // 自动释放对象
                        if (AutoDispose) item.Value.Value.TryDispose();

                        dic.Remove(item.Key);
                    }
                }
            }
        }
        #endregion

        #region IDictionary<TKey,TValue> 成员
        /// <summary></summary>
        /// <param name="key"></param>
        /// <param name="value">数值</param>
        public void Add(TKey key, TValue value) { Items.Add(key, new CacheItem(value, Expire)); }

        /// <summary></summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(TKey key) { return Items.ContainsKey(key); }

        /// <summary></summary>
        public ICollection<TKey> Keys { get { return Items.Keys; } }

        /// <summary></summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Remove(TKey key) { return Items.Remove(key); }

        /// <summary></summary>
        /// <param name="key"></param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            CacheItem item = null;
            var rs = Items.TryGetValue(key, out item);
            value = rs && item != null && (Expire <= 0 || !item.Expired) ? item.Value : default(TValue);
            return rs;
        }

        /// <summary></summary>
        public ICollection<TValue> Values { get { return Items.Values.Select(e => e.Value).ToArray(); } }

        #endregion

        #region ICollection<KeyValuePair<TKey,TValue>> 成员

        /// <summary></summary>
        /// <param name="item"></param>
        public void Add(KeyValuePair<TKey, TValue> item) { Add(item.Key, item.Value); }

        /// <summary></summary>
        public void Clear() { Items.Clear(); }

        /// <summary></summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(KeyValuePair<TKey, TValue> item) { return ContainsKey(item.Key); }

        /// <summary></summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) { throw new NotImplementedException(); }

        /// <summary></summary>
        public int Count { get { return Items.Count; } }

        /// <summary></summary>
        public bool IsReadOnly { get { return (Items as ICollection<KeyValuePair<TKey, CacheItem>>).IsReadOnly; } }

        /// <summary></summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(KeyValuePair<TKey, TValue> item) { return Remove(item.Key); }

        #endregion

        #region IEnumerable<KeyValuePair<TKey,TValue>> 成员
        /// <summary></summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return Items.Select(e => new KeyValuePair<TKey, TValue>(e.Key, e.Value.Value)).ToList().GetEnumerator();
        }

        #endregion

        #region IEnumerable 成员

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        #endregion
    }
}