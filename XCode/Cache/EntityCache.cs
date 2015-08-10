using System;
using System.Text;
using System.Threading;
using NewLife.Log;
using NewLife.Threading;
using XCode.DataAccessLayer;

namespace XCode.Cache
{
    /// <summary>实体缓存</summary>
    /// <remarks>
    /// 关于异步缓存，非常有用！
    /// 第一次读取缓存的时候，同步从数据库读取，这样子手上有一份数据。
    /// 以后更新，都开异步线程去读取，而当前马上返回，让大家继续用着旧数据，这么做性能非常好。
    /// </remarks>
    /// <typeparam name="TEntity">实体类型</typeparam>
    public class EntityCache<TEntity> : CacheBase<TEntity>, IEntityCache where TEntity : Entity<TEntity>, new()
    {
        #region 基础属性
        private DateTime _ExpiredTime;
        /// <summary>缓存过期时间</summary>
        DateTime ExpiredTime { get { return _ExpiredTime; } set { _ExpiredTime = value; } }

        /// <summary>缓存更新次数</summary>
        private Int64 Times;

        private Int32 _Expriod = CacheSetting.EntityCacheExpire;
        /// <summary>过期时间。单位是秒，默认60秒</summary>
        public Int32 Expriod { get { return _Expriod; } set { _Expriod = value; } }

        private Boolean _Asynchronous = true;
        /// <summary>异步更新，默认打开</summary>
        public Boolean Asynchronous { get { return _Asynchronous; } set { _Asynchronous = value; } }

        private Boolean _Using;
        /// <summary>是否在使用缓存，在不触发缓存动作的情况下检查是否有使用缓存</summary>
        internal Boolean Using { get { return _Using; } private set { _Using = value; } }

        /// <summary>当前获得锁的线程</summary>
        private Int32 _thread = 0;
        ///// <summary>是否正在繁忙</summary>
        //internal Boolean Busy { get { return _thread > 0; } }
        #endregion

        #region 缓存核心
        private EntityList<TEntity> _Entities = new EntityList<TEntity>();
        /// <summary>实体集合。无数据返回空集合而不是null</summary>
        public EntityList<TEntity> Entities
        {
            get
            {
                // 更新统计信息
                XCache.CheckShowStatics(ref NextShow, ref Total, ShowStatics);

                // 只要访问了实体缓存数据集合，就认为是使用了实体缓存，允许更新缓存数据期间向缓存集合添删数据
                Using = true;

                // 两种情况更新缓存：1，缓存过期；2，不允许空但是集合又是空
                Boolean nodata = _Entities.Count == 0;
                if (nodata || DateTime.Now >= ExpiredTime)
                {
                    // 为了确保缓存数据有效可用，这里必须加锁，保证第一个线程更新拿到数据之前其它线程全部排队
                    // 即使打开了异步更新，首次读取数据也是同步
                    // 这里特别要注意，第一个线程取得锁以后，如果因为设计失误，导致重复进入缓存，这是设计错误

                    //!!! 所有加锁的地方都务必消息，同一个线程可以重入同一个锁
                    //if (_thread == Thread.CurrentThread.ManagedThreadId) throw new XCodeException("设计错误！当前线程正在获取缓存，在完成之前，本线程不应该使用实体缓存！");
                    // 同一个线程重入查询实体缓存时，直接返回已有缓存或者空，这符合一般设计逻辑
                    if (_thread == Thread.CurrentThread.ManagedThreadId) return _Entities;

                    lock (this)
                    {
                        _thread = Thread.CurrentThread.ManagedThreadId;

                        nodata = _Entities.Count == 0;
                        if (nodata || DateTime.Now >= ExpiredTime)
                            UpdateCache(nodata);
                        else
                            Interlocked.Increment(ref Shoot2);

                        _thread = 0;
                    }
                }
                else
                    Interlocked.Increment(ref Shoot1);

                return _Entities;
            }
        }

        private Func<EntityList<TEntity>> _FillListMethod;
        /// <summary>填充数据的方法</summary>
        public Func<EntityList<TEntity>> FillListMethod
        {
            get
            {
                if (_FillListMethod == null) _FillListMethod = Entity<TEntity>.FindAll;
                return _FillListMethod;
            }
            set { _FillListMethod = value; }
        }
        #endregion

