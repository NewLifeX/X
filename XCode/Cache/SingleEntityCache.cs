using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using NewLife.Log;
using System.Threading;

namespace XCode.Cache
{
    /// <summary>
    /// 单对象缓存
    /// </summary>
    /// <typeparam name="TKey">键值类型</typeparam>
    /// <typeparam name="TEntity">实体类型</typeparam>
    public class SingleEntityCache<TKey, TEntity> : CacheBase<TEntity> where TEntity : Entity<TEntity>, new()
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
                    //Type t = typeof(TEntity);
                    //MethodInfo method = t.GetMethod("FindByKey");
                    //if (method != null)
                    //    _FindKeyMethod = Delegate.CreateDelegate(typeof(FindKeyDelegate<TKey, TEntity>), method) as FindKeyDelegate<TKey, TEntity>;
                    _FindKeyMethod = delegate(TKey key) { return Entity<TEntity>.FindByKey(key); };

                    if (_FindKeyMethod == null) throw new Exception("没有找到FindByKey方法，请先设置查找数据的方法！");
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

        #region 缓存对象
        /// <summary>
        /// 缓存对象
        /// </summary>
        class CacheItem
        {
            ///// <summary>
            ///// 实体
            ///// </summary>
            //public TEntity Entity;

            //// nnhy 2010-10-21
            //// 改为使用弱引用，避免单对象实体永久缓存一些不再使用的对象

            //private WeakReference<TEntity> _Entity;
            private TEntity _Entity;
            /// <summary>实体</summary>
            public TEntity Entity
            {
                get { return _Entity; }
                set { _Entity = value; }
            }

            /// <summary>
            /// 缓存时间
            /// </summary>
            public DateTime CacheTime = DateTime.Now.AddDays(-100);
        }
        #endregion

        #region 单对象缓存
        private SortedList<TKey, CacheItem> _Entities;
        /// <summary>单对象缓存</summary>
        private SortedList<TKey, CacheItem> Entities
        {
            get
            {
                if (_Entities == null) _Entities = new SortedList<TKey, CacheItem>();
                return _Entities;
            }
        }
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

        /// <summary>最后显示时间</summary>
        public DateTime LastShow;

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

            #region 统计
            if (LastShow == DateTime.MinValue) LastShow = DateTime.Now;
            if (LastShow.AddHours(10) < DateTime.Now)
            {
                LastShow = DateTime.Now;

                ShowStatics();
            }

            Interlocked.Increment(ref Total);
            #endregion

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
                    TKey key2 = Entities.Keys[0];
                    if (key2 != null && (Type.GetTypeCode(typeof(TKey)) != TypeCode.String || String.IsNullOrEmpty(key2 as String)))
                    {
                        CacheItem item2 = null;
                        if (Entities.TryGetValue(key2, out item2) && item2 != null)
                        {
                            Entities.RemoveAt(0);

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
                    item.CacheTime = DateTime.Now;

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
            if (DateTime.Now <= item.CacheTime.AddSeconds(Expriod))
            {
                Interlocked.Increment(ref Shoot);
                return item.Entity;
            }

            //自动保存
            if (AutoSave && item.Entity != null) InvokeFill(delegate { item.Entity.Update(); });

            //查找数据
            //item.Entity = FindKeyMethod(key);
            InvokeFill(delegate { item.Entity = FindKeyMethod(key); });
            item.CacheTime = DateTime.Now;

            return item.Entity;
        }

        /// <summary>
        /// 获取数据
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TEntity this[TKey key]
        {
            get
            {
                return GetItem(key);
            }
        }
        #endregion

        #region 方法
        /// <summary>
        /// 是否包含指定键
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Boolean ContainsKey(TKey key)
        {
            return Entities.ContainsKey(key);
        }

        /// <summary>
        /// 移除指定项
        /// </summary>
        /// <param name="key"></param>
        public void RemoveKey(TKey key)
        {
            CacheItem item = null;
            if (!Entities.TryGetValue(key, out item) || item == null) return;
            lock (Entities)
            {
                if (!Entities.TryGetValue(key, out item) || item == null) return;

                if (AutoSave && item.Entity != null) InvokeFill(delegate { item.Entity.Update(); });

                Entities.Remove(key);
            }
        }

        /// <summary>
        /// 清除所有数据
        /// </summary>
        public void Clear()
        {
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
    }

    /// <summary>
    /// 查找数据的方法
    /// </summary>
    /// <typeparam name="TKey">键值类型</typeparam>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <param name="key">键值</param>
    /// <returns></returns>
    public delegate TEntity FindKeyDelegate<TKey, TEntity>(TKey key) where TEntity : Entity<TEntity>, new();
}
