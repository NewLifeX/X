using System;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
    [DisplayName("实体缓存")]
    public class EntityCache<TEntity> : CacheBase<TEntity>, IEntityCache where TEntity : Entity<TEntity>, new()
    {
        #region 基础属性
        /// <summary>缓存过期时间</summary>
        DateTime ExpiredTime { get; set; }

        /// <summary>缓存更新次数</summary>
        private Int64 Times { get; set; }

        /// <summary>过期时间。单位是秒，默认60秒</summary>
        public Int32 Expire { get; set; }

        /// <summary>填充数据的方法</summary>
        public Func<EntityList<TEntity>> FillListMethod { get; set; } = Entity<TEntity>.FindAll;

        /// <summary>是否等待第一次查询。如果不等待，第一次返回空集合。默认true</summary>
        public Boolean WaitFirst { get; set; } = true;

        /// <summary>是否在使用缓存，在不触发缓存动作的情况下检查是否有使用缓存</summary>
        internal Boolean Using { get; private set; }
        #endregion

        #region 构造
        /// <summary>实例化实体缓存</summary>
        public EntityCache()
        {
            Expire = Setting.Current.EntityCacheExpire;
        }
        #endregion

        #region 缓存核心
        /// <summary>当前更新任务</summary>
        private Task _task;

        private EntityList<TEntity> _Entities = new EntityList<TEntity>();
        /// <summary>实体集合。无数据返回空集合而不是null</summary>
        public EntityList<TEntity> Entities
        {
            get
            {
                CheckCache();

                return _Entities;
            }
        }

        void CheckCache()
        {
            // 更新统计信息
            CheckShowStatics(ref NextShow, ref Total, ShowStatics);

            // 只要访问了实体缓存数据集合，就认为是使用了实体缓存，允许更新缓存数据期间向缓存集合添删数据
            Using = true;

            if (ExpiredTime > DateTime.Now)
            {
                Interlocked.Increment(ref Success);
                return;
            }

            // 建立异步更新任务
            if (_task == null)
            {
                lock (this)
                {
                    if (_task == null)
                    {
                        var reason = Times == 0 ? "第一次" : Expire + "秒过期";
                        _task = UpdateCacheAsync(reason);
                        _task.ContinueWith(t => { _task = null; });
                    }
                }
            }
            // 第一次所有线程一起等待结果
            if (Times == 1 && WaitFirst && _task != null) _task.Wait();
        }
        #endregion

        #region 缓存操作
        Task UpdateCacheAsync(String reason)
        {
            // 这里直接计算有效期，避免每次判断缓存有效期时进行的时间相加而带来的性能损耗
            // 设置时间放在获取缓存之前，让其它线程不要空等
            ExpiredTime = DateTime.Now.AddSeconds(Expire);
            Times++;

            if (Debug) DAL.WriteLog("{0}", XTrace.GetCaller(3, 16));

            return Task.Factory.StartNew(FillWaper, reason);
        }

        private void FillWaper(Object state)
        {
            if (Debug)
            {
                var reason = state + "";
                DAL.WriteLog("更新{0}（第{2}次） 原因：{1}", ToString(), reason, Times);
            }

            _Entities = Invoke<Object, EntityList<TEntity>>(s => FillListMethod(), null);

            if (Debug) DAL.WriteLog("完成{0}[{1}]（第{2}次）", ToString(), _Entities.Count, Times);
        }

        /// <summary>清除缓存</summary>
        public void Clear(String reason = null)
        {
            lock (this)
            {
                if (_Entities.Count > 0 && Debug) DAL.WriteLog("清空{0} 原因：{1}", ToString(), reason);

                // 使用异步时，马上打开异步查询更新数据
                if (_Entities.Count > 0)
                    UpdateCacheAsync("清空 " + reason);
                else
                    // 修改为最小，确保过期
                    ExpiredTime = DateTime.MinValue;

                // 清空后，表示不使用缓存
                Using = false;
            }
        }

        private IEntityOperate Operate = Entity<TEntity>.Meta.Factory;
        internal void Update(TEntity entity)
        {
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

            lock (_Entities)
            {
                _Entities.Add(entity);
            }
        }
        #endregion

        #region 统计
        /// <summary>总次数</summary>
        public Int32 Total;

        /// <summary>命中</summary>
        public Int32 Success;

        /// <summary>下一次显示时间</summary>
        public DateTime NextShow;

        /// <summary>显示统计信息</summary>
        public void ShowStatics()
        {
            if (Total > 0)
            {
                var sb = new StringBuilder();
                //sb.AppendFormat("实体缓存<{0,-20}>", typeof(TEntity).Name);
                sb.Append(ToString());
                sb.AppendFormat(" 总次数{0,7:n0}", Total);
                if (Success > 0) sb.AppendFormat("，命中{0,7:n0}（{1,6:P02}）", Success, (Double)Success / Total);

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
            this.Expire = ec.Expire;
            this.FillListMethod = ec.FillListMethod;

            return this;
        }

        /// <summary>输出名称</summary>
        /// <returns></returns>
        public override String ToString()
        {
            var type = GetType();
            return "{0}<{1}>".F(type.GetDisplayName() ?? type.Name, typeof(TEntity).FullName);
        }
        #endregion
    }
}