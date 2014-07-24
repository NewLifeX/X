using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Threading;
using XCode.DataAccessLayer;

namespace XCode.Cache
{
    /// <summary>单对象缓存</summary>
    /// <remarks>
    /// 用一个值为实体的字典作为缓存（键一般就是主键），适用于单表大量互相没有关系的数据。
    /// 同时，AutoSave能够让缓存项在过期时自动保存数据，该特性特别适用于点击计数等场合。
    /// </remarks>
    /// <typeparam name="TKey">键值类型</typeparam>
    /// <typeparam name="TEntity">实体类型</typeparam>
    public class SingleEntityCache<TKey, TEntity> : CacheBase<TEntity>, ISingleEntityCache where TEntity : Entity<TEntity>, new()
    {
        #region 属性
        private Int32 _Expriod = CacheSetting.SingleCacheExpire;
        /// <summary>过期时间。单位是秒，默认60秒/600秒（独占数据库）</summary>
        public Int32 Expriod { get { return _Expriod; } set { _Expriod = value; } }

        private Int32 _MaxEntity = 10000;
        /// <summary>最大实体数。默认10000</summary>
        public Int32 MaxEntity { get { return _MaxEntity; } set { _MaxEntity = value; } }

        private Boolean _AutoSave = true;
        /// <summary>缓存到期时自动保存</summary>
        public Boolean AutoSave { get { return _AutoSave; } set { _AutoSave = value; } }

        private Boolean _AllowNull;
        /// <summary>允许缓存空对象</summary>
        public Boolean AllowNull { get { return _AllowNull; } set { _AllowNull = value; } }

        private Func<TKey, TEntity> _FindKeyMethod;
        /// <summary>查找数据的方法</summary>
        public Func<TKey, TEntity> FindKeyMethod
        {
            get
            {
                if (_FindKeyMethod == null)
                {
                    _FindKeyMethod = key => Entity<TEntity>.FindByKey(key);

                    if (_FindKeyMethod == null) throw new ArgumentNullException("FindKeyMethod", "没有找到FindByKey方法，请先设置查找数据的方法！");
                }
                return _FindKeyMethod;
            }
            set { _FindKeyMethod = value; }
        }

        private Boolean _Using;
        /// <summary>是否在使用缓存</summary>
        internal Boolean Using { get { return _Using; } private set { _Using = value; } }
        #endregion

        #region 构造、检查过期缓存
        TimerX timer = null;
        /// <summary>实例化一个实体缓存</summary>
        public SingleEntityCache()
        {
            // 启动一个定时器，用于定时清理过期缓存。因为比较耗时，最后一个参数采用线程池
            timer = new TimerX(d => Check(), null, Expriod * 1000, Expriod * 1000, true);
        }

        /// <summary>子类重载实现资源释放逻辑时必须首先调用基类方法</summary>
        /// <param name="disposing">从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）。
        /// 因为该方法只会被调用一次，所以该参数的意义不太大。</param>
        protected override void OnDispose(bool disposing)
        {
            Clear();

            if (timer != null) timer.Dispose();

            base.OnDispose(disposing);
        }

        /// <summary>定期检查实体，如果过期，则触发保存</summary>
        void Check()
        {
            // 加锁后把缓存集合拷贝到数组中，避免后面遍历的时候出现多线程冲突
            CacheItem[] cs = null;
            if (Entities.Count <= 0) return;
            lock (Entities)
            {
                if (Entities.Count <= 0) return;

                cs = new CacheItem[Entities.Count];
                Entities.Values.CopyTo(cs, 0);
            }

            if (cs == null || cs.Length < 0) return;

            var list = new List<TKey>();
            foreach (var item in cs)
            {
                // 是否过期
                // 单对象缓存每次缓存的时候，设定一个将来的过期时间，然后以后只需要比较过期时间和当前时间就可以了
                if (item.ExpireTime <= DateTime.Now)
                {
                    if (item.Entity != null)
                    {
                        // 自动保存
                        if (AutoSave)
                        {
                            // 捕获异常，不影响别人
                            try
                            {
                                //item.Entity.Update();
                                // 需要在原连接名表名里面更新对象
                                AutoUpdate(item);
                            }
                            catch { }
                        }
                        item.Entity = null;
                    }
                    list.Add(item.Key);
                }
            }
            // 从缓存中删除，必须加锁
            if (list.Count > 0)
            {
                lock (Entities)
                {
                    foreach (var item in list)
                    {
                        if (Entities.ContainsKey(item)) Entities.Remove(item);
                    }

                    Using = Entities.Count > 0;
                }
            }
        }
        #endregion

