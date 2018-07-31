using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NewLife;
using NewLife.Collections;
using NewLife.Log;
using NewLife.Threading;

namespace XCode.Cache
{
    /// <summary>单对象缓存</summary>
    /// <remarks>
    /// 用一个值为实体的字典作为缓存（键一般就是主键），适用于单表大量互相没有关系的数据。
    /// </remarks>
    /// <typeparam name="TKey">键值类型</typeparam>
    /// <typeparam name="TEntity">实体类型</typeparam>
    public class SingleEntityCache<TKey, TEntity> : CacheBase<TEntity>, ISingleEntityCache<TKey, TEntity> where TEntity : Entity<TEntity>, new()
    {
        #region 属性
        /// <summary>过期时间。单位是秒，默认60秒</summary>
        public Int32 Expire { get; set; }

        /// <summary>最大实体数。默认10000</summary>
        public Int32 MaxEntity { get; set; } = 10000;

        /// <summary>是否在使用缓存</summary>
        public Boolean Using { get; private set; }
        #endregion

        #region 主键
        /// <summary>获取缓存主键的方法，默认方法为获取实体主键值</summary>
        public Func<TEntity, TKey> GetKeyMethod { get; set; }

        /// <summary>查找数据的方法</summary>
        public Func<TKey, TEntity> FindKeyMethod { get; set; }
        #endregion

        #region 从键
        /// <summary>从键是否区分大小写</summary>
        public Boolean SlaveKeyIgnoreCase { get; set; }

        /// <summary>根据从键查找数据的方法</summary>
        public Func<String, TEntity> FindSlaveKeyMethod { get; set; }

        /// <summary>获取缓存从键的方法，默认为空</summary>
        public Func<TEntity, String> GetSlaveKeyMethod { get; set; }
        #endregion

        #region 构造、检查过期缓存
        /// <summary>实例化一个实体缓存</summary>
        public SingleEntityCache()
        {
            var exp = Setting.Current.SingleCacheExpire;
            if (exp <= 0) exp = 60;
            Expire = exp;

            var fi = Entity<TEntity>.Meta.Unique;
            if (fi != null) GetKeyMethod = entity => (TKey)entity[Entity<TEntity>.Meta.Unique.Name];
            FindKeyMethod = key => Entity<TEntity>.FindByKey(key);
        }

        /// <summary>子类重载实现资源释放逻辑时必须首先调用基类方法</summary>
        /// <param name="disposing">从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）。
        /// 因为该方法只会被调用一次，所以该参数的意义不太大。</param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            try
            {
                var reason = GetType().Name + (disposing ? "Dispose" : "GC");
                Clear(reason);
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }

            _Timer.TryDispose();
        }

        private TimerX _Timer;
        private void StartTimer()
        {
            if (_Timer == null)
            {
                var period = Expire * 1000;
                if (period > 60 * 1000) period = 60 * 1000;

                // 启动一个定时器，用于定时清理过期缓存。因为比较耗时，最后一个参数采用线程池
                _Timer = new TimerX(CheckExpire, null, period, period, "SC")
                {
                    Async = true
                };
            }
        }

        private void CheckExpire(Object state)
        {
            var es = Entities;
            if (es == null) return;

            // 过期时间升序，用于缓存满以后删除
            var slist = new SortedList<DateTime, IList<CacheItem>>();

            // 超出个数
            var over = es.Count - MaxEntity;
            if (MaxEntity <= 0 || over < 0) slist = null;

            // 找到所有很久未访问的缓存项，10倍
            var exp = TimerX.Now.AddSeconds(-10 * Expire);
            var list = new List<CacheItem>();
            foreach (var item in es)
            {
                var ci = item.Value;
                if (ci.VisitTime <= exp)
                    list.Add(ci);
                else if (slist != null)
                {
                    if (!slist.TryGetValue(ci.VisitTime, out var ss))
                        slist.Add(ci.VisitTime, ss = new List<CacheItem>());

                    ss.Add(ci);
                }
            }

            // 如果满了，删除前面
            if (slist != null)
            {
                over -= list.Count;
                if (over > 0)
                {
                    for (var i = 0; i < slist.Count && over > 0; i++)
                    {
                        var ss = slist.Values[i];
                        if (ss != null && ss.Count > 0)
                        {
                            over -= ss.Count;
                            list.AddRange(ss);
                        }
                    }

                    XTrace.WriteLine("对象缓存<{0}>满，{1:n0}>{2:n0}，删除[{3:n0}]个", typeof(TEntity).Name, es.Count, MaxEntity, list.Count);
                }
            }

            // 主从一起删除
            var ses = _SlaveEntities;
            foreach (var item in list)
            {
                if (es.Remove(item.Key)) Interlocked.Decrement(ref _Count);
                if (ses != null && !item.SlaveKey.IsNullOrEmpty()) ses.Remove(item.SlaveKey);
            }
        }
        #endregion

        #region 缓存对象
        /// <summary>缓存对象</summary>
        class CacheItem
        {
            /// <summary>键</summary>
            public TKey Key { get; set; }