        #region 缓存操作
        void UpdateCache(Boolean nodata)
        {
            // 异步更新时，如果为空，表明首次，同步获取数据
            // 有且仅有非首次且数据不为空时执行异步查询
            if (Times > 0 && Asynchronous && !nodata)
            {
                // 这里直接计算有效期，避免每次判断缓存有效期时进行的时间相加而带来的性能损耗
                // 设置时间放在获取缓存之前，让其它线程不要空等
                ExpiredTime = DateTime.Now.AddSeconds(Expriod);
                Times++;

                if (Debug)
                {
                    var reason = Times == 1 ? "第一次" : (nodata ? "无缓存数据" : Expriod + "秒过期");
                    DAL.WriteLog("异步更新实体缓存（第{2}次）：{0} 原因：{1} {3}", typeof(TEntity).FullName, reason, Times, XTrace.GetCaller(3, 16));
                }

                ThreadPoolX.QueueUserWorkItem(FillWaper, Times);
            }
            else
            {
                Times++;
                if (Debug)
                {
                    var reason = Times == 1 ? "第一次" : (nodata ? "无缓存数据" : Expriod + "秒过期");
                    DAL.WriteLog("更新实体缓存（第{2}次）：{0} 原因：{1} {3}", typeof(TEntity).FullName, reason, Times, XTrace.GetCaller(3, 16));
                }

                FillWaper(Times);

                // 这里直接计算有效期，避免每次判断缓存有效期时进行的时间相加而带来的性能损耗
                // 设置时间放在获取缓存之后，避免缓存尚未拿到，其它线程拿到空数据
                ExpiredTime = DateTime.Now.AddSeconds(Expriod);
            }
        }

        private void FillWaper(Object state)
        {
            _Entities = Invoke<Object, EntityList<TEntity>>(s => FillListMethod(), null);

            if (Debug) DAL.WriteLog("完成更新缓存（第{1}次）：{0}", typeof(TEntity).FullName, state);
        }

        /// <summary>清除缓存</summary>
        public void Clear(String reason = null)
        {
            lock (this)
            {
                if (_Entities.Count > 0 && Debug) DAL.WriteLog("清空实体缓存：{0} 原因：{1}", typeof(TEntity).FullName, reason);

                // 使用异步时，马上打开异步查询更新数据
                if (Asynchronous && _Entities.Count > 0)
                    UpdateCache(false);
                else
                {
                    // 修改为最小，确保过期
                    ExpiredTime = DateTime.MinValue;
                }

                // 清空后，表示不使用缓存
                Using = false;
            }
        }

        private IEntityOperate Operate = Entity<TEntity>.Meta.Factory;
        internal void Update(TEntity entity)
        {
            // 正在更新当前缓存，跳过
            //if (!Using || _thread > 0 || _Entities == null) return;
            if (!Using) return;

            // 尽管用了事务保护，但是仍然可能有别的地方导致实体缓存更新，这点务必要注意
            var fi = Operate.Unique;
            var e = fi != null ? _Entities.Find(fi.Name, entity[fi.Name]) : null;
            if (e != null)
            {
                //if (e != entity) e.CopyFrom(entity);
                // 更新实体缓存时，不做拷贝，避免产生脏数据，如果恰巧又使用单对象缓存，那会导致自动保存
                lock (_Entities)
                {
                    _Entities.Remove(e);
                }
            }

            //// 加入超级缓存的实体对象，需要标记来自数据库
            //entity.MarkDb(true);
            lock (_Entities)
            {
                _Entities.Add(entity);
            }
        }
        #endregion

        #region 统计
        /// <summary>总次数</summary>
        public Int32 Total;

        /// <summary>第一次命中</summary>
        public Int32 Shoot1;

        /// <summary>第二次命中</summary>
        public Int32 Shoot2;

        /// <summary>下一次显示时间</summary>
        public DateTime NextShow;

        /// <summary>显示统计信息</summary>
        public void ShowStatics()
        {
            if (Total > 0)
            {
                var sb = new StringBuilder();
                sb.AppendFormat("实体缓存<{0,-20}>", typeof(TEntity).Name);
                sb.AppendFormat("总次数{0,7:n0}", Total);
                if (Shoot1 > 0) sb.AppendFormat("，命中{0,7:n0}（{1,6:P02}）", Shoot1, (Double)Shoot1 / Total);
                if (Shoot2 > 0) sb.AppendFormat("，二级命中{0,3:n0}（{1,6:P02}）", Shoot2, (Double)Shoot2 / Total);

                XTrace.WriteLine(sb.ToString());
            }
        }
        #endregion

        #region IEntityCache 成员
        EntityList<IEntity> IEntityCache.Entities { get { return new EntityList<IEntity>(Entities); } }

        /// <summary>根据指定项查找</summary>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        public IEntity Find(string name, object value) { return Entities.Find(name, value); }

        /// <summary>根据指定项查找</summary>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        public EntityList<IEntity> FindAll(string name, object value) { return new EntityList<IEntity>(Entities.FindAll(name, value)); }

        /// <summary>检索与指定谓词定义的条件匹配的所有元素。</summary>
        /// <param name="match">条件</param>
        /// <returns></returns>
        public EntityList<IEntity> FindAll(Predicate<IEntity> match) { return new EntityList<IEntity>(Entities.FindAll(e => match(e))); }
        #endregion

        #region 辅助
        internal EntityCache<TEntity> CopySettingFrom(EntityCache<TEntity> ec)
        {
            this.Expriod = ec.Expriod;
            this.Asynchronous = ec.Asynchronous;
            this.FillListMethod = ec.FillListMethod;

            return this;
        }
        #endregion
    }
}