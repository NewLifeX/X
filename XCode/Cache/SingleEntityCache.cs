using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NewLife.Log;
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
        private Int32 _Expriod = 60;
        /// <summary>过期时间。单位是秒，默认60秒</summary>
        public Int32 Expriod
        {
            get { return _Expriod; }
            set { _Expriod = value; }
        }

        private Int32 _MaxEntity = 10000;
        /// <summary>最大实体数。默认10000</summary>
        public Int32 MaxEntity
        {
            get { return _MaxEntity; }
            set { _MaxEntity = value; }
        }

        private Boolean _AutoSave = true;
        /// <summary>缓存到期时自动保存</summary>
        public Boolean AutoSave
        {
            get { return _AutoSave; }
            set { _AutoSave = value; }
        }

        private Boolean _AllowNull;
        /// <summary>允许缓存空对象</summary>
        public Boolean AllowNull
        {
            get { return _AllowNull; }
            set { _AllowNull = value; }
        }

        private FindKeyDelegate<TKey, TEntity> _FindKeyMethod;
        /// <summary>查找数据的方法</summary>
        public FindKeyDelegate<TKey, TEntity> FindKeyMethod
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

        //private Boolean _Asynchronous;
        ///// <summary>异步更新</summary>
        //public Boolean Asynchronous
        //{
        //    get { return _Asynchronous; }
        //    set { _Asynchronous = value; }
        //}
        #endregion

        #region 构造
        TimerX timer = null;
        /// <summary>实例化一个实体缓存</summary>
        public SingleEntityCache()
        {

            timer = new TimerX(d => Check(), null, Expriod * 1000, Expriod * 1000);
        }

        /// <summary>定期检查实体，如果过期，则触发保存</summary>
        void Check()
        {
            CacheItem[] cs = null;
            if (Entities.Count <= 0) return;
            lock (Entities)
            {
                if (Entities.Count <= 0) return;

                cs = new CacheItem[Entities.Count];
                Entities.Values.CopyTo(cs, 0);
            }

            if (cs != null && cs.Length > 0)
            {
                foreach (var item in cs)
                {
                    // 是否过期
                    if (item.ExpireTime > DateTime.Now && item.Entity != null)
                    {
                        // 自动保存
                        if (AutoSave)
                        {
                            // 捕获异常，不影响别人
                            try
                            {
                                item.Entity.Update();
                            }
                            catch { }
                        }
                        item.Entity = null;
                    }
                }
            }
        }
        #endregion

        #region 缓存对象
        /// <summary>缓存对象</summary>
        class CacheItem
        {
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
        //{
        //    get
        //    {
        //        if (_Entities == null) _Entities = new Dictionary<TKey, CacheItem>();
        //        return _Entities;
        //    }
        //}
        #endregion

        #region 统计
        /// <summary>总次数</summary>
        public Int32 Total;

        /// <summary>命中</summary>
        public Int32 Shoot;

        /// <summary>第一次命中</summary>
        public Int32 Shoot1;

        /// <summary>第二次命中</summary>
        public Int32 Shoot2;

        /// <summary>无效次数</summary>
        public Int32 Invalid;

        /// <summary>下一次显示时间</summary>
        public DateTime NextShow;

        /// <summary>
        /// 显示统计信息
        /// </summary>
        public void ShowStatics()
        {
            if (Total > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("单对象缓存<{0},{1}>", typeof(TKey).Name, typeof(TEntity).Name);
                sb.AppendFormat("总次数{0}", Total);
                if (Shoot > 0) sb.AppendFormat("，数据命中{0}（{1:P02}）", Shoot, (Double)Shoot / Total);
                if (Shoot1 > 0) sb.AppendFormat("，一级命中{0}（{1:P02}）", Shoot1, (Double)Shoot1 / Total);
                if (Shoot2 > 0) sb.AppendFormat("，二级命中{0}（{1:P02}）", Shoot2, (Double)Shoot2 / Total);
                if (Invalid > 0) sb.AppendFormat("，无效次数{0}（{1:P02}）", Invalid, (Double)Invalid / Total);

                XTrace.WriteLine(sb.ToString());
            }
        }
        #endregion

        #region 获取数据
        private TEntity GetItem(TKey key)
        {
            if (key == null) return null;
            if (Type.GetTypeCode(typeof(TKey)) == TypeCode.String)
            {
                String value = key as String;
                if (String.IsNullOrEmpty(value)) return null;
            }

            XCache.CheckShowStatics(ref NextShow, ref Total, ShowStatics);

            CacheItem item = null;
            if (Entities.TryGetValue(key, out item) && item != null)
            {
                Interlocked.Increment(ref Shoot1);
                return GetItem(item, key);
            }

            lock (Entities)
            {
                if (Entities.TryGetValue(key, out item) && item != null)
                {
                    Interlocked.Increment(ref Shoot2);
                    return GetItem(item, key);
                }

                item = new CacheItem();

                //队列满时，移除最老的一个
                if (Entities.Count >= MaxEntity)
                {
                    TKey keyFirst = GetFirstKey();
                    if (keyFirst != null && (Type.GetTypeCode(typeof(TKey)) != TypeCode.String || String.IsNullOrEmpty(keyFirst as String)))
                    {
                        CacheItem item2 = null;
                        if (Entities.TryGetValue(keyFirst, out item2) && item2 != null)
                        {
                            if (DAL.Debug) DAL.WriteLog("单实体缓存{0}超过最大数量限制{1}，准备移除第一项{2}", typeof(TEntity), MaxEntity, keyFirst);

                            Entities.Remove(keyFirst);

                            //自动保存
                            if (AutoSave && item2.Entity != null) InvokeFill(delegate { item2.Entity.Update(); });
                        }
                    }
                }

                //查找数据
                //TEntity entity = FindKeyMethod(key);
                TEntity entity = null;
                InvokeFill(delegate { entity = FindKeyMethod(key); });
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
        }

        /// <summary>
        /// 内部处理返回对象。
        /// 把对象传进来，而不是只传键值然后查找，是为了避免别的线程移除该项
        /// </summary>
        /// <param name="item"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private TEntity GetItem(CacheItem item, TKey key)
        {
            if (item == null) return null;

            //未过期，直接返回
            if (DateTime.Now <= item.ExpireTime)
            {
                Interlocked.Increment(ref Shoot);
                return item.Entity;
            }

            //自动保存
            if (AutoSave && item.Entity != null) InvokeFill(delegate { item.Entity.Update(); });

            //查找数据
            //item.Entity = FindKeyMethod(key);
            InvokeFill(delegate { item.Entity = FindKeyMethod(key); });
            item.ExpireTime = DateTime.Now.AddSeconds(Expriod);

            return item.Entity;
        }

        /// <summary>获取数据</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TEntity this[TKey key] { get { return GetItem(key); } }

        private TKey GetFirstKey()
        {
            foreach (var item in Entities)
            {
                return item.Key;
            }
            return default(TKey);

            //Dictionary<TKey, CacheItem>.Enumerator em = Entities.GetEnumerator();
            //if (!em.MoveNext()) return default(TKey);

            //TKey key = em.Current.Key;
            //em.Dispose();
            //return key;
        }
        #endregion

        #region 方法
        /// <summary>是否包含指定键</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Boolean ContainsKey(TKey key) { return Entities.ContainsKey(key); }

        /// <summary>移除指定项</summary>
        /// <param name="key"></param>
        public void RemoveKey(TKey key)
        {
            CacheItem item = null;
            if (!Entities.TryGetValue(key, out item)) return;
            lock (Entities)
            {
                if (!Entities.TryGetValue(key, out item)) return;

                if (AutoSave && item != null && item.Entity != null) InvokeFill(delegate { item.Entity.Update(); });

                Entities.Remove(key);
            }
        }

        /// <summary>清除所有数据</summary>
        public void Clear()
        {
            if (DAL.Debug) DAL.WriteLog("清空单对象缓存：{0}", typeof(TEntity).FullName);

            if (AutoSave)
            {
                lock (Entities)
                {
                    foreach (TKey key in Entities.Keys)
                    {
                        CacheItem item = Entities[key];
                        if (item == null || item.Entity == null) continue;

                        //item.Entity.Update();
                        InvokeFill(delegate { item.Entity.Update(); });
                    }
                }
            }

            Entities.Clear();
        }
        #endregion

        #region ISingleEntityCache 成员
        /// <summary>获取数据</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        IEntity ISingleEntityCache.this[object key] { get { return GetItem((TKey)key); } }
        #endregion
    }

    /// <summary>查找数据的方法</summary>
    /// <typeparam name="TKey">键值类型</typeparam>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <param name="key">键值</param>
    /// <returns></returns>
    public delegate TEntity FindKeyDelegate<TKey, TEntity>(TKey key) where TEntity : Entity<TEntity>, new();
}