        #region 缓存对象
        /// <summary>缓存对象</summary>
        class CacheItem
        {
            /// <summary>键</summary>
            public TKey Key;

            /// <summary>实体</summary>
            public TEntity Entity;

            /// <summary>缓存过期时间</summary>
            public DateTime ExpireTime;
        }
        #endregion

        #region 单对象缓存
        //private SortedList<TKey, CacheItem> _Entities;
        //! Dictionary在集合方面具有较好查找性能，直接用字段，提高可能的性能
        /// <summary>单对象缓存</summary>
        private Dictionary<TKey, CacheItem> Entities = new Dictionary<TKey, CacheItem>();
        #endregion

        #region 统计
        /// <summary>总次数</summary>
        public Int32 Total;

        /// <summary>命中</summary>
        public Int32 Shoot;

        /// <summary>第一次命中，加锁之前</summary>
        public Int32 Shoot1;

        /// <summary>第二次命中，加锁之后</summary>
        public Int32 Shoot2;

        /// <summary>无效次数，不允许空但是查到对象又为空</summary>
        public Int32 Invalid;

        /// <summary>下一次显示时间</summary>
        public DateTime NextShow;

        /// <summary>显示统计信息</summary>
        public void ShowStatics()
        {
            if (Total > 0)
            {
                var sb = new StringBuilder();
                sb.AppendFormat("单对象缓存<{0},{1}>", typeof(TKey).Name, typeof(TEntity).Name);
                sb.AppendFormat("总次数{0}", Total);
                if (Shoot > 0) sb.AppendFormat("，数据命中{0}（{1:P02}）", Shoot, (Double)Shoot / Total);
                // 一级命中和总命中相等时不显示
                if (Shoot1 > 0 && Shoot1 != Shoot) sb.AppendFormat("，一级命中{0}（{1:P02}）", Shoot1, (Double)Shoot1 / Total);
                if (Shoot2 > 0) sb.AppendFormat("，二级命中{0}（{1:P02}）", Shoot2, (Double)Shoot2 / Total);
                if (Invalid > 0) sb.AppendFormat("，无效次数{0}（{1:P02}）", Invalid, (Double)Invalid / Total);

                XTrace.WriteLine(sb.ToString());
            }
        }
        #endregion

        #region 获取数据
        /// <summary>获取数据</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TEntity this[TKey key] { get { return GetItem(key); } }

        private TEntity GetItem(TKey key)
        {
            // 为空的key，直接返回null，不进行缓存查找
            if (key == null) return null;
            if (Type.GetTypeCode(typeof(TKey)) == TypeCode.String)
            {
                var value = key as String;
                if (value == String.Empty) return null;
            }

            // 更新统计信息
            XCache.CheckShowStatics(ref NextShow, ref Total, ShowStatics);

            // 如果找到项，返回
            CacheItem item = null;
            if (Entities.TryGetValue(key, out item) && item != null)
            {
                Interlocked.Increment(ref Shoot1);
                // 下面的GetData里会判断过期并处理
                return GetData(item, key);
            }

            // 加锁
            lock (Entities)
            {
                // 如果找到项，返回
                if (Entities.TryGetValue(key, out item) && item != null)
                {
                    Interlocked.Increment(ref Shoot2);
                    return GetData(item, key);
                }

                item = new CacheItem();
                item.Key = key;

                //队列满时，移除最老的一个
                while (Entities.Count >= MaxEntity) RemoveFirst();

                Using = true;

                // 更新缓存
                return UpdateCache(item, key);
            }
        }

        /// <summary>内部处理返回对象。
        /// 把对象传进来，而不是只传键值然后查找，是为了避免别的线程移除该项
        /// </summary>
        /// <param name="item"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private TEntity GetData(CacheItem item, TKey key)
        {
            if (item == null) return null;

            // 未过期，直接返回
            if (DateTime.Now <= item.ExpireTime)
            {
                Interlocked.Increment(ref Shoot);
                return item.Entity;
            }

            // 自动保存
            AutoUpdate(item);

            // 更新过期缓存
            return UpdateCache(item, key);
        }