            /// <summary>从键</summary>
            public String SlaveKey { get; set; }

            /// <summary>实体</summary>
            public TEntity Entity { get; set; }

            /// <summary>访问时间</summary>
            public DateTime VisitTime { get; set; }

            /// <summary>缓存过期时间</summary>
            public DateTime ExpireTime { get; set; }

            /// <summary>是否已经过期</summary>
            public Boolean Expired => ExpireTime <= TimerX.Now;

            public void SetEntity(TEntity entity, Int32 expire)
            {
                Entity = entity;
                ExpireTime = TimerX.Now.AddSeconds(expire);
                VisitTime = ExpireTime;
            }
        }
        #endregion

        #region 单对象缓存
        /// <summary>缓存个数</summary>
        private Int32 _Count;

        /// <summary>单对象缓存</summary>
        private ConcurrentDictionary<TKey, CacheItem> Entities = new ConcurrentDictionary<TKey, CacheItem>();

        private ConcurrentDictionary<String, CacheItem> _SlaveEntities;
        /// <summary>单对象缓存，从键查询使用</summary>
        private ConcurrentDictionary<String, CacheItem> SlaveEntities
        {
            get
            {
                if (_SlaveEntities == null)
                {
                    lock (this)
                    {
                        if (_SlaveEntities == null)
                        {
                            if (SlaveKeyIgnoreCase)
                                _SlaveEntities = new ConcurrentDictionary<String, CacheItem>(StringComparer.OrdinalIgnoreCase);
                            else
                                _SlaveEntities = new ConcurrentDictionary<String, CacheItem>();
                        }
                    }
                }
                return _SlaveEntities;
            }
        }
        #endregion

        #region 统计
        /// <summary>总次数</summary>
        public Int32 Total;

        /// <summary>命中</summary>
        public Int32 Success;

        /// <summary>显示统计信息</summary>
        public void ShowStatics()
        {
            if (Total > 0)
            {
                var sb = Pool.StringBuilder.Get();
                var name = "<{0}>({1:n0})".F(typeof(TEntity).Name, Entities.Count);
                sb.AppendFormat("对象缓存{0,-20}", name);
                sb.AppendFormat("总次数{0,11:n0}", Total);
                if (Success > 0) sb.AppendFormat("，命中{0,11:n0}（{1,6:P02}）", Success, (Double)Success / Total);
                sb.AppendFormat("\t[{0}]", typeof(TEntity).FullName);

                XTrace.WriteLine(sb.Put(true));
            }
        }
        #endregion

        #region 获取数据
        /// <summary>根据主键获取实体数据</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TEntity this[TKey key] { get { return GetItem(Entities, key); } set { Add(key, value); } }

        private TEntity GetItem<TKey2>(ConcurrentDictionary<TKey2, CacheItem> dic, TKey2 key)
        {
            // 为空的key，直接返回null，不进行缓存查找
            if (key == null) return null;

            // 更新统计信息
            CheckShowStatics(ref Total, ShowStatics);

            Interlocked.Increment(ref Success);

            // 获取 或 添加
            CacheItem item = null;
            CacheItem ci = null;
            while (!dic.TryGetValue(key, out item))
            {
                if (ci == null) ci = Object.ReferenceEquals(dic, Entities) ? CreateItem(key) : CreateSlaveItem(key);
                // 不要缓存空值
                if (ci == null) return null;

                // 尝试添加，即使多线程并发，这里宁可多浪费点时间，也不要带来锁定
                if (dic.TryAdd(key, ci))
                {
                    item = ci;
                    Interlocked.Increment(ref _Count);
                    break;
                }
            }
            // 最后访问时间
            item.VisitTime = TimerX.Now;

            // 异步更新缓存
            if (item.Expired) ThreadPoolX.QueueUserWorkItem(UpdateData, item);

            return item.Entity;
        }

        private CacheItem CreateItem<TKey2>(TKey2 key)
        {
            Interlocked.Decrement(ref Success);

            // 开始更新数据，然后加入缓存
            var mkey = (TKey)(Object)key;
            var entity = Invoke(FindKeyMethod, mkey);
            return AddItem(mkey, entity);
        }

        private CacheItem CreateSlaveItem<TKey2>(TKey2 key)
        {
            Interlocked.Decrement(ref Success);

            // 开始更新数据，然后加入缓存
            var entity = Invoke(FindSlaveKeyMethod, key + "");
            if (entity == null) return null;

            var mkey = GetKeyMethod(entity);
            return AddItem(mkey, entity);
        }

        /// <summary>向两个字典加入数据</summary>
        /// <param name="key"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        private CacheItem AddItem(TKey key, TEntity entity)
        {
            //if (!Using)
            {
                Using = true;
                //WriteLog("单对象缓存首次使用 {0} {1}", typeof(TEntity).FullName, XTrace.GetCaller(1, 16));
                StartTimer();
            }

            var skey = entity == null ? null : GetSlaveKeyMethod?.Invoke(entity);

            var item = new CacheItem
            {
                Key = key,
                SlaveKey = skey
            };
            item.SetEntity(entity, Expire);

            var es = Entities;
            // 新增或更新
            es[key] = item;

            if (!skey.IsNullOrWhiteSpace())
            {
                var ses = SlaveEntities;
                // 新增或更新
                ses[skey] = item;
            }

            return item;
        }

        /// <summary>根据从键获取实体数据</summary>
        /// <param name="slaveKey"></param>
        /// <returns></returns>
        public TEntity GetItemWithSlaveKey(String slaveKey) => GetItem(SlaveEntities, slaveKey);

        private void UpdateData(Object state)
        {
            var item = state as CacheItem;

            // 先修改过期时间
            item.ExpireTime = TimerX.Now.AddSeconds(Expire);

            try
            {
                // 更新过期缓存，在原连接名表名里面获取
                var entity = Invoke(FindKeyMethod, item.Key);
                if (entity != null)
                    item.SetEntity(entity, Expire);
                else if (item.Entity != null)
                    // 数据库查不到，说明该数据可能已经被删除
                    RemoveKey(item.Key);
            }
            catch (Exception ex)
            {
                XTrace.WriteLine($"[{TableName}/{ConnName}]" + ex.GetTrue());
            }
        }
        #endregion

        #region 方法
        /// <summary>是否包含指定键</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Boolean ContainsKey(TKey key) => Entities.ContainsKey(key);

        /// <summary>是否包含指定从键</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Boolean ContainsSlaveKey(String key) => SlaveEntities.ContainsKey(key);

        /// <summary>向单对象缓存添加项</summary>
        /// <param name="key"></param>
        /// <param name="entity">数值</param>
        /// <returns></returns>
        Boolean Add(TKey key, TEntity entity)
        {
            AddItem(key, entity);

            return true;
        }

        /// <summary>移除指定项</summary>
        /// <param name="key">键值</param>
        private void RemoveKey(TKey key)
        {
            Entities.TryRemove(key, out var item);

            if (item != null && !item.SlaveKey.IsNullOrWhiteSpace())
            {
                SlaveEntities.TryRemove(item.SlaveKey, out item);
            }
        }

        /// <summary>根据主键移除指定项</summary>
        /// <param name="entity"></param>
        public void Remove(TEntity entity)
        {
            if (entity == null) return;
            if (GetKeyMethod == null) return;
            var key = GetKeyMethod(entity);
            RemoveKey(key);
        }

        /// <summary>清除所有数据</summary>
        /// <param name="reason">清除缓存原因</param>
        public void Clear(String reason)
        {
            if (!Using) return;

            WriteLog("清空单对象缓存：{0} 原因：{1} Using = false", typeof(TEntity).FullName, reason);

            var es = Entities;
            if (es == null) return;

            // 不要清空单对象缓存，而是设为过期
            var now = TimerX.Now;
            foreach (var item in es)
            {
                item.Value.ExpireTime = now.AddSeconds(-1);
            }

            Using = false;
        }
        #endregion

        #region ISingleEntityCache 成员
        /// <summary>获取数据</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        IEntity ISingleEntityCache.this[Object key] => this[(TKey)key];

        /// <summary>根据从键获取实体数据</summary>
        /// <param name="slaveKey"></param>
        /// <returns></returns>
        IEntity ISingleEntityCache.GetItemWithSlaveKey(String slaveKey) => GetItemWithSlaveKey(slaveKey);

        /// <summary>是否包含指定主键</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Boolean ISingleEntityCache.ContainsKey(Object key) => ContainsKey((TKey)key);

        /// <summary>移除指定项</summary>
        /// <param name="entity"></param>
        void ISingleEntityCache.Remove(IEntity entity) => Remove(entity as TEntity);

        /// <summary>向单对象缓存添加项</summary>
        /// <param name="value">实体对象</param>
        /// <returns></returns>
        Boolean ISingleEntityCache.Add(IEntity value)
        {
            if (!Using) return false;

            var entity = value as TEntity;
            if (entity == null) return false;

            var key = GetKeyMethod(entity);
            return Add(key, entity);
        }
        #endregion

        #region 辅助
        internal SingleEntityCache<TKey, TEntity> CopySettingFrom(ISingleEntityCache<TKey, TEntity> ec)
        {
            Expire = ec.Expire;
            MaxEntity = ec.MaxEntity;

            GetKeyMethod = ec.GetKeyMethod;
            FindKeyMethod = ec.FindKeyMethod;

            SlaveKeyIgnoreCase = ec.SlaveKeyIgnoreCase;
            GetSlaveKeyMethod = ec.GetSlaveKeyMethod;
            FindSlaveKeyMethod = ec.FindSlaveKeyMethod;

            return this;
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() => "SingleEntityCache<{0}, {1}>[{2}]".F(typeof(TKey).Name, typeof(TEntity).Name, Entities.Count);
        #endregion
    }
}