        /// <summary>更新缓存</summary>
        /// <param name="item"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        TEntity UpdateCache(CacheItem item, TKey key)
        {
            // 在原连接名表名里面获取
            var entity = Invoke(FindKeyMethod, key);
            if (entity != null || AllowNull)
            {
                item.Entity = entity;
                item.ExpireTime = DateTime.Now.AddSeconds(Expriod);

                if (!Entities.ContainsKey(key)) Entities.Add(key, item);
            }
            else
            {
                Interlocked.Increment(ref Invalid);
            }

            return entity;
        }

        /// <summary>移除第一个缓存项</summary>
        private void RemoveFirst()
        {
            var keyFirst = GetFirstKey();
            if (keyFirst != null && (Type.GetTypeCode(typeof(TKey)) != TypeCode.String || String.IsNullOrEmpty(keyFirst as String)))
            {
                CacheItem item = null;
                if (Entities.TryGetValue(keyFirst, out item) && item != null)
                {
                    if (Debug) DAL.WriteLog("单实体缓存{0}超过最大数量限制{1}，准备移除第一项{2}", typeof(TEntity).FullName, MaxEntity, keyFirst);

                    Entities.Remove(keyFirst);

                    //自动保存
                    AutoUpdate(item);
                }
            }
        }

        /// <summary>获取第一个缓存项</summary>
        /// <returns></returns>
        private TKey GetFirstKey()
        {
            foreach (var item in Entities)
            {
                return item.Key;
            }
            return default(TKey);
        }

        /// <summary>自动更新，最主要是在原连接名和表名里面更新对象</summary>
        /// <param name="item"></param>
        private void AutoUpdate(CacheItem item)
        {
            if (item != null && AutoSave && item.Entity != null) Invoke(e => e.Entity.Update(), item);
        }
        #endregion

        #region 方法
        /// <summary>是否包含指定键</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Boolean ContainsKey(TKey key) { return Entities.ContainsKey(key); }

        /// <summary>向单对象缓存添加项</summary>
        /// <param name="key"></param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public Boolean Add(TKey key, TEntity value)
        {
            // 如果找到项，返回
            CacheItem item = null;
            if (Entities.TryGetValue(key, out item) && item != null && DateTime.Now <= item.ExpireTime) return false;

            // 加锁
            lock (Entities)
            {
                if (Entities.TryGetValue(key, out item))
                {
                    // 如果已存在并且过期，则复制
                    if (item != null)
                    {
                        if (DateTime.Now <= item.ExpireTime) return false;
                        item.Entity.CopyFrom(value);
                    }

                    return false;
                }

                item = new CacheItem();
                item.Key = key;
                item.Entity = value;
                item.ExpireTime = DateTime.Now.AddSeconds(Expriod);

                Entities.Add(key, item);

                return true;
            }
        }

        /// <summary>移除指定项</summary>
        /// <param name="key"></param>
        public void RemoveKey(TKey key)
        {
            CacheItem item = null;
            if (!Entities.TryGetValue(key, out item)) return;
            lock (Entities)
            {
                if (!Entities.TryGetValue(key, out item)) return;

                AutoUpdate(item);

                Entities.Remove(key);

                Using = Entities.Count > 0;
            }
        }

        /// <summary>清除所有数据</summary>
        public void Clear()
        {
            if (Debug) DAL.WriteLog("清空单对象缓存：{0}", typeof(TEntity).FullName);

            if (AutoSave)
            {
                // 加锁处理自动保存
                lock (Entities)
                {
                    foreach (var key in Entities)
                    {
                        var item = key.Value;
                        if (item == null || item.Entity == null) continue;

                        //item.Entity.Update();
                        AutoUpdate(item);
                    }
                }
            }

            Entities.Clear();

            Using = false;
        }
        #endregion

        #region ISingleEntityCache 成员
        /// <summary>获取数据</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        IEntity ISingleEntityCache.this[object key] { get { return GetItem((TKey)key); } }
        #endregion

        #region 辅助
        internal SingleEntityCache<TKey, TEntity> CopySettingFrom(SingleEntityCache<TKey, TEntity> ec)
        {
            this.Expriod = ec.Expriod;
            this.MaxEntity = ec.MaxEntity;
            this.AutoSave = ec.AutoSave;
            this.AllowNull = ec.AllowNull;
            this.FindKeyMethod = ec.FindKeyMethod;

            return this;
        }
        #endregion
    }
}